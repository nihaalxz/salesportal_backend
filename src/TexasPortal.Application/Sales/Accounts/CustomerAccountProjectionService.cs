using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;

namespace TexasPortal.Application.Sales.Accounts;

public class CustomerAccountProjectionService : ICustomerAccountProjectionService
{
    private readonly ISalesPortalDbContext _db;

    public CustomerAccountProjectionService(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> RebuildCustomerBalanceCacheAsync(long headId, CancellationToken ct = default)
    {
        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) throw new InvalidOperationException($"Head #{headId} not found.");

        // Calculate authoritative balance from CustomerAccountTransactions (fallback to BalanceTransactions if empty)
        decimal calculatedBalance = 0m;
        var hasAccountTxns = await _db.CustomerAccountTransactions.AnyAsync(t => t.HeadId == headId, ct);

        if (hasAccountTxns)
        {
            calculatedBalance = await _db.CustomerAccountTransactions
                .Where(t => t.HeadId == headId)
                .SumAsync(t => t.Amount, ct);
        }
        else
        {
            calculatedBalance = await _db.BalanceTransactions
                .Where(t => t.HeadId == headId)
                .SumAsync(t => t.Amount, ct);
        }

        head.Balance = calculatedBalance;
        _db.Entry(head).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);

        return calculatedBalance;
    }

    public async Task<decimal> RebuildReceivableOutstandingCacheAsync(long creditReceivableId, CancellationToken ct = default)
    {
        var rec = await _db.CreditReceivables.FirstOrDefaultAsync(r => r.Id == creditReceivableId, ct);
        if (rec == null) throw new InvalidOperationException($"CreditReceivable #{creditReceivableId} not found.");

        // Authoritative sum from ReceivableTransactions
        var netTxnSum = await _db.ReceivableTransactions
            .Where(t => t.ReceivableId == creditReceivableId)
            .SumAsync(t => t.Amount, ct);

        var calculatedOutstanding = Math.Max(0m, rec.TotalAmount + netTxnSum);
        rec.CachedOutstandingAmount = calculatedOutstanding;

        if (calculatedOutstanding <= 0m)
        {
            rec.Status = Domain.Enums.ReceivableStatus.Closed;
            rec.ClosedDate = DateTime.UtcNow;
        }

        _db.Entry(rec).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);

        return calculatedOutstanding;
    }

    public async Task<bool> ValidateCustomerLedgerIntegrityAsync(long headId, CancellationToken ct = default)
    {
        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) return false;

        var hasAccountTxns = await _db.CustomerAccountTransactions.AnyAsync(t => t.HeadId == headId, ct);
        decimal sum = 0m;

        if (hasAccountTxns)
        {
            sum = await _db.CustomerAccountTransactions
                .Where(t => t.HeadId == headId)
                .SumAsync(t => t.Amount, ct);
        }
        else
        {
            sum = await _db.BalanceTransactions
                .Where(t => t.HeadId == headId)
                .SumAsync(t => t.Amount, ct);
        }

        return Math.Abs(head.Balance - sum) < 0.001m;
    }
}
