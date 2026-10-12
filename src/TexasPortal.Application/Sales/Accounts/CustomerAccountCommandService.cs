using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Receivables;
using TexasPortal.Contracts.Sales.Accounts;
using TexasPortal.Contracts.Sales.Receivables;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;
using TexasPortal.Domain.Services;
using TexasPortal.Domain.ValueObjects;

namespace TexasPortal.Application.Sales.Accounts;

public class CustomerAccountCommandService : ICustomerAccountCommandService, ICustomerAccountService
{
    private readonly ISalesPortalDbContext _db;
    private readonly IPaymentAllocationEngine _allocationEngine;
    private readonly ICustomerAccountQueryService _queryService;
    private readonly IReceivableService _receivableService;
    private readonly IAgentsApiClient _agentsApiClient;

    public CustomerAccountCommandService(
        ISalesPortalDbContext db,
        IPaymentAllocationEngine allocationEngine,
        ICustomerAccountQueryService queryService,
        IReceivableService receivableService,
        IAgentsApiClient agentsApiClient)
    {
        _db = db;
        _allocationEngine = allocationEngine;
        _queryService = queryService;
        _receivableService = receivableService;
        _agentsApiClient = agentsApiClient;
    }

    public async Task<CreditAllocationResponse> AuthorizeAndProcessCreditBookingAsync(CreditBookingRequest request, CancellationToken ct = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        Enum.TryParse<HeadType>(request.HeadType, true, out var hType);

        Head? head = null;
        if (hType == HeadType.Customer)
        {
            head = await _db.Heads
                .FirstOrDefaultAsync(h => h.Id == request.HeadId && h.DeletedAt == null, ct);

            if (head == null)
                throw new InvalidOperationException($"Account Head #{request.HeadId} was not found.");
        }

        // Load financial position
        var summary = await _queryService.GetAccountSummaryAsync(request.HeadId, hType, ct);
        if (summary == null)
            throw new InvalidOperationException($"Could not calculate account summary for Head #{request.HeadId}.");

        var availableBalance = Math.Max(0m, summary.Balance);
        var openReceivables = await _db.CreditReceivables
            .AsNoTracking()
            .Where(r => (hType == HeadType.Agent ? r.AgentId == request.HeadId : r.HeadId == request.HeadId) && r.HeadType == hType && r.Status != ReceivableStatus.Closed && r.Status != ReceivableStatus.Cancelled)
            .SumAsync(r => (decimal?)r.CachedOutstandingAmount, ct) ?? 0m;

        var policy = await _db.CreditPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.HeadId == request.HeadId && p.HeadType == hType, ct);
        var creditLimit = policy?.MaxCreditLimit ?? 5000.000m;

        var input = new PaymentAllocationInput(
            AvailableBalance: Money.BHD(availableBalance),
            OutstandingReceivables: Money.BHD(openReceivables),
            CreditLimit: Money.BHD(creditLimit),
            BookingAmount: Money.BHD(request.TotalAmount),
            AutoApplyBalance: request.AutoApplyBalance
        );

        // Execute pure allocation engine calculation
        var output = _allocationEngine.CalculateAllocation(input);

        var correlationId = request.IdempotencyKey ?? $"booking-{request.BookingModule.ToLowerInvariant()}-{request.BookingId}";

        // 1. Append immutable CustomerAccountTransaction entry
        var accountTxn = new CustomerAccountTransaction
        {
            HeadId = request.HeadId,
            TransactionType = CustomerAccountTransactionType.BookingCharge,
            Amount = -request.TotalAmount,
            Currency = "BHD",
            CorrelationId = correlationId,
            ReferenceId = request.BookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = request.SalesmanId,
            Remarks = request.Remarks ?? $"Booking charge for {request.BookingModule} Booking #{request.BookingNumber}"
        };
        _db.CustomerAccountTransactions.Add(accountTxn);

        // Also add BalanceTransaction for backwards compatibility
        var balTxn = new BalanceTransaction
        {
            HeadId = request.HeadId,
            TransactionType = BalanceTransactionType.Booking,
            Amount = -request.TotalAmount,
            Currency = "BHD",
            CorrelationId = correlationId,
            ReferenceId = request.BookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = request.SalesmanId,
            Remarks = $"Booking charge for {request.BookingModule} Booking #{request.BookingNumber}"
        };
        _db.BalanceTransactions.Add(balTxn);

        // Update Balance
        if (hType == HeadType.Customer && head != null)
        {
            head.Balance -= request.TotalAmount;
            _db.Entry(head).State = EntityState.Modified;
        }
        else if (hType == HeadType.Agent)
        {
            await _agentsApiClient.DebitAgentWalletAsync(request.HeadId, request.TotalAmount, $"Credit Sale charge for {request.BookingModule} Booking #{request.BookingNumber}", ct);
        }

        long? creditReceivableId = null;

        // 2. If receivable amount > 0, create CreditReceivable entity (Receivable Ledger)
        if (output.ReceivableAmount.Amount > 0m)
        {
            var recDto = new CreateReceivableDto(
                BookingId: request.BookingId,
                BookingNumber: request.BookingNumber,
                BookingModule: request.BookingModule,
                HeadId: hType == HeadType.Customer ? request.HeadId : null,
                HeadType: request.HeadType,
                CustomerId: hType == HeadType.Customer ? request.HeadId : null,
                AgentId: hType == HeadType.Agent ? request.HeadId : null,
                SalesmanId: request.SalesmanId,
                TotalAmount: output.ReceivableAmount.Amount,
                CreditTerm: request.CreditTerm,
                Remarks: request.Remarks ?? $"Credit Sale for {request.BookingModule} Booking #{request.BookingNumber}"
            );

            var receivable = await _receivableService.CreateReceivableAsync(recDto, ct);
            creditReceivableId = receivable.Id;
        }

        // 3. Post HeadLedger entry ONLY for Customer (HeadLedger has FK to heads table)
        if (hType == HeadType.Customer)
        {
            var serviceType = request.BookingModule.ToLowerInvariant() switch
            {
                "transport" or "transportation" or "transportations" => "Transportation",
                "otherservice" or "other" or "others" => "Other",
                "vfsappointment" or "vfs" => "Vfs",
                "holidaypackage" or "holiday" or "holidays" => "Holiday",
                "flight" or "flights" => "Flight",
                "hotel" or "hotels" => "Hotel",
                "visa" or "visas" => "Visa",
                _ => request.BookingModule
            };

            var ledger = new HeadLedger
            {
                HeadId = request.HeadId,
                ServiceType = serviceType,
                ServiceId = long.TryParse(request.BookingId, out var bId) ? bId : 0L,
                Debit = request.TotalAmount,
                Credit = output.BalanceApplied.Amount,
                Description = $"{serviceType} Booking #{request.BookingNumber}",
                CreatedAt = DateTime.UtcNow
            };
            _db.HeadLedgers.Add(ledger);
        }

        await _db.SaveChangesAsync(ct);

        var paymentStatus = output.ReceivableAmount.Amount == 0m ? "success" : "pending";
        var balanceAfter = summary.Balance - request.TotalAmount;
        var outstandingAfter = openReceivables + output.ReceivableAmount.Amount;

        return new CreditAllocationResponse(
            BalanceTransactionId: accountTxn.Id > 0 ? accountTxn.Id : balTxn.Id,
            CreditReceivableId: creditReceivableId,
            TotalAmount: request.TotalAmount,
            BalanceApplied: output.BalanceApplied.Amount,
            ReceivableCreated: output.ReceivableAmount.Amount,
            AllocationType: output.AllocationType.ToString(),
            PaymentStatus: paymentStatus,
            BalanceAfter: balanceAfter,
            OutstandingAfter: outstandingAfter,
            Currency: "BHD"
        );
    }

    public async Task DepositBalanceAsync(long headId, decimal amount, string referenceId, long createdBy, string remarks, string? correlationId = null, CancellationToken ct = default)
    {
        if (amount <= 0m) throw new ArgumentException("Deposit amount must be greater than zero.", nameof(amount));

        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) throw new InvalidOperationException($"Head #{headId} not found.");

        var cid = correlationId ?? $"deposit-{headId}-{referenceId}";

        var accountTxn = new CustomerAccountTransaction
        {
            HeadId = headId,
            TransactionType = CustomerAccountTransactionType.Deposit,
            Amount = amount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = referenceId,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.CustomerAccountTransactions.Add(accountTxn);

        var balTxn = new BalanceTransaction
        {
            HeadId = headId,
            TransactionType = BalanceTransactionType.Deposit,
            Amount = amount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = referenceId,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.BalanceTransactions.Add(balTxn);

        head.Balance += amount;
        _db.Entry(head).State = EntityState.Modified;

        await _db.SaveChangesAsync(ct);
    }

    public async Task ProcessReceiptSettlementAsync(long headId, long receivableId, decimal settlementAmount, string referenceId, long createdBy, string remarks, string? correlationId = null, CancellationToken ct = default)
    {
        if (settlementAmount <= 0m) return;

        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) throw new InvalidOperationException($"Head #{headId} not found.");

        var rec = await _db.CreditReceivables.FirstOrDefaultAsync(r => r.Id == receivableId, ct);
        if (rec == null) throw new InvalidOperationException($"CreditReceivable #{receivableId} not found.");

        var cid = correlationId ?? $"settlement-{receivableId}-{referenceId}";

        // 1. Customer Ledger: Deposit settlement into Customer Account
        var accountTxn = new CustomerAccountTransaction
        {
            HeadId = headId,
            TransactionType = CustomerAccountTransactionType.ReceiptSettlement,
            Amount = settlementAmount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = referenceId,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.CustomerAccountTransactions.Add(accountTxn);

        var balTxn = new BalanceTransaction
        {
            HeadId = headId,
            TransactionType = BalanceTransactionType.Deposit,
            Amount = settlementAmount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = referenceId,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.BalanceTransactions.Add(balTxn);

        head.Balance += settlementAmount;
        _db.Entry(head).State = EntityState.Modified;

        // 2. Receivable Ledger: Reduce booking debt
        rec.CachedOutstandingAmount = Math.Max(0m, rec.CachedOutstandingAmount - settlementAmount);
        if (rec.CachedOutstandingAmount <= 0m)
        {
            rec.Status = ReceivableStatus.Closed;
            rec.ClosedDate = DateTime.UtcNow;
        }
        else
        {
            rec.Status = ReceivableStatus.PartiallyPaid;
        }
        _db.Entry(rec).State = EntityState.Modified;

        var recTxn = new ReceivableTransaction
        {
            ReceivableId = receivableId,
            TransactionType = CreditTransactionType.ReceiptPayment,
            Amount = -settlementAmount,
            CorrelationId = cid,
            ReferenceId = referenceId,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.ReceivableTransactions.Add(recTxn);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ProcessRefundAsync(long headId, decimal refundAmount, string bookingNumber, long createdBy, string remarks, string? correlationId = null, CancellationToken ct = default)
    {
        if (refundAmount <= 0m) return;

        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) throw new InvalidOperationException($"Head #{headId} not found.");

        var openReceivables = await _db.CreditReceivables
            .Where(r => r.HeadId == headId && r.BookingNumber == bookingNumber && r.Status != ReceivableStatus.Closed && r.Status != ReceivableStatus.Cancelled)
            .ToListAsync(ct);

        decimal remainingRefund = refundAmount;
        var cid = correlationId ?? $"refund-{bookingNumber}-{Guid.NewGuid():N}";

        foreach (var rec in openReceivables)
        {
            if (remainingRefund <= 0m) break;

            var recDeduction = Math.Min(rec.CachedOutstandingAmount, remainingRefund);
            rec.CachedOutstandingAmount -= recDeduction;
            remainingRefund -= recDeduction;

            if (rec.CachedOutstandingAmount <= 0m)
            {
                rec.Status = ReceivableStatus.Closed;
                rec.ClosedDate = DateTime.UtcNow;
            }
            else
            {
                rec.Status = ReceivableStatus.PartiallyPaid;
            }

            var recTxn = new ReceivableTransaction
            {
                ReceivableId = rec.Id,
                TransactionType = CreditTransactionType.CreditNote,
                Amount = -recDeduction,
                CorrelationId = cid,
                ReferenceId = bookingNumber,
                TransactionDate = DateTime.UtcNow,
                CreatedBy = createdBy,
                Remarks = $"Refund applied to open receivable #{rec.Id}"
            };
            _db.ReceivableTransactions.Add(recTxn);
        }

        // Customer Ledger: Deposit refund into Customer Balance
        var accountTxn = new CustomerAccountTransaction
        {
            HeadId = headId,
            TransactionType = CustomerAccountTransactionType.Refund,
            Amount = refundAmount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = bookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.CustomerAccountTransactions.Add(accountTxn);

        var balTxn = new BalanceTransaction
        {
            HeadId = headId,
            TransactionType = BalanceTransactionType.Refund,
            Amount = refundAmount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = bookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = remarks
        };
        _db.BalanceTransactions.Add(balTxn);

        head.Balance += refundAmount;
        _db.Entry(head).State = EntityState.Modified;

        await _db.SaveChangesAsync(ct);
    }

    public async Task ProcessCreditNoteAsync(long headId, long receivableId, decimal amount, string reason, long approvedBy, string? correlationId = null, CancellationToken ct = default)
    {
        if (amount <= 0m) return;

        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) throw new InvalidOperationException($"Head #{headId} not found.");

        var rec = await _db.CreditReceivables.FirstOrDefaultAsync(r => r.Id == receivableId, ct);
        if (rec == null) throw new InvalidOperationException($"CreditReceivable #{receivableId} not found.");

        var cid = correlationId ?? $"creditnote-{receivableId}-{Guid.NewGuid():N}";

        // 1. Customer Ledger entry
        var accountTxn = new CustomerAccountTransaction
        {
            HeadId = headId,
            TransactionType = CustomerAccountTransactionType.CreditNote,
            Amount = amount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = rec.BookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = approvedBy,
            Remarks = reason
        };
        _db.CustomerAccountTransactions.Add(accountTxn);

        head.Balance += amount;
        _db.Entry(head).State = EntityState.Modified;

        // 2. Receivable Ledger entry
        rec.CachedOutstandingAmount = Math.Max(0m, rec.CachedOutstandingAmount - amount);
        if (rec.CachedOutstandingAmount <= 0m)
        {
            rec.Status = ReceivableStatus.Closed;
            rec.ClosedDate = DateTime.UtcNow;
        }
        _db.Entry(rec).State = EntityState.Modified;

        var recTxn = new ReceivableTransaction
        {
            ReceivableId = receivableId,
            TransactionType = CreditTransactionType.CreditNote,
            Amount = -amount,
            CorrelationId = cid,
            ReferenceId = rec.BookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = approvedBy,
            Remarks = reason
        };
        _db.ReceivableTransactions.Add(recTxn);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ProcessAdjustmentAsync(long headId, decimal amount, string reason, long createdBy, string? correlationId = null, CancellationToken ct = default)
    {
        if (amount == 0m) return;

        var head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);
        if (head == null) throw new InvalidOperationException($"Head #{headId} not found.");

        var cid = correlationId ?? $"adjustment-{headId}-{Guid.NewGuid():N}";

        var accountTxn = new CustomerAccountTransaction
        {
            HeadId = headId,
            TransactionType = CustomerAccountTransactionType.ManualAdjustment,
            Amount = amount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = "MANUAL",
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = reason
        };
        _db.CustomerAccountTransactions.Add(accountTxn);

        var balTxn = new BalanceTransaction
        {
            HeadId = headId,
            TransactionType = BalanceTransactionType.Adjustment,
            Amount = amount,
            Currency = "BHD",
            CorrelationId = cid,
            ReferenceId = "MANUAL",
            TransactionDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            Remarks = reason
        };
        _db.BalanceTransactions.Add(balTxn);

        head.Balance += amount;
        _db.Entry(head).State = EntityState.Modified;

        await _db.SaveChangesAsync(ct);
    }
}
