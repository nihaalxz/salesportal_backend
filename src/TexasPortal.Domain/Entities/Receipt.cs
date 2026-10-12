using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class Receipt
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSRCPT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long? CreatedBy { get; set; }
    public string ReceiptId { get; set; } = null!;
    public long? HeadId { get; set; }
    public string? BookingId { get; set; }
    public string Narration { get; set; } = null!;
    public string Service { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = null!; // cash, online, wallet
    public string? PaymentProof { get; set; }
    public string Status { get; set; } = "pending"; // pending, collection, collected, success
    public string? PaymentReference { get; set; }
    public long? BankId { get; set; }
    public string? BankName { get; set; }
    public long? AgentId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Typed discriminator replacing Service string checks in business logic.</summary>
    public ReceiptType ReceiptType { get; set; } = ReceiptType.General;
    /// <summary>Optimistic concurrency token. Auto-managed by SQL Server rowversion.</summary>
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public User? Creator { get; set; }
    public Head? Head { get; set; }
}
