using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Leads;

namespace TexasPortal.Application.Sales.Leads.CreateLead;

public enum CreateLeadResultStatus
{
    Success,
    ValidationError
}

public sealed class CreateLeadResult
{
    public CreateLeadResultStatus Status { get; }
    public LeadDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }

    private CreateLeadResult(
        CreateLeadResultStatus status,
        LeadDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
    }

    public static CreateLeadResult Success(LeadDetailResponse response) =>
        new(CreateLeadResultStatus.Success, response: response);

    public static CreateLeadResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateLeadResultStatus.ValidationError, errors: errors);
}
