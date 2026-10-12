using TexasPortal.Contracts.Sales.Disputes;

namespace TexasPortal.Application.Sales.Disputes.ResolveDispute;

public sealed record ResolveDisputeCommand(
    long CurrentUserId,
    long DisputeId,
    ResolveDisputeRequest Request);
