using System;

namespace TexasPortal.Domain.Entities;

public class VfsApplicant
{
    public long Id { get; set; }
    public long VfsAppointmentId { get; set; }
    public string? Prefix { get; set; }
    public string Name { get; set; } = null!;
    public string? PassportCopy { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal SupplierCost { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public bool IsInhouse { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public VfsAppointment VfsAppointment { get; set; } = null!;
}
