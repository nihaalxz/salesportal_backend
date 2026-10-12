using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TexasPortal.Application.Features.Documents.Models;

namespace TexasPortal.Infrastructure.Pdf;

public class GenericDocumentPdfDocument : IDocument
{
    private readonly DocumentDataModel _model;
    private readonly string _webRootPath;

    public GenericDocumentPdfDocument(DocumentDataModel model, string webRootPath)
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
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#334155").FontFamily("Arial"));

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
                        logoCol.Item().Text(_model.Options.CompanyName).Bold().FontSize(13).FontColor(_model.Options.PrimaryColor);
                    }
                });

                // Right: Company Details (Expanded width for perfect single-line address alignment)
                row.RelativeItem().Column(compCol =>
                {
                    compCol.Item().AlignRight().Text(_model.Options.CompanyName).Bold().FontSize(12).FontColor(_model.Options.PrimaryColor);
                    if (!string.IsNullOrWhiteSpace(_model.Options.VatNumber))
                        compCol.Item().AlignRight().Text(_model.Options.VatNumber).FontSize(8.5f).FontColor("#475569");
                    if (!string.IsNullOrWhiteSpace(_model.Options.OfficeDetails))
                        compCol.Item().AlignRight().Text(_model.Options.OfficeDetails).FontSize(7.8f).FontColor("#475569");
                    var crAndPhone = string.Join(", ", new[] { _model.Options.CrNumber, _model.Options.Phone }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(crAndPhone))
                        compCol.Item().AlignRight().Text(crAndPhone).FontSize(8.5f).FontColor("#475569");
                    if (!string.IsNullOrWhiteSpace(_model.Options.Email) || !string.IsNullOrWhiteSpace(_model.Options.Website))
                        compCol.Item().AlignRight().Text($"{_model.Options.Email} | {_model.Options.Website}").FontSize(8.5f).FontColor("#475569");
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#cbd5e1");
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Title & Document Header Bar
            if (!string.IsNullOrWhiteSpace(_model.Title) || !string.IsNullOrWhiteSpace(_model.Subtitle))
            {
                if (string.Equals(_model.Category, "Invoice", StringComparison.OrdinalIgnoreCase) || string.Equals(_model.Category, "Receipt", StringComparison.OrdinalIgnoreCase))
                {
                    // For Invoices: INVOICE title is large on top, Service Name is smaller below
                    col.Item().AlignCenter().Column(titleCol =>
                    {
                        if (!string.IsNullOrWhiteSpace(_model.Title))
                        {
                            titleCol.Item().AlignCenter().Text(_model.Title.ToUpper()).Bold().FontSize(18f).FontColor(_model.Options.PrimaryColor);
                        }
                        if (!string.IsNullOrWhiteSpace(_model.Subtitle))
                        {
                            titleCol.Item().AlignCenter().Text(_model.Subtitle.ToUpper()).Bold().FontSize(9.5f).FontColor("#64748b");
                        }
                    });
                }
                else
                {
                    // For Reports / Lists: Title is large, Subtitle is smaller date/info
                    col.Item().AlignCenter().Column(titleCol =>
                    {
                        titleCol.Item().AlignCenter().Text(_model.Title.ToUpper()).Bold().FontSize(15f).FontColor(_model.Options.PrimaryColor);
                        if (!string.IsNullOrWhiteSpace(_model.Subtitle))
                        {
                            titleCol.Item().AlignCenter().Text(_model.Subtitle).FontSize(8.5f).FontColor("#64748b");
                        }
                    });
                }
                col.Item().PaddingVertical(6);
            }

            // Metadata Row & Bill-To Info
            if (!string.IsNullOrWhiteSpace(_model.CustomerName) || _model.MetaData.Count > 0)
            {
                col.Item().Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.CustomerName))
                    {
                        row.RelativeItem().Column(leftCol =>
                        {
                            bool isReceipt = string.Equals(_model.Category, "Receipt", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(_model.Title, "RECEIPT", StringComparison.OrdinalIgnoreCase);
                            leftCol.Item().Text(isReceipt ? "Received From:" : "Bill To / Recipient:").FontSize(8.5f).FontColor("#64748b");
                            leftCol.Item().Text(_model.CustomerName.ToUpper()).Bold().FontSize(10.5f).FontColor("#0f172a");
                            if (!string.IsNullOrWhiteSpace(_model.ClientType))
                                leftCol.Item().Text($"Type: {_model.ClientType}").FontSize(8).FontColor("#475569");
                            if (!string.IsNullOrWhiteSpace(_model.CustomerContact))
                                leftCol.Item().Text($"Contact: {_model.CustomerContact}").FontSize(8).FontColor("#475569");
                        });
                    }

                    if (_model.MetaData.Count > 0)
                    {
                        row.RelativeItem().Column(rightCol =>
                        {
                            foreach (var pair in _model.MetaData)
                            {
                                rightCol.Item().AlignRight().Text(t =>
                                {
                                    t.Span($"{pair.Key} : ").FontSize(8.5f).FontColor("#64748b");
                                    t.Span(pair.Value).Bold().FontSize(9).FontColor("#0f172a");
                                });
                            }
                        });
                    }
                });

                col.Item().PaddingVertical(6);
            }



            // Dynamic Table Component
            if (_model.Table.Columns.Count > 0 && _model.Table.Rows.Count > 0)
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var colDef in _model.Table.Columns)
                        {
                            columns.RelativeColumn(colDef.RelativeWidth);
                        }
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        foreach (var colDef in _model.Table.Columns)
                        {
                            var cell = header.Cell().Background(_model.Options.PrimaryColor).PaddingVertical(5).PaddingHorizontal(4);
                            if (colDef.Alignment == ColumnAlignment.Center) cell = cell.AlignCenter();
                            else if (colDef.Alignment == ColumnAlignment.Right) cell = cell.AlignRight();

                            cell.Text(colDef.Header.ToUpper()).Bold().FontSize(7.5f).FontColor(Colors.White);
                        }
                    });

                    // Table Data Rows
                    foreach (var rowData in _model.Table.Rows)
                    {
                        for (int c = 0; c < _model.Table.Columns.Count; c++)
                        {
                            var colDef = _model.Table.Columns[c];
                            string val = c < rowData.Count ? rowData[c] : string.Empty;

                            var cell = table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(5).PaddingHorizontal(4);
                            if (colDef.Alignment == ColumnAlignment.Center) cell = cell.AlignCenter();
                            else if (colDef.Alignment == ColumnAlignment.Right) cell = cell.AlignRight();

                            var textElement = cell.Text(val).FontSize(8f);
                            if (colDef.IsBold) textElement = textElement.Bold();
                            if (!string.IsNullOrWhiteSpace(colDef.CustomColor)) textElement = textElement.FontColor(colDef.CustomColor);
                        }
                    }

                    // Optional Table Total Row
                    if (_model.Table.TotalRow != null && _model.Table.TotalRow.Count > 0)
                    {
                        for (int c = 0; c < _model.Table.Columns.Count; c++)
                        {
                            var colDef = _model.Table.Columns[c];
                            string val = c < _model.Table.TotalRow.Count ? _model.Table.TotalRow[c] : string.Empty;

                            var cell = table.Cell().Background("#f1f5f9").BorderTop(1f).BorderColor(_model.Options.PrimaryColor).PaddingVertical(6).PaddingHorizontal(4);
                            if (colDef.Alignment == ColumnAlignment.Center) cell = cell.AlignCenter();
                            else if (colDef.Alignment == ColumnAlignment.Right) cell = cell.AlignRight();

                            cell.Text(val).Bold().FontSize(8.5f).FontColor(_model.Options.PrimaryColor);
                        }
                    }
                });
            }

            // Remarks / Notes Box
            if (!string.IsNullOrWhiteSpace(_model.RemarksNote))
            {
                col.Item().PaddingTop(8).Background("#f8fafc").BorderLeft(3).BorderColor(_model.Options.PrimaryColor).Padding(6).Text(_model.RemarksNote).FontSize(8f).FontColor("#475569");
            }

            col.Item().PaddingVertical(8);

            // Totals Component (Subtotal, VAT, Rounding, Net Total + Bank Info)
            if (_model.Totals != null)
            {
                col.Item().Row(row =>
                {
                    // Left Column (Amount in words & Bank details)
                    row.RelativeItem(1.3f).Column(leftCol =>
                    {
                        if (!string.IsNullOrWhiteSpace(_model.AmountInWords))
                        {
                            leftCol.Item().Text(text =>
                            {
                                text.Span("Amount In Words : ").Bold().FontColor("#1e293b");
                                text.Span(_model.AmountInWords).FontColor("#334155");
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(_model.Options.Note))
                        {
                            leftCol.Item().PaddingTop(6).Text(text =>
                            {
                                text.Span("Note: ").Bold().FontColor("#1e293b");
                                text.Span(_model.Options.Note).FontColor("#334155");
                            });
                        }

                        if (_model.Category.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
                        {
                            leftCol.Item().PaddingTop(8).Background("#f8fafc").Border(0.5f).BorderColor("#e2e8f0").Padding(8).Column(bankBox =>
                            {
                                bankBox.Item().Row(bHeader =>
                                {
                                    bHeader.RelativeItem().Text("BANK ACCOUNT DETAILS").Bold().FontSize(8.5f).FontColor(_model.Options.PrimaryColor);
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
                        }
                    });

                    row.ConstantItem(15);

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

                            if (_model.Totals.SubTotal.HasValue)
                            {
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text("Sub Total").FontSize(8f).Bold();
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{_model.Totals.SubTotal.Value:0.000}").FontSize(8f);
                            }

                            if (_model.Totals.RoundOff.HasValue && _model.Totals.RoundOff.Value != 0)
                            {
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text("Round").FontSize(8f);
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{_model.Totals.RoundOff.Value:0.000}").FontSize(8f);
                            }

                            // Net Total Highlight Row
                            totTable.Cell().Background("#f1f5f9").Padding(5).Text("Net Total").Bold().FontSize(9.5f).FontColor(_model.Options.PrimaryColor);
                            totTable.Cell().Background("#f1f5f9").Padding(5).AlignRight().Text($"{_model.Totals.NetTotal:0.000} {_model.Totals.Currency}").Bold().FontSize(9.5f).FontColor(_model.Options.PrimaryColor);
                        });
                    });
                });
            }

            // Bottom Signature Section
            col.Item().PaddingTop(20);
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(sCol =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.SalesmanNameOrEmail))
                        sCol.Item().Text($"Salesman / Creator: {_model.SalesmanNameOrEmail}").FontSize(8f).FontColor("#475569");

                    if (!string.IsNullOrWhiteSpace(_model.AccountantName))
                        sCol.Item().Text($"Accountant: {_model.AccountantName}").FontSize(8f).FontColor("#475569");

                    sCol.Item().PaddingTop(8).Text("Thank You For Your Business").Bold().FontSize(9.5f).FontColor(_model.Options.PrimaryColor);
                });

                row.RelativeItem().Column(sigCol =>
                {
                    sigCol.Item().AlignRight().Column(innerSig =>
                    {
                        innerSig.Item().Width(110).LineHorizontal(0.5f).LineColor("#94a3b8");
                        innerSig.Item().PaddingTop(2).AlignCenter().Text("Authorized Signature").FontSize(8f).FontColor("#64748b");
                    });
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7.5f).FontColor("#94a3b8");

            row.RelativeItem().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber().FontSize(7.5f).FontColor("#94a3b8");
                x.Span(" of ").FontSize(7.5f).FontColor("#94a3b8");
                x.TotalPages().FontSize(7.5f).FontColor("#94a3b8");
            });

            row.RelativeItem().AlignRight().Text(_model.Options.CompanyName).FontSize(7.5f).FontColor("#94a3b8");
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