using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TexasPortal.Contracts.Documents;

namespace TexasPortal.Infrastructure.Pdf;

public class QuotationPdfDocument : IDocument
{
    private readonly QuotationPdfRequest _request;
    private readonly string? _webRootPath;

    private const string PrimaryColor = "#1b2559";
    private const string DarkTextColor = "#0f172a";
    private const string MutedTextColor = "#64748b";
    private const string BorderColor = "#cbd5e1";
    private const string LightBgColor = "#f8fafc";

    private const string CompanyName = "TEXAS TRAVEL AND TOURISM W.L.L";
    private const string OfficeDetails = "SHOP NO. 82 , BUILDING 363, ROAD 1705, BLOCK 317, DIPLOMATIC AREA";
    private const string CrAndPhone = "CR: 148457, Tel: +973 77990010";
    private const string ContactDetails = "info@texastravelsbh.com | www.texastravelsbh.com";

    public QuotationPdfDocument(QuotationPdfRequest request, string? webRootPath = null)
    {
        _request = request;
        _webRootPath = webRootPath;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9f).FontColor(DarkTextColor).FontFamily("Arial"));

                page.Content().Border(1).BorderColor("#94a3b8").Padding(16).Column(col =>
                {
                    col.Item().Element(ComposeHeader);
                    col.Item().PaddingTop(12).Element(ComposeCustomerAndMeta);
                    col.Item().PaddingTop(14).Element(ComposeTable);
                    col.Item().PaddingTop(14).Element(ComposeTotalsAndNotes);
                    col.Item().PaddingTop(24).Element(ComposeSignatures);
                });

                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Company Header: Logo on left, details on right
            col.Item().Row(row =>
            {
                // Left: Logo
                row.ConstantItem(150).Column(logoCol =>
                {
                    byte[]? logoBytes = ResolveLogoBytes();
                    if (logoBytes != null && logoBytes.Length > 0)
                    {
                        logoCol.Item().Width(150).Image(logoBytes);
                    }
                    else
                    {
                        logoCol.Item().Text(CompanyName).Bold().FontSize(13).FontColor(PrimaryColor);
                    }
                });

                // Right: Company Details
                row.RelativeItem().Column(compCol =>
                {
                    compCol.Item().AlignRight().Text(CompanyName).Bold().FontSize(12).FontColor(PrimaryColor);
                    compCol.Item().AlignRight().Text(OfficeDetails).FontSize(7.8f).FontColor("#475569");
                    compCol.Item().AlignRight().Text(CrAndPhone).FontSize(8.5f).FontColor("#475569");
                    compCol.Item().AlignRight().Text(ContactDetails).FontSize(8.5f).FontColor("#475569");
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(BorderColor);

            // Document Title
            col.Item().PaddingTop(4).AlignCenter().Column(titleCol =>
            {
                titleCol.Item().AlignCenter().Text("QUOTATION").Bold().FontSize(18f).FontColor(PrimaryColor);
                titleCol.Item().AlignCenter().PaddingTop(2).Text("OFFICIAL PRICE ESTIMATE").Bold().FontSize(9f).FontColor(MutedTextColor);
            });
        });
    }

    private void ComposeCustomerAndMeta(IContainer container)
    {
        container.Row(row =>
        {
            // Left: Client / Recipient Info
            row.RelativeItem(1.2f).Border(0.5f).BorderColor(BorderColor).Background(LightBgColor).Padding(10).Column(clientCol =>
            {
                clientCol.Item().Text("QUOTATION TO:").Bold().FontSize(8f).FontColor(MutedTextColor);
                clientCol.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(_request.CustomerName) ? "Valued Client" : _request.CustomerName.ToUpper()).Bold().FontSize(10.5f).FontColor(DarkTextColor);

                clientCol.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Client Segment: ").FontSize(8f).FontColor(MutedTextColor);
                    t.Span(string.Equals(_request.ClientType, "agent", StringComparison.OrdinalIgnoreCase) ? "Agent" : "Customer").Bold().FontSize(8.5f).FontColor(DarkTextColor);
                });

                if (!string.IsNullOrWhiteSpace(_request.CustomerContact))
                {
                    clientCol.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("Contact No: ").FontSize(8f).FontColor(MutedTextColor);
                        t.Span(_request.CustomerContact).FontSize(8.5f).FontColor(DarkTextColor);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_request.CustomerEmail))
                {
                    clientCol.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("Email: ").FontSize(8f).FontColor(MutedTextColor);
                        t.Span(_request.CustomerEmail).FontSize(8.5f).FontColor(DarkTextColor);
                    });
                }
            });

            row.ConstantItem(14);

            // Right: Quotation Metadata
            row.RelativeItem(1.0f).Border(0.5f).BorderColor(BorderColor).Background(LightBgColor).Padding(10).Column(metaCol =>
            {
                metaCol.Item().Table(mTable =>
                {
                    mTable.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(85);
                        cols.RelativeColumn();
                    });

                    mTable.Cell().PaddingVertical(2).Text("Quotation No:").FontSize(8f).FontColor(MutedTextColor);
                    mTable.Cell().PaddingVertical(2).Text(_request.QuotationNo ?? "TXSQT" + (DateTime.UtcNow.Ticks % 100000).ToString("D5")).Bold().FontSize(9.5f).FontColor(PrimaryColor);

                    mTable.Cell().PaddingVertical(2).Text("Issue Date:").FontSize(8f).FontColor(MutedTextColor);
                    mTable.Cell().PaddingVertical(2).Text((_request.QuotationDate ?? DateTime.UtcNow).ToString("dd-MM-yyyy")).FontSize(8.5f).FontColor(DarkTextColor);
                });
            });
        });
    }

    private void ComposeTable(IContainer container)
    {
        var items = _request.Items ?? new System.Collections.Generic.List<QuotationPdfItemRequest>();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);   // #
                columns.ConstantColumn(120);  // Service
                columns.RelativeColumn();     // Description
                columns.ConstantColumn(38);   // Qty
                columns.ConstantColumn(75);   // Unit Price
                columns.ConstantColumn(85);   // Total (BHD)
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Background(PrimaryColor).PaddingVertical(6).PaddingHorizontal(3).AlignCenter().Text("#").Bold().FontSize(8f).FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).PaddingVertical(6).PaddingHorizontal(5).Text("SERVICE").Bold().FontSize(8f).FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).PaddingVertical(6).PaddingHorizontal(5).Text("DESCRIPTION").Bold().FontSize(8f).FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).PaddingVertical(6).PaddingHorizontal(3).AlignCenter().Text("QTY").Bold().FontSize(8f).FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).PaddingVertical(6).PaddingHorizontal(5).AlignRight().Text("RATE (BHD)").Bold().FontSize(8f).FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).PaddingVertical(6).PaddingHorizontal(5).AlignRight().Text("AMOUNT (BHD)").Bold().FontSize(8f).FontColor(Colors.White);
            });

            // Table Rows
            int index = 1;
            foreach (var item in items)
            {
                var rowBg = (index % 2 == 0) ? "#f8fafc" : "#ffffff";
                decimal itemTotal = item.TotalAmount > 0 ? item.TotalAmount : (item.UnitPrice * Math.Max(1, item.Quantity));

                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(3).AlignCenter().Text(index.ToString()).FontSize(8f).FontColor(MutedTextColor);
                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(5).Text(item.ServiceType?.ToUpper() ?? "").Bold().FontSize(8.5f).FontColor(DarkTextColor);
                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(5).Text(item.Description ?? "-").FontSize(8.5f).FontColor("#334155");
                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(3).AlignCenter().Text(item.Quantity.ToString()).FontSize(8.5f);
                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(5).AlignRight().Text($"{item.UnitPrice:0.000}").FontSize(8.5f);
                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(5).AlignRight().Text($"{itemTotal:0.000}").Bold().FontSize(8.5f).FontColor(DarkTextColor);

                index++;
            }
        });
    }

    private void ComposeTotalsAndNotes(IContainer container)
    {
        var items = _request.Items ?? new System.Collections.Generic.List<QuotationPdfItemRequest>();
        decimal netTotal = items.Sum(x => x.TotalAmount > 0 ? x.TotalAmount : (x.UnitPrice * Math.Max(1, x.Quantity)));

        container.AlignRight().Width(240).Column(rightCol =>
        {
            rightCol.Item().Border(0.5f).BorderColor(BorderColor).Table(totTable =>
            {
                totTable.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text("Sub Total").FontSize(8.5f).Bold();
                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text($"{netTotal:0.000} BHD").FontSize(8.5f);

                totTable.Cell().Background("#f1f5f9").Padding(6).Text("Net Quotation").Bold().FontSize(9.5f).FontColor(PrimaryColor);
                totTable.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text($"{netTotal:0.000} BHD").Bold().FontSize(10f).FontColor(PrimaryColor);
            });
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.Row(row =>
        {
            // Left: Authorized Signature
            row.RelativeItem().Column(col =>
            {
                col.Item().PaddingTop(30);
                col.Item().Width(140).LineHorizontal(0.5f).LineColor("#94a3b8");
                col.Item().PaddingTop(3).Text("Authorized Representative").FontSize(8f).FontColor(MutedTextColor);
            });

            // Right: Client Acceptance Signature
            row.RelativeItem().Column(col =>
            {
                col.Item().PaddingTop(30);
                col.Item().AlignRight().Column(inner =>
                {
                    inner.Item().Width(140).LineHorizontal(0.5f).LineColor("#94a3b8");
                    inner.Item().PaddingTop(3).AlignCenter().Text("Client Acceptance / Signature").FontSize(8f).FontColor(MutedTextColor);
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Quotation Document • Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}").FontSize(7.5f).FontColor("#94a3b8");

            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7.5f).FontColor("#94a3b8");
                x.CurrentPageNumber().FontSize(7.5f).FontColor("#94a3b8");
                x.Span(" of ").FontSize(7.5f).FontColor("#94a3b8");
                x.TotalPages().FontSize(7.5f).FontColor("#94a3b8");
            });
        });
    }

    private byte[]? ResolveLogoBytes()
    {
        var candidates = new[]
        {
            !string.IsNullOrWhiteSpace(_webRootPath) ? Path.Combine(_webRootPath, "texas-logo.png") : null,
            !string.IsNullOrWhiteSpace(_webRootPath) ? Path.Combine(_webRootPath, "static", "texas-logo.png") : null,
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "texas-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "static", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "static", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "TexasPortal.Api", "wwwroot", "texas-logo.png"),
            @"d:\freelanceprojects\TexasPortalsDotNet\texas_sales_portal_backend\texas_sales_portal_backend\src\TexasPortal.Api\wwwroot\texas-logo.png",
            @"d:\freelanceprojects\TexasPortalsDotNet\accountsportal_frontend\static\texas-logo.png",
            @"d:\freelanceprojects\TexasPortalsDotNet\salesportal_frontend\static\texas-logo.png"
        };

        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try { return File.ReadAllBytes(path); } catch { }
            }
        }
        return null;
    }
}