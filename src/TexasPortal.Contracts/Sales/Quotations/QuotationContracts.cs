using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Quotations;

public sealed record QuotationItemDto(
    long? Id,
    string ServiceType,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount
);

public sealed record CreateQuotationRequest(
    string? ClientType,
    long? HeadId,
    string CustomerName,
    string? CustomerContact,
    string? CustomerEmail,
    string? Remarks,
    List<QuotationItemDto> Items
);

public sealed record QuotationSummaryResponse(
    long Id,
    string DocumentNumber,
    long UserId,
    string SalesmanName,
    long? HeadId,
    string ClientType,
    string CustomerName,
    string? CustomerContact,
    string? CustomerEmail,
    int ItemsCount,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt
);

public sealed record QuotationDetailResponse(
    long Id,
    string DocumentNumber,
    long UserId,
    string SalesmanName,
    long? HeadId,
    string ClientType,
    string CustomerName,
    string? CustomerContact,
    string? CustomerEmail,
    decimal TotalAmount,
    string Status,
    string? Remarks,
    DateTime CreatedAt,
    List<QuotationItemDto> Items
);

public sealed record UpdateQuotationStatusRequest(
    string Status,
    string? Note = null
);
