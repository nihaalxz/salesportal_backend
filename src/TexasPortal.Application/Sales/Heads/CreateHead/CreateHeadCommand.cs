using TexasPortal.Contracts.Sales.Heads;

namespace TexasPortal.Application.Sales.Heads.CreateHead;

public sealed record CreateHeadCommand(
    long CurrentUserId,
    CreateHeadRequest Request
);
