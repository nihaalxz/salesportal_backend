using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Disputes;

namespace TexasPortal.Application.Sales.Disputes.ResolveDispute;

public enum ResolveDisputeResultStatus
{
    Success,
    NotFound,
    AlreadyResolved,
    ValidationError
}

public sealed class ResolveDisputeResult
{
    public ResolveDisputeResultStatus Status { get; set; }
    public DisputeResponse? Response { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ResolveDisputeResult Success(DisputeResponse response) =>
        new() { Status = ResolveDisputeResultStatus.Success, Response = response };

    public static ResolveDisputeResult NotFound() =>
        new() { Status = ResolveDisputeResultStatus.NotFound };

    public static ResolveDisputeResult AlreadyResolved() =>
        new() { Status = ResolveDisputeResultStatus.AlreadyResolved };

    public static ResolveDisputeResult ValidationError(Dictionary<string, string[]> errors) =>
        new() { Status = ResolveDisputeResultStatus.ValidationError, Errors = errors };
}
