using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Receivables;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receivables;

public interface IStatementService
{
    Task<CustomerRunningStatementResponse?> GetHeadRunningStatementAsync(long headId, HeadType headType, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
}

public class StatementService : IStatementService
{
    private readonly ISalesPortalDbContext _db;

    public StatementService(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerRunningStatementResponse?> GetHeadRunningStatementAsync(long headId, HeadType headType, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var head = await _db.Heads.AsNoTracking().FirstOrDefaultAsync(h => h.Id == headId, ct);
        var headName = head != null ? $"{head.FirstName} {head.LastName}".Trim() : $"Head #{headId}";

        var receivables = await _db.CreditReceivables
            .AsNoTracking()
            .Include(r => r.Transactions)
            .Where(r => r.HeadId == headId && r.HeadType == headType)
            .ToListAsync(ct);

        if (!receivables.Any())
        {
            return new CustomerRunningStatementResponse(
                headId,
                headName,
                headType.ToString(),
                0m,
                0m,
                0m,
                0m,
                0,
                null,
                null,
                new List<RunningLedgerEntryResponse>()
            );
        }

        var allTxns = receivables.SelectMany(r => r.Transactions).OrderBy(t => t.TransactionDate).ToList();

        var sDate = startDate ?? DateTime.MinValue;
        var eDate = endDate ?? DateTime.MaxValue;

        var openingTxns = allTxns.Where(t => t.TransactionDate < sDate).ToList();
        decimal openingBalance = openingTxns.Sum(t => t.Amount);

        var periodTxns = allTxns.Where(t => t.TransactionDate >= sDate && t.TransactionDate <= eDate).ToList();

        decimal running = openingBalance;
        decimal totalCredit = 0m;
        decimal totalPaid = 0m;
        var ledgerEntries = new List<RunningLedgerEntryResponse>();

        foreach (var t in periodTxns)
        {
            decimal debit = t.Amount > 0 ? t.Amount : 0m;
            decimal credit = t.Amount < 0 ? Math.Abs(t.Amount) : 0m;

            totalCredit += debit;
            totalPaid += credit;

            running += (debit - credit);

            ledgerEntries.Add(new RunningLedgerEntryResponse(
                t.Id,
                t.TransactionDate,
                t.TransactionType.ToString(),
                t.Remarks ?? t.TransactionType.ToString(),
                debit,
                credit,
                running
            ));
        }

        decimal closingBalance = running;

        var payments = periodTxns.Where(t => t.TransactionType == CreditTransactionType.ReceiptPayment).ToList();
        DateTime? lastPaymentDate = payments.LastOrDefault()?.TransactionDate;
        DateTime? nextDueDate = receivables.Where(r => r.Status != ReceivableStatus.Closed).OrderBy(r => r.DueDate).FirstOrDefault()?.DueDate;

        double avgCollectionDays = 0;
        if (payments.Any())
        {
            var totalDays = payments.Sum(p => (p.TransactionDate - receivables.First(r => r.Id == p.ReceivableId).CreatedDate).TotalDays);
            avgCollectionDays = Math.Round(totalDays / payments.Count, 1);
        }

        return new CustomerRunningStatementResponse(
            headId,
            headName,
            headType.ToString(),
            openingBalance,
            totalCredit,
            totalPaid,
            closingBalance,
            avgCollectionDays,
            lastPaymentDate,
            nextDueDate,
            ledgerEntries
        );
    }
}
