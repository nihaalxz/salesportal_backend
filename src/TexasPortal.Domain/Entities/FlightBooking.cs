using System;
using System.Collections.Generic;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class FlightBooking : IUserOwned
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSFLT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public long? HeadId { get; set; }
    public string? ClientType { get; set; }
    public string? Agent { get; set; }
    public long? AgentId { get; set; }
    public string? CareOf { get; set; }
    public string? Supplier { get; set; }
    public bool IsInhouse { get; set; } = true;
    public bool IsDealClosed { get; set; } = false;
    public bool OperationClosed { get; set; } = false;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string FromLocation { get; set; } = null!;
    public string ToLocation { get; set; } = null!;
    public DateTime? TravelDate { get; set; }
    public string? AirlineCompany { get; set; }
    public string? TicketPnr { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public long? BankId { get; set; }
    public string? BankName { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string ProcessingStatus { get; set; } = "pending";
    public decimal ExpectedSupplierCost { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal Profit { get; set; }
    public string? PaymentProof { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Head? Head { get; set; }
    public ICollection<FlightPassenger> Passengers { get; set; } = new List<FlightPassenger>();
}
