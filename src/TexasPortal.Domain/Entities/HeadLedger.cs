using System;

namespace TexasPortal.Domain.Entities;

public class HeadLedger
{
    public long Id { get; set; }
    public long HeadId { get; set; }
    public string ServiceType { get; set; } = null!;
    public long ServiceId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Head Head { get; set; } = null!;
}
