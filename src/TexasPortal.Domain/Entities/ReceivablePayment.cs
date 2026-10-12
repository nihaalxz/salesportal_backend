using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class ReceivablePayment
{
    public long Id { get; set; }
    public long ReceivableId { get; set; }
    public long ReceiptId { get; set; }
    public long? CollectionBatchId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Credit";
    public string PaymentMode { get; set; } = "Credit";
    public DateTime? PaymentDate { get; set; } = DateTime.UtcNow;
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public long ReceivedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public ReceivablePaymentStatus Status { get; set; } = ReceivablePaymentStatus.Submitted;
    public string? Remarks { get; set; }

    public virtual CreditReceivable Receivable { get; set; } = null!;
    public virtual Receipt Receipt { get; set; } = null!;
    public virtual CollectionBatch? CollectionBatch { get; set; }
}
