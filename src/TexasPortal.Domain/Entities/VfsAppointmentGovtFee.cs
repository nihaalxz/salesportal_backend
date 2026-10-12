using System;

namespace TexasPortal.Domain.Entities;

public class VfsAppointmentGovtFee
{
    public long Id { get; set; }
    public string RowKey { get; set; } = Guid.NewGuid().ToString("D");
    public long VfsAppointmentId { get; set; }
    public long? AccountsPaymentId { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentReference { get; set; }
    public string Status { get; set; } = null!;
    public string? PaymentProof { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public VfsAppointment VfsAppointment { get; set; } = null!;
}
