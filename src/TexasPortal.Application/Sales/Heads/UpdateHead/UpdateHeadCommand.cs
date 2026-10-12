using TexasPortal.Contracts.Sales.Heads;

namespace TexasPortal.Application.Sales.Heads.UpdateHead;

public sealed record UpdateHeadCommand(
    long CurrentUserId,
    long HeadId,
    UpdateHeadRequest Request
);
