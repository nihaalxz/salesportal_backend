using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TexasPortal.Application.Sales.Accounts;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Api.Controllers.Sales;

[Authorize]
[ApiController]
[Route("api/customer-accounts")]
public class CustomerAccountController : ControllerBase
{
    private readonly ICustomerAccountQueryService _queryService;

    public CustomerAccountController(ICustomerAccountQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet("{headId:long}")]
    public async Task<IActionResult> GetAccountStatement(long headId, CancellationToken ct)
    {
        var statement = await _queryService.GetAccountStatementAsync(headId, ct);
        if (statement == null) return NotFound(new { Message = $"Customer account #{headId} not found." });
        return Ok(statement);
    }

    [HttpGet("{headId:long}/summary")]
    public async Task<IActionResult> GetAccountSummary(long headId, [FromQuery] string headType = "Customer", CancellationToken ct = default)
    {
        Enum.TryParse<HeadType>(headType, true, out var hType);
        var summary = await _queryService.GetAccountSummaryAsync(headId, hType, ct);
        if (summary == null) return NotFound(new { Message = $"Customer account #{headId} not found." });
        return Ok(summary);
    }

    [HttpGet("{headId:long}/applications")]
    public async Task<IActionResult> GetAccountApplications(long headId, [FromQuery] string headType = "Customer", CancellationToken ct = default)
    {
        System.Enum.TryParse<HeadType>(headType, true, out var hType);
        var apps = await _queryService.GetApplicationReceivablesSummaryAsync(headId, hType, ct);
        return Ok(apps);
    }

    [HttpGet("{headId:long}/timeline")]
    public async Task<IActionResult> GetAccountTimeline(long headId, CancellationToken ct)
    {
        var timeline = await _queryService.GetAccountTimelineAsync(headId, ct);
        return Ok(timeline);
    }
}
