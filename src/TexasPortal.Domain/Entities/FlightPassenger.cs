using System;

namespace TexasPortal.Domain.Entities;

public class FlightPassenger
{
    public long Id { get; set; }
    public long FlightBookingId { get; set; }
    public string? Prefix { get; set; }
    public string PassengerName { get; set; } = null!;
    public decimal PayableAmount { get; set; }
    public decimal SupplierCost { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public bool IsInhouse { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? TicketPnr { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public FlightBooking FlightBooking { get; set; } = null!;
}
