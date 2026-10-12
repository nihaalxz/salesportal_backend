using System;
using System.Collections.Generic;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class HolidayPackage : ICreatorOwned
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = "TXSHOL-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public long CreatedBy { get; set; }
    long? ICreatorOwned.CreatedBy => CreatedBy;
    public long? HeadId { get; set; }
    public string? ClientType { get; set; }
    public string? Agent { get; set; }
    public long? AgentId { get; set; }
    public string Prefix { get; set; } = null!;
    public string PassengerName { get; set; } = null!;
    public string Supplier { get; set; } = null!;
    public bool IsInhouse { get; set; } = true;
    public bool IsDealClosed { get; set; } = false;
    public bool OperationClosed { get; set; } = false;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string Country { get; set; } = null!;
    public int Days { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public string PaymentMode { get; set; } = null!;
    public long? BankId { get; set; }
    public string? BankName { get; set; }
    public string? PaymentProof { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string ProcessingStatus { get; set; } = "pending";
    public DateTime? DeletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User Creator { get; set; } = null!;
    public Head? Head { get; set; }
    public ICollection<HolidayPassenger> Passengers { get; set; } = new List<HolidayPassenger>();
    public ICollection<HolidayPackageGovtFee> GovtFees { get; set; } = new List<HolidayPackageGovtFee>();
    public ICollection<HolidayPackageDocument> Documents { get; set; } = new List<HolidayPackageDocument>();
}
