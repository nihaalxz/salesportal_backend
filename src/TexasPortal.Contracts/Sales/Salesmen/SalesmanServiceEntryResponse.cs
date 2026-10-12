using System;

namespace TexasPortal.Contracts.Sales.Salesmen;

public sealed class SalesmanServiceEntryResponse
{
    public long Id { get; set; }
    public string ServiceType { get; set; } = null!;
    public string DocumentNumber { get; set; } = null!;
    public string? CustomerHead { get; set; }
    public string? BeneficiaryName { get; set; }
    public string? Details { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public string? ProcessingStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
