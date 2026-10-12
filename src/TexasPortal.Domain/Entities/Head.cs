using System;
using System.Collections.Generic;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class Head : ICreatorOwned
{
    public long Id { get; set; }
    public long? CreatedBy { get; set; }
    public string? UserId { get; set; }
    public string Type { get; set; } = "customer";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string MobileNo { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Nationality { get; set; } = null!;
    /// <summary>
    /// Customer running account balance (mapped to underlying Wallet DB column).
    /// </summary>
    public decimal Wallet { get; set; }

    /// <summary>
    /// Domain alias for Wallet representing customer prepaid balance.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal Balance
    {
        get => Wallet;
        set => Wallet = value;
    }

    public string Address { get; set; } = null!;
    public string Status { get; set; } = "active";
    public DateTime? DeletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? Creator { get; set; }
    public ICollection<HeadLedger> Ledgers { get; set; } = new List<HeadLedger>();
    public ICollection<BalanceTransaction> BalanceTransactions { get; set; } = new List<BalanceTransaction>();
}
