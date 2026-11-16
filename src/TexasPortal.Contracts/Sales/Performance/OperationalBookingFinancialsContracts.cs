using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Performance;

public sealed class OperationalBookingFinancialsSummaryResponse
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string DealStatus { get; init; } = "all";
    public long? SalesmanId { get; init; }
    public int TotalBookingsCount { get; init; }
    public decimal TotalSalesValue { get; init; }
    public decimal TotalSupplierCost { get; init; }
    public decimal TotalGrossProfit { get; init; }
    public decimal TotalFinalProfit { get; init; }
    public decimal TotalEstimatedProfit { get; init; }
    public List<ServiceBookingFinancialItem> ServiceBreakdown { get; init; } = new();
}

public sealed class ServiceBookingFinancialItem
{
    public string ServiceType { get; init; } = string.Empty;
    public string ServiceLabel { get; init; } = string.Empty;
    public int BookingCount { get; init; }
    public decimal SalesAmount { get; init; }
    public decimal CostAmount { get; init; }
    public decimal ProfitAmount { get; init; }
    public decimal FinalProfitAmount { get; init; }
    public decimal MarginPercentage => SalesAmount > 0m ? Math.Round((ProfitAmount / SalesAmount) * 100m, 2) : 0m;
}

