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

public interface IReportingService
{
    Task<CreditAgingReportResponse> GetAgingReportAsync(CancellationToken ct = default);
    Task<CreditDashboardResponse> GetDashboardMetricsAsync(CancellationToken ct = default);
}

public class ReportingService : IReportingService
{
    private readonly ISalesPortalDbContext _db;

    public ReportingService(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<CreditAgingReportResponse> GetAgingReportAsync(CancellationToken ct = default)
    {
        var activeReceivables = await _db.CreditReceivables
            .AsNoTracking()
            .Where(r => r.Status != ReceivableStatus.Closed && r.Status != ReceivableStatus.Cancelled)
            .ToListAsync(ct);

        var totalOutstanding = activeReceivables.Sum(r => r.CachedOutstandingAmount);
        var today = DateTime.UtcNow.Date;

        var buckets = new List<AgingBucketDto>();

        // 1. Current (not overdue)
        var currentRecs = activeReceivables.Where(r => r.DueDate.Date >= today).ToList();
        buckets.Add(CreateBucket("Current", currentRecs, totalOutstanding));

        // 2. 0-30 Days Overdue
        var recs0_30 = activeReceivables.Where(r => r.DueDate.Date < today && (today - r.DueDate.Date).Days <= 30).ToList();
        buckets.Add(CreateBucket("0-30 Days", recs0_30, totalOutstanding));

        // 3. 31-60 Days Overdue
        var recs31_60 = activeReceivables.Where(r => (today - r.DueDate.Date).Days >= 31 && (today - r.DueDate.Date).Days <= 60).ToList();
        buckets.Add(CreateBucket("31-60 Days", recs31_60, totalOutstanding));

        // 4. 61-90 Days Overdue
        var recs61_90 = activeReceivables.Where(r => (today - r.DueDate.Date).Days >= 61 && (today - r.DueDate.Date).Days <= 90).ToList();
        buckets.Add(CreateBucket("61-90 Days", recs61_90, totalOutstanding));

        // 5. 90+ Days Overdue
        var recs90Plus = activeReceivables.Where(r => (today - r.DueDate.Date).Days > 90).ToList();
        buckets.Add(CreateBucket("90+ Days", recs90Plus, totalOutstanding));

        return new CreditAgingReportResponse(totalOutstanding, buckets);
    }

    public async Task<CreditDashboardResponse> GetDashboardMetricsAsync(CancellationToken ct = default)
    {
        var activeReceivables = await _db.CreditReceivables
            .AsNoTracking()
            .Where(r => r.Status != ReceivableStatus.Closed && r.Status != ReceivableStatus.Cancelled)
            .ToListAsync(ct);

        var totalOutstanding = activeReceivables.Sum(r => r.CachedOutstandingAmount);
        var today = DateTime.UtcNow.Date;

        var overdueReceivables = activeReceivables.Where(r => r.DueDate.Date < today).Sum(r => r.CachedOutstandingAmount);

        var todaysTxns = await _db.ReceivableTransactions
            .AsNoTracking()
            .Where(t => t.TransactionDate.Date == today)
            .ToListAsync(ct);

        var todaysCreditSales = todaysTxns.Where(t => t.TransactionType == CreditTransactionType.CreditSale).Sum(t => t.Amount);
        var todaysCollections = Math.Abs(todaysTxns.Where(t => t.TransactionType == CreditTransactionType.ReceiptPayment).Sum(t => t.Amount));

        var totalSalesAllTime = await _db.CreditReceivables.AsNoTracking().SumAsync(r => (decimal?)r.TotalAmount, ct) ?? 0m;
        var totalCollectionsAllTime = await _db.ReceivableTransactions
            .AsNoTracking()
            .Where(t => t.TransactionType == CreditTransactionType.ReceiptPayment)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var collectionPercentage = totalSalesAllTime > 0 ? Math.Round((double)(Math.Abs(totalCollectionsAllTime) / totalSalesAllTime) * 100.0, 1) : 100.0;

        // DSO Calculation: (Accounts Receivable / Total Credit Sales) * 30
        var last30DaysSales = await _db.CreditReceivables
            .AsNoTracking()
            .Where(r => r.CreatedDate >= today.AddDays(-30))
            .SumAsync(r => (decimal?)r.TotalAmount, ct) ?? 0m;

        double dso = last30DaysSales > 0 ? Math.Round((double)(totalOutstanding / last30DaysSales) * 30.0, 1) : 0.0;
        int activeDebtors = activeReceivables.Select(r => r.HeadId).Distinct().Count();

        return new CreditDashboardResponse(
            totalOutstanding,
            todaysCreditSales,
            todaysCollections,
            overdueReceivables,
            collectionPercentage,
            30.0,
            dso,
            activeDebtors
        );
    }

    private static AgingBucketDto CreateBucket(string name, List<CreditReceivable> recs, decimal totalOutstanding)
    {
        var outstanding = recs.Sum(r => r.CachedOutstandingAmount);
        var custCount = recs.Where(r => r.HeadType == HeadType.Customer).Select(r => r.HeadId).Distinct().Count();
        var agentCount = recs.Where(r => r.HeadType == HeadType.Agent).Select(r => r.HeadId).Distinct().Count();
        var pct = totalOutstanding > 0 ? Math.Round((double)(outstanding / totalOutstanding) * 100.0, 1) : 0.0;

        return new AgingBucketDto(
            name,
            outstanding,
            custCount,
            agentCount,
            recs.Count,
            pct
        );
    }
}
