using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Sales.Dashboard;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly ISalesPortalDbContext _db;

    public DashboardController(ISalesReadService salesReadService, ICurrentUser currentUser, ISalesPortalDbContext db)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _db = db;
    }

    /// <summary>Gets the authenticated salesman's dashboard summary.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetDashboardSummaryAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>Gets a 7-day rolling daily booking value trend for the authenticated salesman.</summary>
    [HttpGet("trend")]
    [ProducesResponseType(typeof(DashboardTrendResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetTrend(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetDashboardTrendAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>Gets dedicated current-month volume and profit target metrics for the authenticated salesman's profile.</summary>
    [HttpGet("profile/target-metrics")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetProfileTargetMetrics(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetProfileTargetMetricsAsync(userId, ct);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        return Ok(new
        {
            result.MonthlyVolume,
            result.MonthlyProfit,
            result.AllTimeVolume,
            result.AllTimeProfit,
            MonthlyTarget = user?.MonthlyTarget ?? 0m,
            ProfitTarget = user?.ProfitTarget ?? 0m
        });
    }
}
