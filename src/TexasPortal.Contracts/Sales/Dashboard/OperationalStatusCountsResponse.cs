namespace TexasPortal.Contracts.Sales.Dashboard;

/// <summary>
/// Operational status count response — returned by the internal count endpoint
/// used by the Operational Portal dashboard to show accurate pending/under-review queues.
/// </summary>
public sealed record OperationalStatusCountsResponse(
    int PendingVisasCount,
    int UnderReviewVisasCount,
    int PendingVfsCount,
    int UnderReviewVfsCount,
    int PendingHolidaysCount,
    int UnderReviewHolidaysCount
);
