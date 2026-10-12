using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

public sealed record VfsApplicantRequest(
    string? Prefix,
    string Name,
    string? PassportCopy = null,
    decimal ExpectedSupplierCost = 0m,
    decimal Amount = 0m,
    bool IsInhouse = true,
    long? SupplierId = null
);

public sealed record VfsApplicantResponse(
    long Id,
    string? Prefix,
    string Name,
    string? PassportCopy,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    long? SupplierId,
    string? SupplierName
);

/// <summary>
/// Request contract for creating a VFS appointment booking.
/// </summary>
public sealed record CreateVfsAppointmentRequest(
    string ClientType,
    long? HeadId,
    string? Agent,
    long? AgentId,
    string Prefix,
    string Name,
    long? SupplierId,
    string Country,
    string VisaType,
    decimal ExpectedSupplierCost,
    decimal Amount, // PayableAmount
    string PaymentMethod, // "cash" | "wallet" | "credit" | "online"
    long? BankId = null,
    string? BankName = null,
    string? PaymentProof = null,
    DateTime? AppointmentDate = null,
    TimeSpan? AppointmentTime = null,
    string? VfsReceipt = null,
    string? PassportCopy = null,
    bool IsInhouse = false,
    List<VfsApplicantRequest>? Applicants = null
);

/// <summary>
/// Response contract for a successfully created VFS appointment booking.
/// </summary>
public sealed record VfsAppointmentResponse(
    long Id,
    long CreatedBy,
    long? HeadId,
    string ClientType,
    string? Agent,
    long? AgentId,
    string Prefix,
    string Name,
    long? SupplierId,
    string Country,
    string VisaType,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    string PaymentMode,
    string? PaymentProof,
    string PaymentStatus,
    string ProcessingStatus,
    DateTime? AppointmentDate,
    TimeSpan? AppointmentTime,
    string? VfsReceipt,
    string? PassportCopy,
    DateTime CreatedAt,
    string? DocumentNumber = null,
    List<VfsApplicantResponse>? Applicants = null
);
