using System;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class HotelBooking : IUserOwned
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSHOTL-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public long? HeadId { get; set; }
    public string? ClientType { get; set; }
    public string? Agent { get; set; }
    public long? AgentId { get; set; }
    public bool IsInhouse { get; set; }
    public bool IsDealClosed { get; set; } = false;
    public bool OperationClosed { get; set; } = false;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal Profit { get; set; }
    public string? Prefix { get; set; }
    public string GuestName { get; set; } = null!;
    public string? CareOf { get; set; }
    public string? HotelName { get; set; }
    public string? Supplier { get; set; }
    public string Country { get; set; } = null!;
    public int Days { get; set; }
    public DateTime CheckInDate { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string PaymentStatus { get; set; } = "pending";
    public string ProcessingStatus { get; set; } = "pending";
    public string OrderStatus { get; set; } = "pending";
    public string? PaymentProof { get; set; }
    public string? PaymentReference { get; set; }
    public long? BankId { get; set; }
    public string? BankName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Head? Head { get; set; }
    public System.Collections.Generic.ICollection<HotelGuest> Guests { get; set; } = new System.Collections.Generic.List<HotelGuest>();
}
