using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Performance;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/performance")]
[Produces("application/json")]
public sealed class PerformanceController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;

    public PerformanceController(ISalesReadService salesReadService, ICurrentUser currentUser)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
    }

    /// <summary>Gets monthly performance records for the authenticated salesman.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PerformanceSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] int? year = null,
        [FromQuery] string? month = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetPerformanceRecordsAsync(userId, new(year, month), ct);
        return Ok(result);
    }
}
