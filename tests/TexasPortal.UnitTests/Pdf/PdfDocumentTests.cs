using System;
using System.Collections.Generic;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using TexasPortal.Application.Features.Documents.Models;
using TexasPortal.Contracts.Documents;
using TexasPortal.Infrastructure.Pdf;
using Xunit;

namespace TexasPortal.UnitTests.Pdf;

public class PdfDocumentTests
{
    static PdfDocumentTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void ResolveLogoBytes_ReturnsEmbeddedTexasLogo_EvenWithoutWebRoot()
    {
        var bytes = PdfAssetHelper.ResolveLogoBytes(webRootPath: null);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        // PNG magic bytes: 89 50 4E 47
        Assert.Equal((byte)0x89, bytes[0]);
        Assert.Equal((byte)0x50, bytes[1]);
        Assert.Equal((byte)0x4E, bytes[2]);
        Assert.Equal((byte)0x47, bytes[3]);
    }

    [Fact]
    public void GenericDocumentPdfDocument_Invoice_EmbedsLogoImageAndRenders()
    {
        var model = new DocumentDataModel
        {
            Category = "Invoice",
            Title = "INVOICE",
            Subtitle = "FLIGHT BOOKING",
            DocumentNo = "TXSFLT795299",
            Date = new DateTime(2026, 11, 8),
            CustomerName = "AJMAL SAUDI",
            ClientType = "customer",
            SalesmanNameOrEmail = "ISHANA ABIDHA SHIHAR KHAN",
            AccountantName = "ASHKAR K",
            RemarksNote = "Flight Booking - AJMAL SAUDI",
            AmountInWords = "One Hundred Ten Dinars Only",
            MetaData = new List<KeyValuePair<string, string>>
            {
                new("Invoice No", "TXSFLT795299"),
                new("Date", "08/11/2026"),
                new("Service", "FLIGHT BOOKING")
            },
            Table = new DocumentTable
            {
                Columns = new List<DocumentColumn>
                {
                    new() { Header = "S NO.", RelativeWidth = 0.5f, Alignment = ColumnAlignment.Center },
                    new() { Header = "DESCRIPTION", RelativeWidth = 4.0f, Alignment = ColumnAlignment.Left },
                    new() { Header = "QTY", RelativeWidth = 0.6f, Alignment = ColumnAlignment.Center },
                    new() { Header = "RATE", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Right },
                    new() { Header = "PRICE", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Right },
                    new() { Header = "N.PRICE", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Right },
                    new() { Header = "TOTAL", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
                },
                Rows = new List<List<string>>
                {
                    new() { "1", "Flight: COK to BAH - BEENA NAZAR", "1", "110.000", "110.000", "110.000", "110.000" }
                }
            },
            Totals = new DocumentTotals
            {
                SubTotal = 110.000m,
                NetTotal = 110.000m,
                Currency = "BHD"
            }
        };

        var doc = new GenericDocumentPdfDocument(model, string.Empty);
        var pdfBytes = doc.GeneratePdf();
        var raw = Encoding.ASCII.GetString(pdfBytes);

        Assert.NotEmpty(pdfBytes);
        Assert.Contains("/Subtype /Image", raw);
    }

    [Fact]
    public void QuotationPdfDocument_EmbedsLogoImageAndRenders()
    {
        var req = new QuotationPdfRequest(
            QuotationNo: "TXSQT000002",
            QuotationDate: new DateTime(2026, 11, 24),
            ValidUntil: null,
            ClientType: "customer",
            HeadId: 10,
            CustomerName: "MUHAMMED ZAHEES",
            CustomerContact: "36822657",
            CustomerEmail: "texasbhfinance@gmail.com",
            SalesmanName: "Admin",
            RemarksNote: null,
            Items: new List<QuotationPdfItemRequest>
            {
                new("FLIGHT BOOKING", "flight ticket", 1, 200.000m, 200.000m)
            }
        );

        var doc = new QuotationPdfDocument(req);
        var pdfBytes = doc.GeneratePdf();
        var raw = Encoding.ASCII.GetString(pdfBytes);

        Assert.NotEmpty(pdfBytes);
        Assert.Contains("/Subtype /Image", raw);
    }
}
