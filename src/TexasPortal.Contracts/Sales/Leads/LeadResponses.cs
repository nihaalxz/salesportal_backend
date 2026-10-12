using System;

namespace TexasPortal.Contracts.Sales.Leads;

public class LeadSummaryResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public string CountryCode { get; init; } = null!;
    public string MobileNo { get; init; } = null!;
    public string? Services { get; init; }
    public string? LeadSource { get; init; }
    public string Status { get; init; } = null!;
    public bool ReminderActive { get; init; }
    public DateTime? ReminderDate { get; init; }
    public DateTime? CreatedAt { get; init; }

    /// <summary>Name of the salesman who owns this lead (set by OperationsReadService for accountant-side views).</summary>
    public string? CreatedByName { get; init; }

    /// <summary>ID of the salesman who owns this lead.</summary>
    public long CreatedById { get; init; }
}

public sealed class LeadDetailResponse : LeadSummaryResponse
{
    public string? Description { get; init; }
}

