using System;

namespace TexasPortal.Contracts.Sales.Salesmen;

public sealed class SalesmanReportResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public string Email { get; init; } = null!;
    public int LeadsCount { get; init; }
    public int BookingsCount { get; init; }
    public decimal TotalSalesValue { get; init; }
    public decimal TotalProfit { get; init; }          // Expected Profit
    public decimal TotalFinalProfit { get; init; }     // Realized / Final Profit

    // Service Breakdown Statistics (Expected Profit)
    public int FlightCount { get; init; }
    public decimal FlightSales { get; init; }
    public decimal FlightProfit { get; init; }
    public decimal FlightFinalProfit { get; init; }

    public int HotelCount { get; init; }
    public decimal HotelSales { get; init; }
    public decimal HotelProfit { get; init; }
    public decimal HotelFinalProfit { get; init; }

    public int VisaCount { get; init; }
    public decimal VisaSales { get; init; }
    public decimal VisaProfit { get; init; }
    public decimal VisaFinalProfit { get; init; }

    public int VfsCount { get; init; }
    public decimal VfsSales { get; init; }
    public decimal VfsProfit { get; init; }
    public decimal VfsFinalProfit { get; init; }

    public int HolidayCount { get; init; }
    public decimal HolidaySales { get; init; }
    public decimal HolidayProfit { get; init; }
    public decimal HolidayFinalProfit { get; init; }

    public int TransportCount { get; init; }
    public decimal TransportSales { get; init; }
    public decimal TransportProfit { get; init; }
    public decimal TransportFinalProfit { get; init; }

    public int OtherCount { get; init; }
    public decimal OtherSales { get; init; }
    public decimal OtherProfit { get; init; }
    public decimal OtherFinalProfit { get; init; }

    public decimal CreditToCollect { get; init; }

    // Reconciliation Check
    public bool IsReconciled { get; init; } = true;
    public decimal ReconciliationDifference { get; init; } = 0m;
}
