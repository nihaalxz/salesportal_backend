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

public interface IReceivableService
{
    Task<CreditReceivable> CreateReceivableAsync(CreateReceivableDto dto, CancellationToken ct = default);
    Task<CreditReceivableResponse?> GetReceivableByIdAsync(long id, CancellationToken ct = default);
    Task<List<CreditReceivableResponse>> QueryReceivablesAsync(string? status = null, string? bookingModule = null, string? priority = null, long? salesmanId = null, long? headId = null, CancellationToken ct = default);
    Task<List<HeadReceivableApplicationResponse>> GetHeadOpenReceivablesAsync(long headId, CancellationToken ct = default);
    Task RecalculateLedgerBalanceAsync(long receivableId, CancellationToken ct = default);
}

public class ReceivableService : IReceivableService
{
    private readonly ISalesPortalDbContext _db;
    private readonly ICreditPolicyService _creditPolicyService;

    public ReceivableService(ISalesPortalDbContext db, ICreditPolicyService creditPolicyService)
    {
        _db = db;
        _creditPolicyService = creditPolicyService;
    }

    public async Task<CreditReceivable> CreateReceivableAsync(CreateReceivableDto dto, CancellationToken ct = default)
    {
        Enum.TryParse<BookingModule>(dto.BookingModule, true, out var module);
        Enum.TryParse<HeadType>(dto.HeadType, true, out var hType);
        Enum.TryParse<CreditTerm>(dto.CreditTerm, true, out var cTerm);

        if (dto.HeadId.HasValue)
        {
            await _creditPolicyService.ValidateCreditLimitAsync(dto.HeadId.Value, hType, dto.TotalAmount, ct);
        }

        var dueDate = _creditPolicyService.CalculateDueDate(DateTime.UtcNow, cTerm);

        var receivable = new CreditReceivable
        {
            BookingId = dto.BookingId,
            BookingNumber = dto.BookingNumber,
            BookingModule = module,
            HeadId = dto.HeadId,
            HeadType = hType,
            CustomerId = dto.CustomerId,
            AgentId = dto.AgentId,
            SalesmanId = dto.SalesmanId,
            Currency = "BHD",
            TotalAmount = dto.TotalAmount,
            CachedOutstandingAmount = dto.TotalAmount,
            Status = ReceivableStatus.Open,
            PaymentStatus = ReceivablePaymentStatus.Pending,
            CreditTerm = cTerm,
            DueDate = dueDate,
            CreatedDate = DateTime.UtcNow,
            Remarks = dto.Remarks
        };

        _db.CreditReceivables.Add(receivable);
        await _db.SaveChangesAsync(ct);

        // Record initial CreditSale ledger transaction (+ TotalAmount)
        var initialTxn = new ReceivableTransaction
        {
            ReceivableId = receivable.Id,
            TransactionType = CreditTransactionType.CreditSale,
            Amount = dto.TotalAmount,
            ReferenceId = dto.BookingNumber,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = dto.SalesmanId,
            Remarks = "Initial Credit Sale creation"
        };

        _db.ReceivableTransactions.Add(initialTxn);
        await _db.SaveChangesAsync(ct);

        return receivable;
    }

    public async Task<CreditReceivableResponse?> GetReceivableByIdAsync(long id, CancellationToken ct = default)
    {
        var r = await _db.CreditReceivables
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (r == null) return null;

        return MapToResponse(r);
    }

    public async Task<List<CreditReceivableResponse>> QueryReceivablesAsync(string? status = null, string? bookingModule = null, string? priority = null, long? salesmanId = null, long? headId = null, CancellationToken ct = default)
    {
        var query = _db.CreditReceivables.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReceivableStatus>(status, true, out var rStatus))
        {
            query = query.Where(x => x.Status == rStatus);
        }
        if (!string.IsNullOrWhiteSpace(bookingModule) && Enum.TryParse<BookingModule>(bookingModule, true, out var bModule))
        {
            query = query.Where(x => x.BookingModule == bModule);
        }
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<CollectionPriority>(priority, true, out var cPriority))
        {
            query = query.Where(x => x.Priority == cPriority);
        }
        if (salesmanId.HasValue)
        {
            query = query.Where(x => x.SalesmanId == salesmanId.Value);
        }
        if (headId.HasValue)
        {
            query = query.Where(x => x.HeadId == headId.Value);
        }

        var list = await query.OrderByDescending(x => x.CreatedDate).ToListAsync(ct);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<List<HeadReceivableApplicationResponse>> GetHeadOpenReceivablesAsync(long headId, CancellationToken ct = default)
    {
        await EnsureCreditReceivablesSyncedAsync(headId, ct);

        var openRecs = await _db.CreditReceivables
            .AsNoTracking()
            .Where(x => (x.HeadId == headId || x.CustomerId == headId || x.AgentId == headId) && (x.Status == ReceivableStatus.Open || x.Status == ReceivableStatus.PartiallyPaid))
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(ct);

        return openRecs.Select(x => new HeadReceivableApplicationResponse(
            x.Id,
            x.BookingNumber ?? ("REC-" + x.Id),
            x.BookingModule.ToString(),
            x.TotalAmount,
            x.TotalAmount - x.CachedOutstandingAmount,
            x.CachedOutstandingAmount,
            x.Status.ToString(),
            x.CreatedDate
        )).ToList();
    }

    private async Task EnsureCreditReceivablesSyncedAsync(long headId, CancellationToken ct)
    {
        var newReceivables = new List<CreditReceivable>();

        // 1. Visas
        var visaApps = await _db.SalesVisaApplications
            .AsNoTracking()
            .Where(v => (v.HeadId == headId || v.AgentId == headId))
            .ToListAsync(ct);

        foreach (var visa in visaApps)
        {
            var isCredit = await _db.SalesVisaApplicants.AnyAsync(a => a.SalesVisaApplicationId == visa.Id && a.PaymentMethod == "credit", ct);
            if (isCredit)
            {
                var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == visa.DocumentNumber || (cr.BookingId == visa.Id.ToString() && cr.BookingModule == BookingModule.Visa), ct);
                if (!exists)
                {
                    newReceivables.Add(new CreditReceivable
                    {
                        BookingId = visa.Id.ToString(),
                        BookingNumber = visa.DocumentNumber,
                        BookingModule = BookingModule.Visa,
                        HeadId = visa.HeadId ?? visa.AgentId,
                        HeadType = visa.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                        CustomerId = visa.HeadId,
                        AgentId = visa.AgentId,
                        SalesmanId = visa.UserId,
                        Currency = "BHD",
                        TotalAmount = visa.PayableAmount,
                        CachedOutstandingAmount = visa.PayableAmount,
                        Status = ReceivableStatus.Open,
                        PaymentStatus = ReceivablePaymentStatus.Pending,
                        CreditTerm = CreditTerm.Days30,
                        DueDate = DateTime.UtcNow.AddDays(30),
                        CreatedDate = visa.CreatedAt ?? DateTime.UtcNow,
                        Remarks = $"Synced Credit Sale for Visa #{visa.DocumentNumber}"
                    });
                }
            }
        }

        // 2. VFS
        var vfsApps = await _db.VfsAppointments
            .AsNoTracking()
            .Where(v => (v.HeadId == headId || v.AgentId == headId) && (v.PaymentMode == "credit" || v.PaymentMode == "Credit"))
            .ToListAsync(ct);

        foreach (var vfs in vfsApps)
        {
            var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == vfs.DocumentNumber || (cr.BookingId == vfs.Id.ToString() && cr.BookingModule == BookingModule.Vfs), ct);
            if (!exists)
            {
                newReceivables.Add(new CreditReceivable
                {
                    BookingId = vfs.Id.ToString(),
                    BookingNumber = vfs.DocumentNumber,
                    BookingModule = BookingModule.Vfs,
                    HeadId = vfs.HeadId ?? vfs.AgentId,
                    HeadType = vfs.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                    CustomerId = vfs.HeadId,
                    AgentId = vfs.AgentId,
                    SalesmanId = vfs.CreatedBy,
                    Currency = "BHD",
                    TotalAmount = vfs.PayableAmount,
                    CachedOutstandingAmount = vfs.PayableAmount,
                    Status = ReceivableStatus.Open,
                    PaymentStatus = ReceivablePaymentStatus.Pending,
                    CreditTerm = CreditTerm.Days30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    CreatedDate = vfs.CreatedAt ?? DateTime.UtcNow,
                    Remarks = $"Synced Credit Sale for VFS #{vfs.DocumentNumber}"
                });
            }
        }

        // 3. Flight Bookings
        var flightApps = await _db.FlightBookings
            .AsNoTracking()
            .Where(f => (f.HeadId == headId || f.AgentId == headId) && (f.PaymentMethod == "credit" || f.PaymentMethod == "Credit"))
            .ToListAsync(ct);

        foreach (var flt in flightApps)
        {
            var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == flt.DocumentNumber || (cr.BookingId == flt.Id.ToString() && cr.BookingModule == BookingModule.Flight), ct);
            if (!exists)
            {
                newReceivables.Add(new CreditReceivable
                {
                    BookingId = flt.Id.ToString(),
                    BookingNumber = flt.DocumentNumber,
                    BookingModule = BookingModule.Flight,
                    HeadId = flt.HeadId ?? flt.AgentId,
                    HeadType = flt.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                    CustomerId = flt.HeadId,
                    AgentId = flt.AgentId,
                    SalesmanId = flt.UserId,
                    Currency = "BHD",
                    TotalAmount = flt.PayableAmount,
                    CachedOutstandingAmount = flt.PayableAmount,
                    Status = ReceivableStatus.Open,
                    PaymentStatus = ReceivablePaymentStatus.Pending,
                    CreditTerm = CreditTerm.Days30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    CreatedDate = flt.CreatedAt ?? DateTime.UtcNow,
                    Remarks = $"Synced Credit Sale for Flight #{flt.DocumentNumber}"
                });
            }
        }

        // 4. Hotel Bookings
        var hotelApps = await _db.HotelBookings
            .AsNoTracking()
            .Where(h => (h.HeadId == headId || h.AgentId == headId) && (h.PaymentMethod == "credit" || h.PaymentMethod == "Credit"))
            .ToListAsync(ct);

        foreach (var htl in hotelApps)
        {
            var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == htl.DocumentNumber || (cr.BookingId == htl.Id.ToString() && cr.BookingModule == BookingModule.Hotel), ct);
            if (!exists)
            {
                newReceivables.Add(new CreditReceivable
                {
                    BookingId = htl.Id.ToString(),
                    BookingNumber = htl.DocumentNumber,
                    BookingModule = BookingModule.Hotel,
                    HeadId = htl.HeadId ?? htl.AgentId,
                    HeadType = htl.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                    CustomerId = htl.HeadId,
                    AgentId = htl.AgentId,
                    SalesmanId = htl.UserId,
                    Currency = "BHD",
                    TotalAmount = htl.PayableAmount,
                    CachedOutstandingAmount = htl.PayableAmount,
                    Status = ReceivableStatus.Open,
                    PaymentStatus = ReceivablePaymentStatus.Pending,
                    CreditTerm = CreditTerm.Days30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    CreatedDate = htl.CreatedAt ?? DateTime.UtcNow,
                    Remarks = $"Synced Credit Sale for Hotel #{htl.DocumentNumber}"
                });
            }
        }

        // 5. Holiday Packages
        var holApps = await _db.HolidayPackages
            .AsNoTracking()
            .Where(h => (h.HeadId == headId || h.AgentId == headId) && (h.PaymentMode == "credit" || h.PaymentMode == "Credit"))
            .ToListAsync(ct);

        foreach (var hol in holApps)
        {
            var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == hol.DocumentNumber || (cr.BookingId == hol.Id.ToString() && cr.BookingModule == BookingModule.Holiday), ct);
            if (!exists)
            {
                newReceivables.Add(new CreditReceivable
                {
                    BookingId = hol.Id.ToString(),
                    BookingNumber = hol.DocumentNumber,
                    BookingModule = BookingModule.Holiday,
                    HeadId = hol.HeadId ?? hol.AgentId,
                    HeadType = hol.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                    CustomerId = hol.HeadId,
                    AgentId = hol.AgentId,
                    SalesmanId = hol.CreatedBy,
                    Currency = "BHD",
                    TotalAmount = hol.PayableAmount,
                    CachedOutstandingAmount = hol.PayableAmount,
                    Status = ReceivableStatus.Open,
                    PaymentStatus = ReceivablePaymentStatus.Pending,
                    CreditTerm = CreditTerm.Days30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    CreatedDate = hol.CreatedAt ?? DateTime.UtcNow,
                    Remarks = $"Synced Credit Sale for Holiday #{hol.DocumentNumber}"
                });
            }
        }

        // 6. Transportations
        var transApps = await _db.Transportations
            .AsNoTracking()
            .Where(t => (t.HeadId == headId || t.AgentId == headId) && (t.PaymentMethod == "credit" || t.PaymentMethod == "Credit"))
            .ToListAsync(ct);

        foreach (var tr in transApps)
        {
            var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == tr.DocumentNumber || (cr.BookingId == tr.Id.ToString() && cr.BookingModule == BookingModule.Transport), ct);
            if (!exists)
            {
                newReceivables.Add(new CreditReceivable
                {
                    BookingId = tr.Id.ToString(),
                    BookingNumber = tr.DocumentNumber,
                    BookingModule = BookingModule.Transport,
                    HeadId = tr.HeadId ?? tr.AgentId,
                    HeadType = tr.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                    CustomerId = tr.HeadId,
                    AgentId = tr.AgentId,
                    SalesmanId = tr.CreatedBy,
                    Currency = "BHD",
                    TotalAmount = tr.PayableAmount,
                    CachedOutstandingAmount = tr.PayableAmount,
                    Status = ReceivableStatus.Open,
                    PaymentStatus = ReceivablePaymentStatus.Pending,
                    CreditTerm = CreditTerm.Days30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    CreatedDate = tr.CreatedAt ?? DateTime.UtcNow,
                    Remarks = $"Synced Credit Sale for Transport #{tr.DocumentNumber}"
                });
            }
        }

        // 7. Other Services
        var othApps = await _db.OtherServices
            .AsNoTracking()
            .Where(o => (o.HeadId == headId || o.AgentId == headId) && (o.PaymentMethod == "credit" || o.PaymentMethod == "Credit"))
            .ToListAsync(ct);

        foreach (var oth in othApps)
        {
            var exists = await _db.CreditReceivables.AnyAsync(cr => cr.BookingNumber == oth.DocumentNumber || (cr.BookingId == oth.Id.ToString() && cr.BookingModule == BookingModule.OtherService), ct);
            if (!exists)
            {
                newReceivables.Add(new CreditReceivable
                {
                    BookingId = oth.Id.ToString(),
                    BookingNumber = oth.DocumentNumber,
                    BookingModule = BookingModule.OtherService,
                    HeadId = oth.HeadId ?? oth.AgentId,
                    HeadType = oth.ClientType == "agent" ? HeadType.Agent : HeadType.Customer,
                    CustomerId = oth.HeadId,
                    AgentId = oth.AgentId,
                    SalesmanId = oth.CreatedBy,
                    Currency = "BHD",
                    TotalAmount = oth.PayableAmount,
                    CachedOutstandingAmount = oth.PayableAmount,
                    Status = ReceivableStatus.Open,
                    PaymentStatus = ReceivablePaymentStatus.Pending,
                    CreditTerm = CreditTerm.Days30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    CreatedDate = oth.CreatedAt ?? DateTime.UtcNow,
                    Remarks = $"Synced Credit Sale for Other Service #{oth.DocumentNumber}"
                });
            }
        }

        if (newReceivables.Count > 0)
        {
            _db.CreditReceivables.AddRange(newReceivables);
            await _db.SaveChangesAsync(ct);

            foreach (var rec in newReceivables)
            {
                _db.ReceivableTransactions.Add(new ReceivableTransaction
                {
                    ReceivableId = rec.Id,
                    TransactionType = CreditTransactionType.CreditSale,
                    Amount = rec.TotalAmount,
                    CorrelationId = $"initial-creditsale:{rec.Id}",
                    ReferenceId = rec.BookingNumber,
                    TransactionDate = rec.CreatedDate,
                    CreatedBy = rec.SalesmanId,
                    Remarks = $"Initial Credit Sale creation for {rec.BookingModule} #{rec.BookingNumber}"
                });
            }
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RecalculateLedgerBalanceAsync(long receivableId, CancellationToken ct = default)
    {
        var receivable = await _db.CreditReceivables
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == receivableId, ct);

        if (receivable == null) return;

        // SUM all immutable transactions (+ for CreditSale/Adjustment, - for ReceiptPayment/CreditNote/WriteOff)
        var sum = receivable.Transactions.Sum(t => t.Amount);
        receivable.CachedOutstandingAmount = Math.Max(0m, sum);

        if (receivable.CachedOutstandingAmount <= 0)
        {
            receivable.Status = ReceivableStatus.Closed;
            receivable.PaymentStatus = ReceivablePaymentStatus.Approved;
            receivable.ClosedDate = DateTime.UtcNow;
        }
        else if (receivable.CachedOutstandingAmount < receivable.TotalAmount)
        {
            receivable.Status = ReceivableStatus.PartiallyPaid;
            receivable.PaymentStatus = ReceivablePaymentStatus.UnderReview;
        }
        else
        {
            receivable.Status = ReceivableStatus.Open;
            receivable.PaymentStatus = ReceivablePaymentStatus.Pending;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static CreditReceivableResponse MapToResponse(CreditReceivable r)
    {
        return new CreditReceivableResponse(
            r.Id,
            r.BookingId,
            r.BookingNumber,
            r.BookingModule.ToString(),
            r.HeadId,
            r.HeadType.ToString(),
            r.CustomerId,
            r.AgentId,
            r.SalesmanId,
            r.Currency,
            r.TotalAmount,
            r.CachedOutstandingAmount,
            r.Status.ToString(),
            r.PaymentStatus.ToString(),
            r.Priority.ToString(),
            r.CreditTerm.ToString(),
            r.DueDate,
            r.CreatedDate,
            r.ClosedDate,
            r.Remarks
        );
    }
}
