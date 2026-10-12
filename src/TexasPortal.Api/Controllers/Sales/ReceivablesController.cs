using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Receivables;
using TexasPortal.Contracts.Sales.Receivables;

namespace TexasPortal.Api.Controllers.Sales;

[Authorize]
[ApiController]
[Route("api/receivables")]
public class ReceivablesController : ControllerBase
{
    private readonly IReceivableService _receivableService;
    private readonly IReceiptService _receiptService;
    private readonly IApprovalService _approvalService;
    private readonly IStatementService _statementService;
    private readonly IReportingService _reportingService;
    private readonly ICurrentUser _currentUser;

    public ReceivablesController(
        IReceivableService receivableService,
        IReceiptService receiptService,
        IApprovalService approvalService,
        IStatementService statementService,
        IReportingService reportingService,
        ICurrentUser currentUser)
    {
        _receivableService = receivableService;
        _receiptService = receiptService;
        _approvalService = approvalService;
        _statementService = statementService;
        _reportingService = reportingService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetReceivables(
        [FromQuery] string? status,
        [FromQuery] string? bookingModule,
        [FromQuery] string? priority,
        [FromQuery] long? salesmanId,
        [FromQuery] long? headId,
        CancellationToken ct)
    {
        var result = await _receivableService.QueryReceivablesAsync(status, bookingModule, priority, salesmanId, headId, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetReceivableById(long id, CancellationToken ct)
    {
        var res = await _receivableService.GetReceivableByIdAsync(id, ct);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken ct)
    {
        var metrics = await _reportingService.GetDashboardMetricsAsync(ct);
        return Ok(metrics);
    }

    [HttpGet("aging")]
    public async Task<IActionResult> GetAgingReport(CancellationToken ct)
    {
        var report = await _reportingService.GetAgingReportAsync(ct);
        return Ok(report);
    }

    [HttpGet("statements/customer/{headId:long}")]
    public async Task<IActionResult> GetCustomerStatement(long headId, CancellationToken ct)
    {
        var statement = await _statementService.GetHeadRunningStatementAsync(headId, Domain.Enums.HeadType.Customer, null, null, ct);
        return Ok(statement);
    }

    [HttpGet("statements/agent/{agentId:long}")]
    public async Task<IActionResult> GetAgentStatement(long agentId, CancellationToken ct)
    {
        var statement = await _statementService.GetHeadRunningStatementAsync(agentId, Domain.Enums.HeadType.Agent, null, null, ct);
        return Ok(statement);
    }

    [HttpGet("head/{headId:long}/applications")]
    public async Task<IActionResult> GetHeadOpenApplications(long headId, CancellationToken ct)
    {
        var applications = await _receivableService.GetHeadOpenReceivablesAsync(headId, ct);
        return Ok(applications);
    }

    public record ApprovePaymentRequest(long PaymentId);
    public record RejectPaymentRequest(long PaymentId, string Reason);

    [HttpPost("approve")]
    public async Task<IActionResult> ApprovePayment([FromBody] ApprovePaymentRequest req, CancellationToken ct)
    {
        await _approvalService.ApprovePaymentAsync(req.PaymentId, _currentUser.UserId ?? 0, ct);
        return Ok(new { Message = "Payment approved successfully." });
    }

    [HttpPost("reject")]
    public async Task<IActionResult> RejectPayment([FromBody] RejectPaymentRequest req, CancellationToken ct)
    {
        await _approvalService.RejectPaymentAsync(req.PaymentId, _currentUser.UserId ?? 0, req.Reason, ct);
        return Ok(new { Message = "Payment rejected." });
    }

    [HttpPost("payments")]
    public async Task<IActionResult> CreateReceiptWithAllocations([FromBody] CreateReceiptWithAllocationsDto dto, CancellationToken ct)
    {
        try
        {
            var receipt = await _receiptService.CreateReceiptWithAllocationsAsync(dto, _currentUser.UserId ?? 0, ct);
            return Ok(new
            {
                id = receipt.Id,
                receiptId = receipt.ReceiptId,
                documentNumber = receipt.DocumentNumber,
                headId = receipt.HeadId,
                agentId = receipt.AgentId,
                amount = receipt.Amount,
                paymentMode = receipt.PaymentMode,
                status = receipt.Status,
                createdAt = receipt.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message, Detail = ex.InnerException?.Message ?? ex.Message });
        }
    }
}
