using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Disputes.ResolveDispute;
using TexasPortal.Application.Sales.Disputes.CreateDispute;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Disputes;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/disputes")]
[Produces("application/json")]
public sealed class DisputesController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly ResolveDisputeHandler _resolveDisputeHandler;
    private readonly CreateDisputeHandler _createDisputeHandler;

    public DisputesController(
        ISalesReadService salesReadService,
        ICurrentUser currentUser,
        ResolveDisputeHandler resolveDisputeHandler,
        CreateDisputeHandler createDisputeHandler)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _resolveDisputeHandler = resolveDisputeHandler;
        _createDisputeHandler = createDisputeHandler;
    }

    /// <summary>Lists disputes for the authenticated salesman.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DisputeResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDisputes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100");

        var query = new DisputesListQuery(page, pageSize, status, searchTerm);
        var result = await _salesReadService.GetDisputesAsync(userId, query, ct);
        return Ok(result);
    }

    /// <summary>Gets details for a specific dispute.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(DisputeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDispute([FromRoute] long id, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetDisputeByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets eligible booking options for dispute creation by service type.</summary>
    [HttpGet("booking-options")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<DisputeBookingOptionResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetBookingOptions([FromQuery] string serviceType, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetDisputeBookingOptionsAsync(userId, serviceType, ct);
        return Ok(result);
    }

    /// <summary>Creates a new dispute case.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DisputeResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { error = "Subject is required." });
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { error = "Description is required." });
        if (string.IsNullOrWhiteSpace(request.DisputableType))
            return BadRequest(new { error = "Service type is required." });

        var result = await _createDisputeHandler.HandleAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetDispute), new { id = result.Id }, result);
    }

    /// <summary>Resolves (accepts or rejects) a dispute case.</summary>
    [HttpPost("{id:long}/resolve")]
    [ProducesResponseType(typeof(DisputeResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ResolveDispute([FromRoute] long id, [FromBody] ResolveDisputeRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _resolveDisputeHandler.HandleAsync(new ResolveDisputeCommand(userId, id, request), ct);

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
