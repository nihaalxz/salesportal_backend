using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Features.Invoices.Factory;
using TexasPortal.Application.Features.Invoices.Services;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize]
[Route("api/sales/invoices")]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceBuilderFactory _builderFactory;
    private readonly IInvoicePdfService _pdfService;
    private readonly ICurrentUser _currentUser;

    public InvoicesController(
        IInvoiceBuilderFactory builderFactory,
        IInvoicePdfService pdfService,
        ICurrentUser currentUser)
    {
        _builderFactory = builderFactory;
        _pdfService = pdfService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Centralized endpoint to generate PDF invoices for any supported entity type (visa, flight, hotel, holiday, transport, vfs, other, receipt).
    /// </summary>
    [HttpGet("{entityType}/{id:long}")]
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

        var builder = _builderFactory.GetBuilder(entityType);
        if (builder is null)
        {
            return BadRequest($"Unsupported invoice entity type '{entityType}'.");
        }

        var invoiceModel = await builder.BuildAsync(id, userId, ct);
        if (invoiceModel is null)
        {
            return NotFound($"Invoice record for '{entityType}' #{id} was not found.");
        }

        byte[] pdfBytes = await _pdfService.GeneratePdfAsync(invoiceModel, ct);

        bool isReceipt = string.Equals(entityType, "receipt", StringComparison.OrdinalIgnoreCase); string filename = isReceipt ? $"RECEIPT-{invoiceModel.InvoiceNo}.pdf" : $"INVOICE-{invoiceModel.InvoiceNo}.pdf";
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{filename}\"");
        return File(pdfBytes, "application/pdf");
    }

}
