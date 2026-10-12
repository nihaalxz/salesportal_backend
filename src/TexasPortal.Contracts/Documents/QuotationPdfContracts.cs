using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Documents;

public sealed record QuotationPdfItemRequest(
    string ServiceType,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount
);

public sealed record QuotationPdfRequest(
    string? QuotationNo,
    DateTime? QuotationDate,
    DateTime? ValidUntil,
    string ClientType,
    long? HeadId,
    string CustomerName,
    string? CustomerContact,
    string? CustomerEmail,
    string? SalesmanName,
    string? RemarksNote,
    List<QuotationPdfItemRequest> Items
);
