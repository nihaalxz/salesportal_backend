using System;

namespace TexasPortal.Domain.Entities;

public class BadDebtWriteOff
{
    public long Id { get; set; }
    public string WriteOffNumber { get; set; } = null!;
    public long ReceivableId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = null!;
    public long ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual CreditReceivable Receivable { get; set; } = null!;
}
