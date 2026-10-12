using TexasPortal.Contracts.Sales.Receipts;

namespace TexasPortal.Application.Sales.Receipts.UpdateReceiptStatus;

public sealed record UpdateReceiptStatusCommand(
    long CurrentUserId,
    long ReceiptId,
    UpdateReceiptStatusRequest Request
);
