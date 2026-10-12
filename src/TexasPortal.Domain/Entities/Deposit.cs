using System;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class Deposit : IUserOwned
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSDEP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public string Type { get; set; } = null!;
    public long DestinationId { get; set; }
    public string? Narration { get; set; }
    public DateTime Date { get; set; }
    public string? PaymentProof { get; set; }
    public string? ApiTransactionId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
