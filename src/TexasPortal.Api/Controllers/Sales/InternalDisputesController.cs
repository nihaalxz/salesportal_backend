using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Sales.Disputes.ResolveDispute;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Disputes;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Route("api/internal/disputes")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "OperationsServiceAccess")]
[Produces("application/json")]
public sealed class InternalDisputesController : ControllerBase
{
    private readonly IOperationsReadService _readService;
    private readonly ResolveDisputeHandler _resolveDisputeHandler;

    public InternalDisputesController(
        IOperationsReadService readService,
        ResolveDisputeHandler resolveDisputeHandler)
    {
        _readService = readService;
        _resolveDisputeHandler = resolveDisputeHandler;
    }

    /// <summary>Queries all disputes across all sales users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DisputeResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDisputes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100");

        var result = await _readService.GetDisputesAsync(page, pageSize, status, search, sort, ct);
        return Ok(result);
    }

    /// <summary>Gets dashboard summary statistics for disputes.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DisputesDashboardResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        var result = await _readService.GetDisputesDashboardAsync(ct);
        return Ok(result);
    }

    /// <summary>Gets dispute details by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(DisputeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDispute([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _readService.GetDisputeByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Submits an approval or rejection decision for a dispute.</summary>
    [HttpPost("{id:long}/decision")]
    [ProducesResponseType(typeof(DisputeResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> SubmitDecision(
        [FromRoute] long id,
        [FromBody] DisputeDecisionRequest request,
        [FromQuery] string? resolvedBy = null,
        CancellationToken ct = default)
    {
        var decision = (request.Decision ?? "").Trim().ToLowerInvariant();
        var action = (decision == "approve" || decision == "accepted" || decision == "accept")
            ? "accepted"
            : "rejected";

        var resolveRequest = new ResolveDisputeRequest(
            Action: action,
            Resolution: request.Resolution,
            ResolvedBy: string.IsNullOrWhiteSpace(resolvedBy) ? "Accounts Portal" : resolvedBy,
            ApprovedRefundAmount: request.ApprovedRefundAmount
        );

        var result = await _resolveDisputeHandler.HandleAsync(new ResolveDisputeCommand(0, id, resolveRequest), ct);

        if (result.Status == ResolveDisputeResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResolveDisputeResultStatus.AlreadyResolved)
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid Operation",
                Detail = "This dispute case has already been resolved or rejected."
            });

        if (result.Status == ResolveDisputeResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Detail = "Please correct validation errors.",
                Extensions = { ["errors"] = result.Errors }
            });

        return Ok(result.Response);
    }
}
