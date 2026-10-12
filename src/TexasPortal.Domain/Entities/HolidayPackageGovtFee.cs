using System;

namespace TexasPortal.Domain.Entities;

public class HolidayPackageGovtFee
{
    public long Id { get; set; }
    public string RowKey { get; set; } = Guid.NewGuid().ToString("D");
    public long HolidayPackageId { get; set; }
    public long? AccountsPaymentId { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentReference { get; set; }
    public string Status { get; set; } = null!;
    public string? PaymentProof { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public HolidayPackage HolidayPackage { get; set; } = null!;
}
