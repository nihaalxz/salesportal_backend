using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Application.Sales.Leads.CreateLead;
using TexasPortal.Application.Sales.Leads.UpdateLead;
using TexasPortal.Application.Sales.Leads.ArchiveLead;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/leads")]
[Produces("application/json")]
public sealed class LeadsController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly CreateLeadHandler _createLeadHandler;
    private readonly UpdateLeadHandler _updateLeadHandler;
    private readonly ArchiveLeadHandler _archiveLeadHandler;

    public LeadsController(
        ISalesReadService salesReadService, 
        ICurrentUser currentUser,
        CreateLeadHandler createLeadHandler,
        UpdateLeadHandler updateLeadHandler,
        ArchiveLeadHandler archiveLeadHandler)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _createLeadHandler = createLeadHandler;
        _updateLeadHandler = updateLeadHandler;
        _archiveLeadHandler = archiveLeadHandler;
    }

    /// <summary>Lists leads owned by the authenticated salesman.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<LeadSummaryResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetLeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100");

        var allowedSortBy = new[] { "name", "status", "createdAt" };
        if (!allowedSortBy.Contains(sortBy, System.StringComparer.OrdinalIgnoreCase))
            return BadRequest($"sortBy must be one of: {string.Join(", ", allowedSortBy)}");

        var query = new LeadsListQuery(page, pageSize, searchTerm, status, sortBy, sortOrder);
        var result = await _salesReadService.GetLeadsAsync(userId, query, ct);
        return Ok(result);
    }

    /// <summary>Gets details for a specific lead owned by the authenticated salesman.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(LeadDetailResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetLead([FromRoute] long id, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetLeadByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a new sales lead.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LeadDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _createLeadHandler.HandleAsync(new CreateLeadCommand(userId, request), ct);
        if (result.Status == CreateLeadResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Detail = "Please correct validation errors.",
                Extensions = { ["errors"] = result.Errors }
            });

        return CreatedAtAction(nameof(GetLead), new { id = result.Response!.Id }, result.Response);
    }

    /// <summary>Updates an existing sales lead.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(LeadDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateLead([FromRoute] long id, [FromBody] UpdateLeadRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _updateLeadHandler.HandleAsync(new UpdateLeadCommand(userId, id, request), ct);
        if (result.Status == UpdateLeadResultStatus.NotFound)
            return NotFound();

        if (result.Status == UpdateLeadResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Detail = "Please correct validation errors.",
                Extensions = { ["errors"] = result.Errors }
            });

        return Ok(result.Response);
    }

    /// <summary>Archives (soft-deletes) a sales lead.</summary>
    [HttpDelete("{id:long}")] // Using standard DELETE for archive/destroy mapping to match Laravel destroy routes
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteLead([FromRoute] long id, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _archiveLeadHandler.HandleAsync(new ArchiveLeadCommand(userId, id), ct);
        if (result.Status == ArchiveLeadResultStatus.NotFound)
            return NotFound();

        return NoContent();
    }
}
