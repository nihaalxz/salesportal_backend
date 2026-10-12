using System;

namespace TexasPortal.Contracts.Sales.Heads;

public sealed class OutstandingBookingResponse
{
    public long Id { get; init; }
    public string ServiceType { get; init; } = null!; // Flight, Hotel, Visa, Vfs, Holiday, Transport, Other
    public string ReferenceCode { get; init; } = null!;
    public string Description { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public decimal PayableAmount { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
