using System;
using System.Collections.Generic;

namespace TexasPortal.Domain.Entities;

public class SalesVisaApplicant
{
    public long Id { get; set; }
    public long SalesVisaApplicationId { get; set; }
    public string? VisaAppId { get; set; }
    public string? Prefix { get; set; }
    public string? FName { get; set; }
    public string? LName { get; set; }
    public DateTime? Dob { get; set; }
    public string? Nationality { get; set; }
    public string? CareOf { get; set; }
    public string? ContactNo { get; set; }
    public string? EmailId { get; set; }
    public string? PassportNo { get; set; }
    public string? IqamaHotelNo { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal? UnpaidAmount { get; set; }
    public string? BankDoc { get; set; }
    public string? PassportCopy { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string? PaymentProof { get; set; }
    public long? ProcessedBy { get; set; }
    public long? PaymentConfirmedBy { get; set; }
    public decimal SupplierCost { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public bool IsInhouse { get; set; }
    public long? BankId { get; set; }
    public string? BankName { get; set; }
    public decimal? GovtFeeAmount { get; set; }
    public string? GovtFeeReference { get; set; }
    public string GovtFeeStatus { get; set; } = "pending";
    public string? GovtFeePaymentProof { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public SalesVisaApplication SalesVisaApplication { get; set; } = null!;
    public ICollection<SalesVisaGovtFee> GovtFees { get; set; } = new List<SalesVisaGovtFee>();
}
