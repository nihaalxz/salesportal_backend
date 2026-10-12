using System;
using System.Collections.Generic;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class CollectionBatch
{
    public long Id { get; set; }
    public string BatchNumber { get; set; } = null!;
    public long SalesmanId { get; set; }
    public decimal TotalAmount { get; set; }
    public int ReceiptCount { get; set; }
    public ReceivablePaymentStatus Status { get; set; } = ReceivablePaymentStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedBy { get; set; }
    public string? Remarks { get; set; }

    public virtual ICollection<ReceivablePayment> Payments { get; set; } = new List<ReceivablePayment>();
}
