using System;
using System.Collections.Generic;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class SalesVisaApplication : IUserOwned
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSVISA-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public int CountryId { get; set; }
    public int VisaPackId { get; set; }
    public string? VisaPackName { get; set; }
    public string ClientType { get; set; } = null!;
    public string? Agent { get; set; }
    public long? AgentId { get; set; }
    public long? HeadId { get; set; }
    public string ProcessingStatus { get; set; } = "pending";
    public string PaymentStatus { get; set; } = "pending";
    public string? VisaDocument { get; set; }
    public string? ApprovalNote { get; set; }
    public string? RejectionDocument { get; set; }
    public string? RejectionNote { get; set; }
    public string? RefundNote { get; set; }
    public string? DocumentRequestNote { get; set; }
    public DateTime? DocumentRequestDate { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public bool IsInhouse { get; set; } = true;
    public bool IsDealClosed { get; set; } = false;
    public bool OperationClosed { get; set; } = false;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal RejectionFee { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Head? Head { get; set; }
    public ICollection<SalesVisaApplicant> Applicants { get; set; } = new List<SalesVisaApplicant>();
}
