using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Heads;

namespace TexasPortal.Application.Sales.Heads.UpdateHead;

public enum UpdateHeadResultStatus
{
    Success,
    ValidationError,
    NotFound
}

public sealed class UpdateHeadResult
{
    public UpdateHeadResultStatus Status { get; }
    public HeadDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }

    private UpdateHeadResult(
        UpdateHeadResultStatus status,
        HeadDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
    }

    public static UpdateHeadResult Success(HeadDetailResponse response) =>
        new(UpdateHeadResultStatus.Success, response: response);

    public static UpdateHeadResult ValidationError(Dictionary<string, string[]> errors) =>
        new(UpdateHeadResultStatus.ValidationError, errors: errors);

    public static UpdateHeadResult NotFound() =>
        new(UpdateHeadResultStatus.NotFound);
}
