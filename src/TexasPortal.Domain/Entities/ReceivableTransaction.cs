using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class ReceivableTransaction
{
    public long Id { get; set; }
    public long ReceivableId { get; set; }
    public CreditTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; } // + for debt increase, - for settlement/reduction
    public string? ReferenceId { get; set; }
    /// <summary>
    /// Unique idempotency key. Format: "receipt-approval:{receiptId}:receivable:{receivableId}".
    /// A unique filtered index on this column prevents duplicate ledger entries on retry.
    /// </summary>
    public string? CorrelationId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public string? Remarks { get; set; }

    public virtual CreditReceivable Receivable { get; set; } = null!;
}
