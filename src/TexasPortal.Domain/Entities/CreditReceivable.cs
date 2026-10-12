using System;
using System.Collections.Generic;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class CreditReceivable
{
    public long Id { get; set; }
    public string BookingId { get; set; } = null!;
    public string BookingNumber { get; set; } = null!;
    public BookingModule BookingModule { get; set; }
    public long? HeadId { get; set; }
    public HeadType HeadType { get; set; }
    public long? CustomerId { get; set; }
    public long? AgentId { get; set; }
    public long SalesmanId { get; set; }
    public string Currency { get; set; } = "BHD";
    public decimal TotalAmount { get; set; }
    public decimal CachedOutstandingAmount { get; set; }
    public ReceivableStatus Status { get; set; } = ReceivableStatus.Open;
    public ReceivablePaymentStatus PaymentStatus { get; set; } = ReceivablePaymentStatus.Pending;
    public CollectionPriority Priority { get; set; } = CollectionPriority.Low;
    public CreditTerm CreditTerm { get; set; } = CreditTerm.Immediate;
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedDate { get; set; }
    public FinancialSourceType FinancialSourceType { get; set; } = FinancialSourceType.Booking;
    public string? Remarks { get; set; }
    /// <summary>Optimistic concurrency token. Auto-managed by SQL Server rowversion.</summary>
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public virtual ICollection<ReceivableTransaction> Transactions { get; set; } = new List<ReceivableTransaction>();
    public virtual ICollection<ReceiptAllocation> Allocations { get; set; } = new List<ReceiptAllocation>();
}
