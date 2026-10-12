using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Receipts;
using TexasPortal.Application.Sales.Receipts.CreateReceipt;

using TexasPortal.Application.Sales.Receipts.UpdateReceiptStatus;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/receipts")]
[Produces("application/json")]
public sealed class ReceiptsController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _env;
    private readonly CreateReceiptHandler _createReceiptHandler;
    private readonly UpdateReceiptStatusHandler _updateReceiptStatusHandler;

    public ReceiptsController(
        ISalesReadService salesReadService,
        ICurrentUser currentUser,
        IWebHostEnvironment env,
        CreateReceiptHandler createReceiptHandler,
        UpdateReceiptStatusHandler updateReceiptStatusHandler)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _env = env;
        _createReceiptHandler = createReceiptHandler;
        _updateReceiptStatusHandler = updateReceiptStatusHandler;
    }

    /// <summary>Lists receipts owned by the salesman.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ReceiptSummaryResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetReceipts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100");

        var query = new ReceiptsListQuery(page, pageSize, searchTerm);
        var result = await _salesReadService.GetReceiptsAsync(userId, query, ct);
        return Ok(result);
    }

    /// <summary>Gets details for a specific receipt.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReceiptDetailResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReceipt([FromRoute] string id, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _salesReadService.GetReceiptByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a new payment receipt.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReceiptDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        var result = await _createReceiptHandler.HandleAsync(new CreateReceiptCommand(userId, request), ct);
        if (result.Status == CreateReceiptResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Detail = "Please correct validation errors.",
                Extensions = { ["errors"] = result.Errors }
            });

        return CreatedAtAction(nameof(GetReceipt), new { id = result.Response!.Id }, result.Response);
    }



    /// <summary>Retrieves bookings eligible for specific allocation filtering by head and service type.</summary>
    [HttpGet("booking-options")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<BookingOptionResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetBookingOptions(
        [FromQuery] long headId,
        [FromQuery] string service,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        if (headId <= 0) return BadRequest("headId must be greater than zero");
        if (string.IsNullOrWhiteSpace(service)) return BadRequest("service type is required");

        var result = await _salesReadService.GetBookingOptionsAsync(userId, headId, service, ct);
        return Ok(result);
    }

    /// <summary>Retrieves verified bank accounts from the accounts portal.</summary>
    [HttpGet("banks")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<BankResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetBanks(CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        var result = await _salesReadService.GetBanksAsync(ct);
        return Ok(result);
    }

    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<SupplierResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSuppliers(CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        var result = await _salesReadService.GetSuppliersAsync(ct);
        return Ok(result);
    }

    /// <summary>Updates status of a pending payment receipt (salesman workflow).</summary>
    [HttpPost("{id:long}/status")]
    [ProducesResponseType(typeof(ReceiptDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> UpdateReceiptStatus(
        [FromRoute] long id,
        [FromBody] UpdateReceiptStatusRequest request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
            return Unauthorized();

        var result = await _updateReceiptStatusHandler.HandleAsync(new UpdateReceiptStatusCommand(userId, id, request), ct);
        if (result.Status == UpdateReceiptStatusResultStatus.NotFound)
            return NotFound();
        if (result.Status == UpdateReceiptStatusResultStatus.ValidationError)
            return UnprocessableEntity(new ProblemDetails
            {
                Status = 422,
                Title = "Validation Failed",
                Extensions = { ["errors"] = result.Errors }
            });
        if (result.Status == UpdateReceiptStatusResultStatus.Conflict)
            return Conflict(new ProblemDetails { Status = 409, Title = "Conflict", Detail = result.Message });
        if (result.Status == UpdateReceiptStatusResultStatus.ServerError)
            return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error", Detail = result.Message });

        return Ok(result.Response);
    }
}

