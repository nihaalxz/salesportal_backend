using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Cash;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/cash")]
[Produces("application/json")]
public sealed class CashManagementController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;

    public CashManagementController(ISalesReadService salesReadService, ICurrentUser currentUser)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
    }

    /// <summary>Gets the authenticated salesman's current cash-in-hand balance.</summary>
    [HttpGet("in-hand")]
    [ProducesResponseType(typeof(CashInHandResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCashInHand(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetCashInHandAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets the authenticated salesman's bank deposit history.</summary>
    [HttpGet("deposits")]
    [ProducesResponseType(typeof(PagedResponse<DepositSummaryResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDeposits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100");

        var result = await _salesReadService.GetDepositsAsync(userId, new(page, pageSize, status), ct);
        return Ok(result);
    }
}
