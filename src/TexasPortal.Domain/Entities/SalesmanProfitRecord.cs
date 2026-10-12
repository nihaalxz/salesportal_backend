using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class SalesmanProfitRecord
{
    public long Id { get; set; }
    public long SalesmanId { get; set; }
    public string BookingType { get; set; } = null!;
    public long BookingId { get; set; }
    public string? DocumentNumber { get; set; }

    public decimal PayableAmount { get; set; }
    public decimal FinalSupplierCost { get; set; }
    public decimal ProfitAmount { get; set; }

    public ProfitType ProfitType { get; set; } = ProfitType.Normal;
    public SupplierCostSource SupplierCostSource { get; set; } = SupplierCostSource.Estimated;

    public decimal? AccountsApprovedTotal { get; set; }
    public decimal? RejectionFee { get; set; }
    public bool IsFinal { get; set; }

    public DateTime BookingCreatedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? LastReconciledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? Salesman { get; set; }
}
