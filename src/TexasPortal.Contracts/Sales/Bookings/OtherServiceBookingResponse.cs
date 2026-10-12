using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

public sealed record OtherServiceBookingResponse(
    long Id,
    long CreatedBy,
    long? HeadId,
    string ClientType,
    string Name,
    string ServiceName,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    string PaymentMethod,
    string? PaymentProof,
    string PaymentStatus,
    string ProcessingStatus,
    DateTime CreatedAt,
    string? DocumentNumber = null,
    List<OtherServiceApplicantResponse>? Applicants = null
);
