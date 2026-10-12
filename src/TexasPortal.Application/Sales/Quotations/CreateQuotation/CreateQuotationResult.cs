using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Quotations;

namespace TexasPortal.Application.Sales.Quotations.CreateQuotation;

public enum CreateQuotationResultStatus
{
    Success,
    ValidationError,
    HeadNotFound
}

public sealed class CreateQuotationResult
{
    public CreateQuotationResultStatus Status { get; }
    public QuotationDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }

    private CreateQuotationResult(
        CreateQuotationResultStatus status,
        QuotationDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
    }

    public static CreateQuotationResult Success(QuotationDetailResponse response) =>
        new(CreateQuotationResultStatus.Success, response: response);

    public static CreateQuotationResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateQuotationResultStatus.ValidationError, errors: errors);

    public static CreateQuotationResult HeadNotFound() =>
        new(CreateQuotationResultStatus.HeadNotFound);
}
