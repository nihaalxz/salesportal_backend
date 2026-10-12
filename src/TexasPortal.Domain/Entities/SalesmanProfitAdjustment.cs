using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class SalesmanProfitAdjustment
{
    public long Id { get; set; }
    public long SalesmanProfitRecordId { get; set; }
    public long SalesmanId { get; set; }
    public string BookingType { get; set; } = null!;
    public long BookingId { get; set; }
    public string? DocumentNumber { get; set; }

    public SalesmanProfitAdjustmentType AdjustmentType { get; set; }
    public decimal Amount { get; set; }

    public string ReferenceType { get; set; } = null!;
    public string ReferenceId { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;

    public string Description { get; set; } = null!;

    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public SalesmanProfitRecord? SalesmanProfitRecord { get; set; }
}
