using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Dashboard;

/// <summary>
/// A single data point in the 7-day trend series.
/// </summary>
public sealed class DashboardTrendPoint
{
    /// <summary>Date label e.g. "Mon", "Tue".</summary>
    public string Label { get; init; } = null!;

    /// <summary>Calendar date (UTC date).</summary>
    public DateOnly Date { get; init; }

    /// <summary>Total payable amount from all bookings created on this date.</summary>
    public decimal TotalValue { get; init; }

    /// <summary>Number of bookings created on this date.</summary>
    public int BookingCount { get; init; }
}

/// <summary>
/// 7-day rolling booking value trend response.
/// </summary>
public sealed class DashboardTrendResponse
{
    public List<DashboardTrendPoint> Points { get; init; } = new();
}
