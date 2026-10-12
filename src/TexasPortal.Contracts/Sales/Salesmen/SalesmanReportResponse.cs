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
    public decimal TotalProfit { get; init; }

    // Service Breakdown Statistics
    public int FlightCount { get; init; }
    public decimal FlightSales { get; init; }
    public decimal FlightProfit { get; init; }

    public int HotelCount { get; init; }
    public decimal HotelSales { get; init; }
    public decimal HotelProfit { get; init; }

    public int VisaCount { get; init; }
    public decimal VisaSales { get; init; }
    public decimal VisaProfit { get; init; }

    public int VfsCount { get; init; }
    public decimal VfsSales { get; init; }
    public decimal VfsProfit { get; init; }

    public int HolidayCount { get; init; }
    public decimal HolidaySales { get; init; }
    public decimal HolidayProfit { get; init; }

    public int TransportCount { get; init; }
    public decimal TransportSales { get; init; }
    public decimal TransportProfit { get; init; }

    public int OtherCount { get; init; }
    public decimal OtherSales { get; init; }
    public decimal OtherProfit { get; init; }

    public decimal CreditToCollect { get; init; }
}

