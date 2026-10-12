using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Heads;

namespace TexasPortal.Application.Sales.Heads.CreateHead;

public enum CreateHeadResultStatus
{
    Success,
    ValidationError
}

public sealed class CreateHeadResult
{
    public CreateHeadResultStatus Status { get; }
    public HeadDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }

    private CreateHeadResult(
        CreateHeadResultStatus status,
        HeadDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
    }

    public static CreateHeadResult Success(HeadDetailResponse response) =>
        new(CreateHeadResultStatus.Success, response: response);

    public static CreateHeadResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateHeadResultStatus.ValidationError, errors: errors);
}
