using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using TexasPortal.Application.Features.Invoices.Models;
using TexasPortal.Application.Features.Invoices.Services;

namespace TexasPortal.Infrastructure.Pdf;

public class InvoicePdfService : IInvoicePdfService
{
    private readonly IWebHostEnvironment _env;

    public InvoicePdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Task<byte[]> GeneratePdfAsync(InvoiceDataModel model, CancellationToken ct = default)
    {
        string webRootPath = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var document = new InvoicePdfDocument(model, webRootPath);
        byte[] pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }
}
