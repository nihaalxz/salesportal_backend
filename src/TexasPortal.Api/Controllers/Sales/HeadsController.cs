using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Application.Sales.Heads.CreateHead;
using TexasPortal.Application.Sales.Heads.UpdateHead;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/heads")]
[Produces("application/json")]
public sealed class HeadsController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly CreateHeadHandler _createHeadHandler;
    private readonly UpdateHeadHandler _updateHeadHandler;

    public HeadsController(
        ISalesReadService salesReadService, 
        ICurrentUser currentUser,
        CreateHeadHandler createHeadHandler,
        UpdateHeadHandler updateHeadHandler)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _createHeadHandler = createHeadHandler;
        _updateHeadHandler = updateHeadHandler;
    }

    private IActionResult UnauthorizedResult() => Unauthorized();

    /// <summary>Lists customer heads owned by the authenticated salesman.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<HeadSummaryResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetHeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "lastName",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? type = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100");

        var allowedSortBy = new[] { "firstName", "lastName", "wallet", "createdAt" };
        if (!allowedSortBy.Contains(sortBy, System.StringComparer.OrdinalIgnoreCase))
            return BadRequest($"sortBy must be one of: {string.Join(", ", allowedSortBy)}");

        var query = new HeadsListQuery(page, pageSize, searchTerm, sortBy, sortOrder, type);
        var result = await _salesReadService.GetHeadsAsync(userId, query, ct);
        return Ok(result);
    }

    /// <summary>Gets details for a specific customer head owned by the authenticated salesman.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(HeadDetailResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHead([FromRoute] long id, [FromQuery] string? type = null, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetHeadByIdAsync(userId, id, type, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a new customer head code-named TX-xxx.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HeadDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateHead([FromBody] CreateHeadRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _createHeadHandler.HandleAsync(new CreateHeadCommand(userId, request), ct);
        if (result.Status == CreateHeadResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Detail = "Please correct validation errors.",
                Extensions = { ["errors"] = result.Errors }
            });

        return CreatedAtAction(nameof(GetHead), new { id = result.Response!.Id }, result.Response);
    }

    /// <summary>Updates customer head details.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(HeadDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateHead([FromRoute] long id, [FromBody] UpdateHeadRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _updateHeadHandler.HandleAsync(new UpdateHeadCommand(userId, id, request), ct);
        if (result.Status == UpdateHeadResultStatus.NotFound)
            return NotFound();

        if (result.Status == UpdateHeadResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Detail = "Please correct validation errors.",
                Extensions = { ["errors"] = result.Errors }
            });

        return Ok(result.Response);
    }

    /// <summary>Gets the chronological ledger for a specific head owned by the authenticated salesman.</summary>
    [HttpGet("{id:long}/ledger")]
    [ProducesResponseType(typeof(PagedResponse<HeadLedgerEntryResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHeadLedger(
        [FromRoute] long id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] System.DateTime? startDate = null,
        [FromQuery] System.DateTime? endDate = null,
        [FromQuery] string? serviceType = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 200) return BadRequest("pageSize must be between 1 and 200");

        var result = await _salesReadService.GetHeadLedgerAsync(userId, id, page, pageSize, type, startDate, endDate, serviceType, search, sort, ct);
        return Ok(result);
    }

    /// <summary>Gets outstanding bookings for a head.</summary>
    [HttpGet("{id:long}/bookings/outstanding")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<OutstandingBookingResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetOutstandingBookings([FromRoute] long id, [FromQuery] string? type = null, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetOutstandingBookingsForHeadAsync(userId, id, type, ct);
        return Ok(result);
    }

    /// <summary>Looks up active heads by search term. Use type=agent for agency accounts, type=customer (default) for direct customers.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<HeadLookupResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LookupHeads(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? type = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (limit is < 1 or > 100) return BadRequest("limit must be between 1 and 100");

        var result = await _salesReadService.LookupHeadsAsync(userId, search, limit, type, ct);
        return Ok(result);
    }
}
