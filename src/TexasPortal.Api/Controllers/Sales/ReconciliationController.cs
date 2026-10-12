using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Sales.Receivables;

namespace TexasPortal.Api.Controllers.Sales;

/// <summary>
/// Admin-only endpoints to detect and repair financial inconsistencies.
/// Invoke after deployment to backfill receipts approved before v4.0 orchestrator.
/// </summary>
[ApiController]
[Authorize]
[Route("api/reconciliation")]
[Produces("application/json")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly IReceivableReconciliationService _reconciliationService;

    public ReconciliationController(IReceivableReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    /// <summary>
    /// Detects credit_settlement receipts with status approved/success that still have
    /// ReceivablePayments in Submitted state — indicating an incomplete financial pipeline.
    /// </summary>
    [HttpGet("receipts/orphans")]
    public async Task<IActionResult> GetOrphanReceipts(CancellationToken ct)
    {
        var orphans = await _reconciliationService.DetectOrphanReceiptsAsync(ct);
        return Ok(new { Count = orphans.Count, Orphans = orphans });
    }

    /// <summary>
    /// Retroactively applies the financial pipeline (ReceivableTransaction insert +
    /// cache recalculation) to all orphaned receipts. Safe to run multiple times (idempotent).
    ///
    /// Use immediately after deploying v4.0 to fix TXSRCPT000045 and any similar receipts.
    /// </summary>
    [HttpPost("receipts/backfill")]
    public async Task<IActionResult> BackfillApprovedReceipts(CancellationToken ct)
    {
        var report = await _reconciliationService.BackfillApprovedReceiptsAsync(ct: ct);
        return Ok(new
        {
            report.Processed,
            report.Fixed,
            report.Skipped,
            report.Errors,
            Message = $"Backfill complete. {report.Fixed}/{report.Processed} receipts fixed."
        });
    }

    /// <summary>
    /// Rebuilds CachedOutstandingAmount for all receivables from their immutable
    /// ReceivableTransaction ledger. Use to fix cache corruption without touching history.
    /// Optionally scoped to a specific headId.
    /// </summary>
    [HttpPost("receivables/rebuild-cache")]
    public async Task<IActionResult> RebuildOutstandingCache([FromQuery] long? headId, CancellationToken ct)
    {
        var report = await _reconciliationService.RebuildCacheAsync(headId, ct);
        return Ok(new
        {
            report.Processed,
            report.Fixed,
            report.Errors,
            Message = $"Cache rebuild complete. {report.Fixed}/{report.Processed} receivables updated."
        });
    }
}

