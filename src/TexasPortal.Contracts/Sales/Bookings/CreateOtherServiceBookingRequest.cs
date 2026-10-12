using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TexasPortal.Contracts.Sales.Bookings;

public sealed record OtherServiceApplicantRequest(
    string? Prefix,
    string Name,
    decimal ExpectedSupplierCost = 0m,
    decimal Amount = 0m,
    bool IsInhouse = true,
    long? SupplierId = null
);

public sealed record OtherServiceApplicantResponse(
    long Id,
    string? Prefix,
    string Name,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    long? SupplierId,
    string? SupplierName
);

public sealed record CreateOtherServiceBookingRequest(
    [Required] string ClientType, // customer, agent
    long? HeadId,
    string? Prefix,
    [Required] string Name,
    string? Agent,
    long? AgentId,
    bool IsInhouse,
    long? SupplierId,
    decimal ExpectedSupplierCost,
    [Required] decimal Amount,
    [Required] string ServiceName,
    string? Narration,
    [Required] string PaymentMethod, // credit, wallet, cash, online
    long? BankId,
    string? BankName,
    [Required] string Status, // pending, completed
    string? PaymentProof,
    List<OtherServiceApplicantRequest>? Applicants = null
);
