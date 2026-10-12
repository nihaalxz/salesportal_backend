namespace TexasPortal.Application.Sales.Receipts.Approval;

/// <summary>
/// Command passed to <see cref="ReceiptApprovalOrchestrator"/> to approve a receipt
/// and atomically settle all linked credit receivables.
/// </summary>
public sealed record ReceiptApprovalCommand(long ReceiptId, long AccountantUserId);
