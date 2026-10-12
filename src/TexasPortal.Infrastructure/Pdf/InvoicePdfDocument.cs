using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TexasPortal.Application.Features.Invoices.Models;

namespace TexasPortal.Infrastructure.Pdf;

public class InvoicePdfDocument : IDocument
{
    private readonly InvoiceDataModel _model;
    private readonly string _webRootPath;

    public InvoicePdfDocument(InvoiceDataModel model, string webRootPath)
    {
        _model = model;
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
                page.DefaultTextStyle(x => x.FontSize(9).FontColor("#334155").FontFamily("Arial"));

                page.Content().Border(1).BorderColor("#94a3b8").Padding(16).Column(col =>
                {
                    col.Item().Element(ComposeHeader);
                    col.Item().Element(ComposeContent);
                    col.Item().PaddingTop(15).Element(ComposeFooter);
                });
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
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
                        logoCol.Item().Text(_model.Options.CompanyName).Bold().FontSize(13).FontColor("#1b2559");
                    }
                });

                // Right: Company Details (Expanded width for perfect single-line address alignment)
                row.RelativeItem().Column(compCol =>
                {
                    compCol.Item().AlignRight().Text(_model.Options.CompanyName).Bold().FontSize(12).FontColor("#1b2559");
                    if (!string.IsNullOrWhiteSpace(_model.Options.OfficeDetails))
                        compCol.Item().AlignRight().Text(_model.Options.OfficeDetails).FontSize(7.8f).FontColor("#475569");
                    var crAndPhone = string.Join(", ", new[] { _model.Options.CrNumber, _model.Options.Phone }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(crAndPhone))
                        compCol.Item().AlignRight().Text(crAndPhone).FontSize(8.5f).FontColor("#475569");
                    compCol.Item().AlignRight().Text($"{_model.Options.Email} | {_model.Options.Website}").FontSize(8.5f).FontColor("#475569");
                });
            });

            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#cbd5e1");
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Sub-Header metadata row
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(leftCol =>
                {
                    bool isReceipt = string.Equals(_model.ServiceName, "PAYMENT RECEIPT", StringComparison.OrdinalIgnoreCase) ||
                                     _model.InvoiceNo.StartsWith("RCP", StringComparison.OrdinalIgnoreCase) ||
                                     _model.InvoiceNo.StartsWith("TXS-ARCPT", StringComparison.OrdinalIgnoreCase);
                    leftCol.Item().Text(isReceipt ? "Received From:" : "Invoice to:").FontSize(9).FontColor("#64748b");
                    leftCol.Item().Text(_model.CustomerName.ToUpper()).Bold().FontSize(11).FontColor("#0f172a");
                });

                row.RelativeItem().Column(rightCol =>
                {
                    rightCol.Item().AlignRight().Text(text =>
                    {
                        bool isReceiptNum = string.Equals(_model.ServiceName, "PAYMENT RECEIPT", StringComparison.OrdinalIgnoreCase) ||
                                            _model.InvoiceNo.StartsWith("RCP", StringComparison.OrdinalIgnoreCase) ||
                                            _model.InvoiceNo.StartsWith("TXS-ARCPT", StringComparison.OrdinalIgnoreCase);
                        text.Span(isReceiptNum ? "Receipt No : " : "Invoice No : ").FontSize(9).FontColor("#64748b");
                        text.Span(_model.InvoiceNo).Bold().FontSize(10).FontColor("#0f172a");
                    });

                    rightCol.Item().AlignRight().Text(text =>
                    {
                        text.Span("Date : ").FontSize(9).FontColor("#64748b");
                        text.Span(_model.Date.ToString("dd/MM/yyyy")).Bold().FontSize(9.5f).FontColor("#0f172a");
                    });

                    rightCol.Item().AlignRight().Text(text =>
                    {
                        text.Span("Service : ").FontSize(9).FontColor("#64748b");
                        text.Span(_model.ServiceName.ToUpper()).Bold().FontSize(9.5f).FontColor("#0f172a");
                    });
                });
            });

            // Center Heading: Service Name smaller, Invoice No larger
            col.Item().PaddingVertical(10).AlignCenter().Column(titleCol =>
            {
                if (!string.IsNullOrWhiteSpace(_model.ServiceName))
                    titleCol.Item().AlignCenter().Text(_model.ServiceName.ToUpper()).Bold().FontSize(10f).FontColor("#64748b");
                bool isRcpHead = string.Equals(_model.ServiceName, "PAYMENT RECEIPT", StringComparison.OrdinalIgnoreCase) ||
                                 _model.InvoiceNo.StartsWith("RCP", StringComparison.OrdinalIgnoreCase) ||
                                 _model.InvoiceNo.StartsWith("TXS-ARCPT", StringComparison.OrdinalIgnoreCase);
                titleCol.Item().AlignCenter().Text(isRcpHead ? $"Receipt #{_model.InvoiceNo}" : $"Invoice #{_model.InvoiceNo}").Bold().FontSize(16f).FontColor("#1b2559");
            });

            // Line Items Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // S NO.
                    columns.RelativeColumn(3);  // DESCRIPTION
                    columns.ConstantColumn(40);  // QTY
                    columns.ConstantColumn(60);  // RATE
                    columns.ConstantColumn(60);  // PRICE
                    columns.ConstantColumn(60);  // N.PRICE
                    columns.ConstantColumn(65);  // TOTAL
                });

                // Header Row
                table.Header(header =>
                {
                    static IContainer HeaderStyle(IContainer cell) =>
                        cell.Background("#1b2559").PaddingVertical(6).PaddingHorizontal(4).AlignCenter();

                    header.Cell().Element(HeaderStyle).Text("S NO.").Bold().FontSize(8).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("DESCRIPTION").Bold().FontSize(8).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("QTY").Bold().FontSize(8).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("RATE").Bold().FontSize(8).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("PRICE").Bold().FontSize(8).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("N.PRICE").Bold().FontSize(8).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("TOTAL").Bold().FontSize(8).FontColor(Colors.White);
                });

                // Table Data Rows
                foreach (var item in _model.Items)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).AlignCenter().Text(item.ItemNo.ToString()).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).Text(item.Description).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).AlignCenter().Text(item.Quantity.ToString("0")).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).AlignRight().Text(item.Rate.ToString("0.000")).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).AlignRight().Text(item.Price.ToString("0.000")).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).AlignRight().Text(item.NetPrice.ToString("0.000")).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(6).PaddingHorizontal(4).AlignRight().Text(item.Total.ToString("0.000")).FontSize(8.5f);
                }
            });

            // Remarks box below table
            if (!string.IsNullOrWhiteSpace(_model.RemarksNote))
            {
                col.Item().PaddingTop(8).Background("#f8fafc").BorderLeft(3).BorderColor("#1b2559").Padding(8).Text(_model.RemarksNote).FontSize(8.5f).FontColor("#475569");
            }

            col.Item().PaddingVertical(15);

            // Footer Breakdown (Two Columns: Bank & Words on Left, Totals on Right)
            col.Item().Row(row =>
            {
                // Left Column
                row.RelativeItem(1.4f).Column(leftCol =>
                {
                    leftCol.Item().Text(text =>
                    {
                        text.Span("Amount In Words : ").Bold().FontColor("#1e293b");
                        text.Span(_model.AmountInWords).FontColor("#334155");
                    });

                    leftCol.Item().PaddingTop(10).Text(text =>
                    {
                        text.Span("Note: ").Bold().FontColor("#1e293b");
                        text.Span(_model.Options.Note).FontColor("#334155");
                    });

                    leftCol.Item().PaddingTop(10).Background("#f8fafc").Border(0.5f).BorderColor("#e2e8f0").Padding(8).Column(bankBox =>
                    {
                        bankBox.Item().Row(bHeader =>
                        {
                            bHeader.RelativeItem().Text("BANK ACCOUNT DETAILS").Bold().FontSize(8.5f).FontColor("#1b2559");
                            if (!string.IsNullOrWhiteSpace(_model.Options.BankDetails.Currency))
                            {
                                bHeader.AutoItem().Text($"Currency: {_model.Options.BankDetails.Currency}").Bold().FontSize(8f).FontColor("#059669");
                            }
                        });

                        bankBox.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor("#e2e8f0");

                        bankBox.Item().PaddingTop(5).Table(bTable =>
                        {
                            bTable.ColumnsDefinition(bCols =>
                            {
                                bCols.ConstantColumn(80);
                                bCols.RelativeColumn();
                            });

                            bTable.Cell().PaddingVertical(1.5f).Text("Account Name:").FontSize(7.5f).Bold().FontColor("#64748b");
                            bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.AccountName).FontSize(7.5f).Bold().FontColor("#0f172a");

                            bTable.Cell().PaddingVertical(1.5f).Text("Bank Name:").FontSize(7.5f).Bold().FontColor("#64748b");
                            bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.BankName).FontSize(7.5f).FontColor("#334155");

                            bTable.Cell().PaddingVertical(1.5f).Text("Account Number:").FontSize(7.5f).Bold().FontColor("#64748b");
                            bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.AccountNo).FontSize(7.5f).FontColor("#0f172a");

                            bTable.Cell().PaddingVertical(1.5f).Text("IBAN:").FontSize(7.5f).Bold().FontColor("#64748b");
                            bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.Iban).FontSize(7.5f).FontColor("#0f172a");

                            if (!string.IsNullOrWhiteSpace(_model.Options.BankDetails.SwiftCode))
                            {
                                bTable.Cell().PaddingVertical(1.5f).Text("Swift Code:").FontSize(7.5f).Bold().FontColor("#64748b");
                                bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.SwiftCode).FontSize(7.5f).FontColor("#334155");
                            }
                        });
                    });
                });

                row.ConstantItem(20);

                // Right Column (Totals Box)
                row.RelativeItem(1.0f).Column(rightCol =>
                {
                    rightCol.Item().Border(0.5f).BorderColor("#cbd5e1").Table(totTable =>
                    {
                        totTable.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });



                        if (_model.RoundOff != 0)
                        {
                            totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text("Round").FontSize(8.5f);
                            totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text(_model.RoundOff.ToString("0.000")).FontSize(8.5f);
                        }

                        // Net Total Row Highlighted
                        totTable.Cell().Background("#f1f5f9").Padding(6).Text("Net Total").Bold().FontSize(10).FontColor("#1b2559");
                        totTable.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text($"{_model.NetTotal:0.000} ب.د").Bold().FontSize(10).FontColor("#1b2559");
                    });
                });
            });

            col.Item().PaddingTop(30);

            // Bottom Signature & Salesman row
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(sCol =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.SalesmanEmail))
                        sCol.Item().Text($"Salesman / Creator: {_model.SalesmanEmail}").FontSize(8.5f).FontColor("#475569");

                    if (!string.IsNullOrWhiteSpace(_model.AccountantName))
                        sCol.Item().Text($"Accountant: {_model.AccountantName}").FontSize(8.5f).FontColor("#475569");

                    sCol.Item().PaddingTop(12).Text("Thank You For Your Business").Bold().FontSize(10).FontColor("#1b2559");
                });

                row.RelativeItem().Column(sigCol =>
                {
                    sigCol.Item().AlignRight().Column(innerSig =>
                    {
                        innerSig.Item().Width(120).LineHorizontal(0.5f).LineColor("#94a3b8");
                        innerSig.Item().PaddingTop(3).AlignCenter().Text("Signature").FontSize(8.5f).FontColor("#64748b");
                    });
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.CurrentPageNumber().FontSize(8).FontColor("#94a3b8");
            x.Span(" of ").FontSize(8).FontColor("#94a3b8");
            x.TotalPages().FontSize(8).FontColor("#94a3b8");
        });
    }

    private byte[]? ResolveLogoBytes()
    {
        var candidates = new[]
        {
            Path.Combine(_webRootPath ?? "", _model.Options.LogoPath?.TrimStart('/', '\\') ?? "texas-logo.png"),
            Path.Combine(_webRootPath ?? "", "texas-logo.png"),
            Path.Combine(_webRootPath ?? "", "static", "texas-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "texas-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "static", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "static", "texas-logo.png"),
            @"d:\freelanceprojects\TexasPortalsDotNet\accountsportal_frontend\static\texas-logo.png"
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