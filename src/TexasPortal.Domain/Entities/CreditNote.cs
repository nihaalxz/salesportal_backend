using System;

namespace TexasPortal.Domain.Entities;

public class CreditNote
{
    public long Id { get; set; }
    public string CreditNoteNumber { get; set; } = null!;
    public long ReceivableId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = null!;
    public long ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual CreditReceivable Receivable { get; set; } = null!;
}
