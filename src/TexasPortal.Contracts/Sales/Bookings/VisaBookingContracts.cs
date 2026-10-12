using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

/// <summary>
/// Request payload for a visa applicant.
/// </summary>
public sealed record VisaApplicantRequest(
    string? Prefix,
    string? FName,
    string? LName,
    DateTime? Dob,
    string? Nationality,
    string? CareOf,
    string? ContactNo,
    string? EmailId,
    string? PassportNo,
    string? IqamaHotelNo,
    decimal Amount, // selling price
    decimal ExpectedSupplierCost,
    bool IsInhouse,
    string? PassportCopy = null,
    string? BankDoc = null
);

/// <summary>
/// Request contract for creating a new Visa Booking.
/// </summary>
public sealed record CreateVisaBookingRequest(
    string ClientType, // "customer" | "agent"
    long? HeadId,
    string? Agent,
    long? AgentId,
    int CountryId,
    int VisaPackId,
    string? VisaPackName,
    string PaymentMethod, // "cash" | "wallet" | "credit" | "online"
    long? BankId,
    string? BankName,
    string? PaymentProof,
    string? PaymentReference,
    long? SupplierId, // booking-level supplier
    List<VisaApplicantRequest> Applicants,
    bool IsInhouse = false
);

/// <summary>
/// Response returned for a visa applicant.
/// </summary>
public sealed record VisaApplicantResponse(
    long Id,
    string? Prefix,
    string? FName,
    string? LName,
    DateTime? Dob,
    string? Nationality,
    string? CareOf,
    string? ContactNo,
    string? EmailId,
    string? PassportNo,
    string? IqamaHotelNo,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    string? PaymentMethod,
    string PaymentStatus,
    string? PaymentProof,
    string? PaymentReference,
    long? BankId,
    string? BankName,
    string? PassportCopy = null,
    string? BankDoc = null,
    string? GovtFeeReference = null
);

/// <summary>
/// Response returned after successfully creating a visa booking.
/// </summary>
public sealed record VisaBookingResponse(
    long Id,
    string DocumentNumber,
    long UserId,
    int CountryId,
    int VisaPackId,
    string? VisaPackName,
    string ClientType,
    string? Agent,
    long? AgentId,
    long? HeadId,
    string PaymentStatus,
    string ProcessingStatus,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    long? SupplierId,
    string? SupplierName,
    List<VisaApplicantResponse> Applicants,
    DateTime CreatedAt
);

public sealed record VisaCountryMasterResponse(
    long Id,
    string Name,
    string? Code,
    bool IsActive,
    int PackagesCount
);

public sealed record VisaPackageMasterResponse(
    long Id,
    long CountryId,
    string CountryName,
    string Name,
    string? Description,
    bool IsActive
);


/// <summary>
/// Request contract for updating an applicant's details from Operational Portal.
/// </summary>
public sealed record UpdateVisaApplicantDetailsRequest(
    string? Prefix,
    string? FName,
    string? LName,
    DateTime? Dob,
    string? Nationality,
    string? PassportNo,
    string? ContactNo,
    string? EmailId
);
