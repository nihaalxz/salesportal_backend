using System;

namespace TexasPortal.Domain.Entities;

public class ReceiptAllocation
{
    public long Id { get; set; }
    public long ReceiptId { get; set; }
    public long ReceivableId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime? AllocationDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }

    public virtual Receipt Receipt { get; set; } = null!;
    public virtual CreditReceivable Receivable { get; set; } = null!;
}
