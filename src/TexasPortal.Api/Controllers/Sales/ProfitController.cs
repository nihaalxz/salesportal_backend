using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Profit;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize]
[Route("api/sales/profit")]
[Produces("application/json")]
public sealed class ProfitController : ControllerBase
{
    private readonly ISalesmanProfitService _profitService;
    private readonly ICurrentUser _currentUser;

    public ProfitController(ISalesmanProfitService profitService, ICurrentUser currentUser)
    {
        _profitService = profitService;
        _currentUser = currentUser;
    }

    /// <summary>Gets authoritative profit record and adjustment detail for a specific booking.</summary>
    [HttpGet("{bookingType}/{bookingIdOrRef}")]
    public async Task<IActionResult> GetBookingProfit(string bookingType, string bookingIdOrRef, CancellationToken ct)
    {
        var detail = await _profitService.GetProfitDetailAsync(bookingType, bookingIdOrRef, ct);
        if (detail == null) return NotFound(new { Message = "Profit record not found for specified booking." });
        return Ok(detail);
    }

    /// <summary>Reconciles profit record calculation against expected formula.</summary>
    [HttpGet("{bookingType}/{bookingIdOrRef}/reconcile")]
    public async Task<IActionResult> ReconcileProfit(string bookingType, string bookingIdOrRef, CancellationToken ct)
    {
        var result = await _profitService.ReconcileProfitDetailsAsync(bookingType, bookingIdOrRef, ct);
        return Ok(result);
    }

    /// <summary>Gets current salesman's total profit summary.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetProfitSummary([FromQuery] long? salesmanId, CancellationToken ct)
    {
        var targetSalesmanId = salesmanId ?? _currentUser.UserId;
        if (!targetSalesmanId.HasValue || targetSalesmanId.Value <= 0)
            return BadRequest(new { Message = "Valid SalesmanId is required." });

        var summary = await _profitService.GetProfitSummaryAsync(targetSalesmanId.Value, ct);
        return Ok(summary);
    }

    /// <summary>Gets monthly profit breakdown for a salesman.</summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyProfit([FromQuery] int? year, [FromQuery] int? month, [FromQuery] long? salesmanId, CancellationToken ct)
    {
        var targetSalesmanId = salesmanId ?? _currentUser.UserId;
        if (!targetSalesmanId.HasValue || targetSalesmanId.Value <= 0)
            return BadRequest(new { Message = "Valid SalesmanId is required." });

        var now = DateTime.UtcNow;
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;

        var monthlyProfit = await _profitService.GetMonthlyProfitAsync(targetSalesmanId.Value, targetYear, targetMonth, ct);
        return Ok(new
        {
            SalesmanId = targetSalesmanId.Value,
            Year = targetYear,
            Month = targetMonth,
            MonthlyProfit = monthlyProfit
        });
    }

    /// <summary>Gets all itemized profit records for a salesman with resolved client names.</summary>
    [HttpGet("records")]
    public async Task<IActionResult> GetSalesmanProfitRecords(
        [FromQuery] long? salesmanId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var targetSalesmanId = salesmanId ?? _currentUser.UserId;
        if (!targetSalesmanId.HasValue || targetSalesmanId.Value <= 0)
            return BadRequest(new { Message = "Valid SalesmanId is required." });

        var records = await _profitService.GetSalesmanProfitItemDtosAsync(targetSalesmanId.Value, startDate, endDate, ct);
        return Ok(records);
    }

    /// <summary>Gets profit summary broken down by service type.</summary>
    [HttpGet("by-service")]
    public async Task<IActionResult> GetProfitByService([FromQuery] long? salesmanId, CancellationToken ct)
    {
        var targetSalesmanId = salesmanId ?? _currentUser.UserId;
        if (!targetSalesmanId.HasValue || targetSalesmanId.Value <= 0)
            return BadRequest(new { Message = "Valid SalesmanId is required." });

        var summary = await _profitService.GetProfitSummaryByServiceAsync(targetSalesmanId.Value, ct);
        return Ok(summary);
    }
}
