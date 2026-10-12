using System;

namespace TexasPortal.Domain.Entities;

public class Dispute
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSDISP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long UserId { get; set; }
    public long? HeadId { get; set; }
    public long DisputableId { get; set; }
    public string DisputableType { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = "open"; // open, resolved, rejected
    public string? Resolution { get; set; }
    public string? ResolutionNote { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsRefund { get; set; }
    public decimal? RefundAmount { get; set; }
    public decimal? ApprovedRefundAmount { get; set; }
    public bool RefundProcessed { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime? RefundDate { get; set; }
    public string? Iban { get; set; }
    public string? Name { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Head? Head { get; set; }
}
