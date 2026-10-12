using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TexasPortal.Application.Sales.Profit;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Route("api/internal/profit")]
[Produces("application/json")]
public sealed class InternalProfitController : ControllerBase
{
    private readonly ISalesmanProfitService _profitService;

    public InternalProfitController(ISalesmanProfitService profitService)
    {
        _profitService = profitService;
    }

    /// <summary>
    /// Internal endpoint invoked when Accounts Portal approves a supplier payment.
    /// Idempotently finalizes or updates the salesman profit record.
    /// </summary>
    [HttpPost("refresh/{bookingType}/{bookingIdOrRef}")]
    public async Task<IActionResult> RefreshProfit(string bookingType, string bookingIdOrRef, CancellationToken ct)
    {
        var record = await _profitService.FinalizeOrUpdateProfitAsync(bookingType, bookingIdOrRef, ct);
        return Ok(new
        {
            Success = true,
            BookingType = bookingType,
            BookingIdOrRef = bookingIdOrRef,
            ProfitRecord = record
        });
    }

    /// <summary>
    /// Internal endpoint invoked to add an independent salesman profit adjustment (e.g. Sales Return, Manual Adjustment, Correction).
    /// Enforces idempotency via request.IdempotencyKey.
    /// </summary>
    [HttpPost("adjust")]
    public async Task<IActionResult> AddProfitAdjustment([FromBody] SalesmanProfitAdjustmentRequest request, CancellationToken ct)
    {
        try
        {
            var adjustment = await _profitService.AddAdjustmentAsync(request, ct);
            return Ok(new
            {
                Success = true,
                Adjustment = adjustment
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Admin endpoint to run backfill migration on historical bookings across all 7 service types.
    /// Safe to execute multiple times (idempotent).
    /// </summary>
    [HttpPost("backfill")]
    public async Task<IActionResult> BackfillProfitRecords(CancellationToken ct)
    {
        var report = await _profitService.BackfillAllProfitRecordsAsync(ct);
        return Ok(new
        {
            Message = "Historical profit records backfill completed.",
            Report = report
        });
    }
}
