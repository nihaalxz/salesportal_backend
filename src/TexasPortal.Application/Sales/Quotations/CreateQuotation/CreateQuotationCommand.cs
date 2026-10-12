using TexasPortal.Contracts.Sales.Quotations;

namespace TexasPortal.Application.Sales.Quotations.CreateQuotation;

public sealed record CreateQuotationCommand(
    long UserId,
    CreateQuotationRequest Request
);
