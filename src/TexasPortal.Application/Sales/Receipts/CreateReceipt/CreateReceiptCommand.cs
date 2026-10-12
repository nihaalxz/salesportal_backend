using TexasPortal.Contracts.Sales.Receipts;

namespace TexasPortal.Application.Sales.Receipts.CreateReceipt;

public sealed record CreateReceiptCommand(
    long CurrentUserId,
    CreateReceiptRequest Request
);
