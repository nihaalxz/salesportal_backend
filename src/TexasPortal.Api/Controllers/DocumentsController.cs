using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Features.Documents.Builders;
using TexasPortal.Application.Features.Documents.Factory;
using TexasPortal.Application.Features.Documents.Services;
using TexasPortal.Contracts.Documents;
using TexasPortal.Infrastructure.Pdf;

namespace TexasPortal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentBuilderFactory _builderFactory;
    private readonly IDocumentPdfService _pdfService;
    private readonly ICurrentUser _currentUser;

    public DocumentsController(
        IDocumentBuilderFactory builderFactory,
        IDocumentPdfService pdfService,
        ICurrentUser currentUser)
    {
        _builderFactory = builderFactory;
        _pdfService = pdfService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Generates server-side PDF invoice for individual detail pages.
    /// </summary>
    [HttpGet("invoice/{entityType}/{id:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInvoicePdf(
        [FromRoute] string entityType,
        [FromRoute] long id,
        CancellationToken ct = default)
    {
        long userId = _currentUser.UserId ?? 0;
        var builder = _builderFactory.GetBuilder("invoice", entityType);
        if (builder is null)
        {
            return BadRequest($"Unsupported document invoice type '{entityType}'.");
        }

        var buildArgs = new DocumentBuildArgs
        {
            Category = "invoice",
            Type = entityType,
            EntityId = id,
            UserId = userId
        };

        var docModel = await builder.BuildAsync(buildArgs, ct);
        if (docModel is null)
        {
            return NotFound($"Document record for '{entityType}' #{id} was not found.");
        }

        byte[] pdfBytes = await _pdfService.GeneratePdfAsync(docModel, ct);
        string filename = $"{entityType.ToUpper()}-{docModel.DocumentNo}.pdf";
        Response.Headers.Append("Content-Disposition", "inline; filename=" + filename);
        return File(pdfBytes, "application/pdf");
    }

    /// <summary>
    /// Generates server-side PDF list report for index pages with active search & filters.
    /// </summary>
    [HttpGet("list/{entityType}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetListReportPdf(
        [FromRoute] string entityType,
        CancellationToken ct = default)
    {
        long userId = _currentUser.UserId ?? 0;
        var builder = _builderFactory.GetBuilder("list", entityType);
        if (builder is null)
        {
            return BadRequest($"Unsupported document list report type '{entityType}'.");
        }

        var queryDict = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString(), System.StringComparer.OrdinalIgnoreCase);

        var buildArgs = new DocumentBuildArgs
        {
            Category = "list",
            Type = entityType,
            UserId = userId,
            QueryParams = queryDict
        };

        var docModel = await builder.BuildAsync(buildArgs, ct);
        if (docModel is null)
        {
            return NotFound($"List report for '{entityType}' could not be generated.");
        }

        byte[] pdfBytes = await _pdfService.GeneratePdfAsync(docModel, ct);
        string filename = $"{entityType.ToUpper()}-ListReport.pdf";
        Response.Headers.Append("Content-Disposition", "inline; filename=" + filename);
        return File(pdfBytes, "application/pdf");
    }

    /// <summary>
    /// Generates server-side PDF report for general reports (e.g. ledger statement).
    /// </summary>
    [HttpGet("report/{entityType}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReportPdf(
        [FromRoute] string entityType,
        CancellationToken ct = default)
    {
        long userId = _currentUser.UserId ?? 0;
        var builder = _builderFactory.GetBuilder("report", entityType) ?? _builderFactory.GetBuilder("list", entityType);
        if (builder is null)
        {
            return BadRequest($"Unsupported document report type '{entityType}'.");
        }

        var queryDict = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString(), System.StringComparer.OrdinalIgnoreCase);

        var buildArgs = new DocumentBuildArgs
        {
            Category = "report",
            Type = entityType,
            UserId = userId,
            QueryParams = queryDict
        };

        var docModel = await builder.BuildAsync(buildArgs, ct);
        if (docModel is null)
        {
            return NotFound($"Report for '{entityType}' could not be generated.");
        }

        byte[] pdfBytes = await _pdfService.GeneratePdfAsync(docModel, ct);
        string filename = $"{entityType.ToUpper()}-Report.pdf";
        Response.Headers.Append("Content-Disposition", "inline; filename=" + filename);
        return File(pdfBytes, "application/pdf");
    }

    /// <summary>
    /// Generates server-side PDF quotation with no company logo or company contact info.
    /// </summary>
    [HttpPost("quotation/pdf")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(400)]
    public IActionResult GenerateQuotationPdf(
        [FromBody] QuotationPdfRequest request,
        CancellationToken ct = default)
    {
        if (request is null || request.Items is null || request.Items.Count == 0)
        {
            return BadRequest("At least one quotation item is required.");
        }

        var document = new QuotationPdfDocument(request);
        byte[] pdfBytes = document.GeneratePdf();

        string qNo = string.IsNullOrWhiteSpace(request.QuotationNo) ? "QUOTATION" : request.QuotationNo.Trim();
        string filename = $"{qNo}.pdf";
        Response.Headers.Append("Content-Disposition", "inline; filename=" + filename);
        return File(pdfBytes, "application/pdf");
    }
}
