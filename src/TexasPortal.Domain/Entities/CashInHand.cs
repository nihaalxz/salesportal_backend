using System;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class CashInHand : IUserOwned
{
    public long Id { get; set; }
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public decimal Amount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
