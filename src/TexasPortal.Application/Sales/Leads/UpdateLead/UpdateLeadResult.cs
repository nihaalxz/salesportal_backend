using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Leads;

namespace TexasPortal.Application.Sales.Leads.UpdateLead;

public enum UpdateLeadResultStatus
{
    Success,
    ValidationError,
    NotFound
}

public sealed class UpdateLeadResult
{
    public UpdateLeadResultStatus Status { get; }
    public LeadDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }

    private UpdateLeadResult(
        UpdateLeadResultStatus status,
        LeadDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
    }

    public static UpdateLeadResult Success(LeadDetailResponse response) =>
        new(UpdateLeadResultStatus.Success, response: response);

    public static UpdateLeadResult ValidationError(Dictionary<string, string[]> errors) =>
        new(UpdateLeadResultStatus.ValidationError, errors: errors);

    public static UpdateLeadResult NotFound() =>
        new(UpdateLeadResultStatus.NotFound);
}
