using System;

namespace TexasPortal.Contracts.Sales.Dashboard;

public sealed class DashboardSummaryResponse
{
    // Booking Counts
    public int TotalFlightBookings { get; init; }
    public int TotalHotelBookings { get; init; }
    public int TotalVisaApplications { get; init; }
    public int TotalVfsAppointments { get; init; }
    public int TotalHolidayPackages { get; init; }
    public int TotalTransportations { get; init; }
    public int TotalOtherServices { get; init; }

    // Financial Aggregates (BHD)
    public decimal TotalPayableAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalPendingAmount { get; init; }
    public decimal TotalProfit { get; init; }

    // Cash & Wallet
    public decimal CashInHand { get; init; }

    // Active Leads & Customers
    public int ActiveLeads { get; init; }
    public int TotalHeads { get; init; }

    // Aggregates for Accounts & Sales Dashboard
    public decimal TotalReceivables { get; init; }
    public decimal TotalCustomerWallets { get; init; }
    public int PendingReceipts { get; init; }
    public int PendingApplications { get; init; }
}
