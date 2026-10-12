using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Quotations.CreateQuotation;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Documents;
using TexasPortal.Contracts.Sales.Quotations;
using TexasPortal.Infrastructure.Pdf;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/quotations")]
[Produces("application/json")]
public sealed class QuotationsController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly CreateQuotationHandler _createQuotationHandler;

    public QuotationsController(
        ISalesReadService salesReadService,
        ICurrentUser currentUser,
        CreateQuotationHandler createQuotationHandler)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _createQuotationHandler = createQuotationHandler;
    }

    private bool TryGetUserId(out long userId)
    {
        userId = 0;
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null) return false;
        userId = _currentUser.UserId.Value;
        return true;
    }

    /// <summary>Lists quotations for the authenticated salesman with search and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<QuotationSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetQuotations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? clientType = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");

        var result = await _salesReadService.GetQuotationsAsync(userId, new QuotationsListQuery(page, pageSize, searchTerm, clientType, status), ct);
        return Ok(result);
    }

    /// <summary>Gets quotation details by database ID or document number.</summary>
    [HttpGet("{idOrDoc}")]
    [ProducesResponseType(typeof(QuotationDetailResponse), 200)]
    [ProducesResponseType(404), ProducesResponseType(401)]
    public async Task<IActionResult> GetQuotationById(string idOrDoc, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _salesReadService.GetQuotationByIdAsync(userId, idOrDoc, ct);
        if (result == null) return NotFound("Quotation not found.");

        return Ok(result);
    }

    /// <summary>Creates a new quotation with sequential document number TXSQT000001.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(QuotationDetailResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(422)]
    public async Task<IActionResult> CreateQuotation(
        [FromBody] CreateQuotationRequest request,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var command = new CreateQuotationCommand(userId, request);
        var result = await _createQuotationHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateQuotationResultStatus.Success =>
                Created($"/api/sales/quotations/{result.Response!.Id}", result.Response),

            CreateQuotationResultStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateQuotationResultStatus.HeadNotFound =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Specified Customer / Agent Head ID was not found."
                }),

            _ => StatusCode(500, "An error occurred while creating the quotation.")
        };
    }

    /// <summary>Generates and downloads the PDF for a saved quotation by ID or document number.</summary>
    [HttpGet("{idOrDoc}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(400), ProducesResponseType(404), ProducesResponseType(401)]
    public async Task<IActionResult> GetQuotationPdf(string idOrDoc, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var detail = await _salesReadService.GetQuotationByIdAsync(userId, idOrDoc, ct);
        if (detail == null) return NotFound("Quotation not found.");

        var isApproved = string.Equals(detail.Status, "approved", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(detail.Status, "success", StringComparison.OrdinalIgnoreCase);

        if (!isApproved)
        {
            return BadRequest("Quotation is currently pending approval by Accounts. PDF downloads and previews are available once approved.");
        }

        var pdfRequest = new QuotationPdfRequest(
            QuotationNo: detail.DocumentNumber,
            QuotationDate: detail.CreatedAt,
            ValidUntil: null,
            ClientType: detail.ClientType,
            HeadId: detail.HeadId,
            CustomerName: detail.CustomerName,
            CustomerContact: detail.CustomerContact,
            CustomerEmail: detail.CustomerEmail,
            SalesmanName: detail.SalesmanName,
            RemarksNote: detail.Remarks,
            Items: detail.Items.Select(i => new QuotationPdfItemRequest(
                ServiceType: i.ServiceType,
                Description: i.Description,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                TotalAmount: i.TotalAmount
            )).ToList()
        );

        var document = new QuotationPdfDocument(pdfRequest);
        byte[] pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"{detail.DocumentNumber}.pdf");
    }

    /// <summary>Deletes a saved quotation.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404), ProducesResponseType(401)]
    public async Task<IActionResult> DeleteQuotation(long id, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var deleted = await _salesReadService.DeleteQuotationAsync(userId, id, ct);
        if (!deleted) return NotFound("Quotation not found.");

        return NoContent();
    }
}
