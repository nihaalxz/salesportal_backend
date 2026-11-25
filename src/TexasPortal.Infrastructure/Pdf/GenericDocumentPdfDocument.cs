using System;
using System.IO;
using System.Linq;
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
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#334155").FontFamily("Lato"));

                page.Content().Column(col =>
                {
                    col.Item().Element(ComposeHeader);
                    col.Item().Element(ComposeContent);
                    col.Item().PaddingTop(16).Element(ComposeFooter);
                });
            });
    }

    private void ComposeHeader(IContainer container)
    {
        var accentColor = string.IsNullOrWhiteSpace(_model.Options.PrimaryColor) ? "#1b2559" : _model.Options.PrimaryColor;

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Left: Company Details (matches reference PDF header hierarchy)
                row.RelativeItem().Column(compCol =>
                {
                    compCol.Item().Text(_model.Options.CompanyName).Bold().FontSize(13).FontColor("#111827");
                    if (!string.IsNullOrWhiteSpace(_model.Options.VatNumber))
                        compCol.Item().PaddingTop(2).Text(_model.Options.VatNumber).FontSize(8.5f).FontColor("#334155");
                    if (!string.IsNullOrWhiteSpace(_model.Options.OfficeDetails))
                        compCol.Item().PaddingTop(1).Text(_model.Options.OfficeDetails).FontSize(8f).FontColor("#334155");
                    var crAndPhone = string.Join(", ", new[] { _model.Options.CrNumber, _model.Options.Phone }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(crAndPhone))
                        compCol.Item().PaddingTop(1).Text(crAndPhone).FontSize(8.5f).FontColor("#334155");
                    if (!string.IsNullOrWhiteSpace(_model.Options.Email) || !string.IsNullOrWhiteSpace(_model.Options.Website))
                        compCol.Item().PaddingTop(1).Text($"{_model.Options.Email} | {_model.Options.Website}").FontSize(8.5f).FontColor("#334155");
                });

                row.ConstantItem(16);

                // Right: Texas Logo
                row.ConstantItem(150).AlignRight().AlignMiddle().Column(logoCol =>
                {
                    byte[]? logoBytes = ResolveLogoBytes();
                    if (logoBytes != null && logoBytes.Length > 0)
                    {
                        logoCol.Item().AlignRight().Width(145).Image(logoBytes);
                    }
                    else
                    {
                        logoCol.Item().AlignRight().Text(_model.Options.CompanyName).Bold().FontSize(12).FontColor(accentColor);
                    }
                });
            });

            col.Item().PaddingTop(10).PaddingBottom(8).LineHorizontal(1.5f).LineColor(accentColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        var accentColor = string.IsNullOrWhiteSpace(_model.Options.PrimaryColor) ? "#1b2559" : _model.Options.PrimaryColor;

        container.Column(col =>
        {
            // Title & Document Header Bar
            if (!string.IsNullOrWhiteSpace(_model.Title) || !string.IsNullOrWhiteSpace(_model.Subtitle))
            {
                if (string.Equals(_model.Category, "Invoice", StringComparison.OrdinalIgnoreCase) || string.Equals(_model.Category, "Receipt", StringComparison.OrdinalIgnoreCase))
                {
                    col.Item().AlignCenter().Column(titleCol =>
                    {
                        if (!string.IsNullOrWhiteSpace(_model.Title))
                        {
                            titleCol.Item().AlignCenter().Text(_model.Title.ToUpper()).Bold().FontSize(15f).FontColor(accentColor);
                        }
                        if (!string.IsNullOrWhiteSpace(_model.Subtitle))
                        {
                            titleCol.Item().AlignCenter().PaddingTop(2).Text(_model.Subtitle.ToUpper()).Bold().FontSize(9f).FontColor("#64748b");
                        }
                    });
                }
                else
                {
                    col.Item().AlignCenter().Column(titleCol =>
                    {
                        titleCol.Item().AlignCenter().Text(_model.Title.ToUpper()).Bold().FontSize(14.5f).FontColor(accentColor);
                        if (!string.IsNullOrWhiteSpace(_model.Subtitle))
                        {
                            titleCol.Item().AlignCenter().PaddingTop(2).Text(_model.Subtitle).FontSize(8.5f).FontColor("#64748b");
                        }
                    });
                }
                col.Item().PaddingVertical(8);
            }

            // Metadata Row & Bill-To Info (borderless reference structure)
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
                            leftCol.Item().Text(isReceipt ? "Received From:" : "Bill To / Recipient:").Bold().FontSize(8.5f).FontColor("#111827");
                            leftCol.Item().PaddingTop(3).Text(_model.CustomerName.ToUpper()).Bold().FontSize(10.5f).FontColor("#0f172a");
                            if (!string.IsNullOrWhiteSpace(_model.ClientType))
                                leftCol.Item().PaddingTop(2).Text($"Type: {_model.ClientType}").FontSize(8.5f).FontColor("#334155");
                            if (!string.IsNullOrWhiteSpace(_model.CustomerContact))
                                leftCol.Item().PaddingTop(1).Text($"Contact: {_model.CustomerContact}").FontSize(8.5f).FontColor("#334155");
                        });
                    }

                    if (_model.MetaData.Count > 0)
                    {
                        row.RelativeItem().Column(rightCol =>
                        {
                            bool isReceipt = string.Equals(_model.Category, "Receipt", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(_model.Title, "RECEIPT", StringComparison.OrdinalIgnoreCase);
                            rightCol.Item().AlignRight().Text(isReceipt ? "Receipt Details" : "Invoice Details").Bold().FontSize(8.5f).FontColor("#111827");
                            foreach (var pair in _model.MetaData)
                            {
                                rightCol.Item().PaddingTop(2).AlignRight().Text(t =>
                                {
                                    t.Span($"{pair.Key} : ").FontSize(8.5f).FontColor("#475569");
                                    t.Span(pair.Value).Bold().FontSize(8.5f).FontColor("#0f172a");
                                });
                            }
                        });
                    }
                });

                col.Item().PaddingVertical(8);
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
                            var cell = header.Cell().Background(accentColor).PaddingVertical(6).PaddingHorizontal(5);
                            if (colDef.Alignment == ColumnAlignment.Center) cell = cell.AlignCenter();
                            else if (colDef.Alignment == ColumnAlignment.Right) cell = cell.AlignRight();

                            cell.Text(colDef.Header.ToUpper()).Bold().FontSize(8f).FontColor(Colors.White);
                        }
                    });

                    // Table Data Rows (clean white background, subtle bottom border)
                    foreach (var rowData in _model.Table.Rows)
                    {
                        for (int c = 0; c < _model.Table.Columns.Count; c++)
                        {
                            var colDef = _model.Table.Columns[c];
                            string val = c < rowData.Count ? rowData[c] : string.Empty;

                            var cell = table.Cell().Background(Colors.White).BorderBottom(0.5f).BorderColor("#cbd5e1").PaddingVertical(6).PaddingHorizontal(5);
                            if (colDef.Alignment == ColumnAlignment.Center) cell = cell.AlignCenter();
                            else if (colDef.Alignment == ColumnAlignment.Right) cell = cell.AlignRight();

                            var textElement = cell.Text(val).FontSize(8.5f).FontColor("#0f172a");
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

                            var cell = table.Cell().Background(Colors.White).BorderTop(0.75f).BorderBottom(0.75f).BorderColor("#64748b").PaddingVertical(6).PaddingHorizontal(5);
                            if (colDef.Alignment == ColumnAlignment.Center) cell = cell.AlignCenter();
                            else if (colDef.Alignment == ColumnAlignment.Right) cell = cell.AlignRight();

                            cell.Text(val).Bold().FontSize(8.5f).FontColor(accentColor);
                        }
                    }
                });
            }

            // Remarks / Notes Box
            if (!string.IsNullOrWhiteSpace(_model.RemarksNote))
            {
                col.Item().PaddingTop(8).BorderLeft(2.5f).BorderColor(accentColor).PaddingLeft(8).PaddingVertical(4).Text(_model.RemarksNote).FontSize(8f).FontColor("#334155");
            }

            col.Item().PaddingVertical(10);

            // Totals Component (Subtotal, VAT, Rounding, Net Total + Bank Info)
            if (_model.Totals != null)
            {
                col.Item().Row(row =>
                {
                    // Left Column (Amount in words, Note & Bank details - borderless reference layout)
                    row.RelativeItem(1.3f).Column(leftCol =>
                    {
                        if (!string.IsNullOrWhiteSpace(_model.AmountInWords))
                        {
                            leftCol.Item().Text("Amount In Words :").Bold().FontSize(8.5f).FontColor("#111827");
                            leftCol.Item().PaddingTop(2).Text(_model.AmountInWords).FontSize(8.5f).FontColor("#334155");
                        }

                        if (!string.IsNullOrWhiteSpace(_model.Options.Note))
                        {
                            leftCol.Item().PaddingTop(8).Text("Note:").Bold().FontSize(8.5f).FontColor("#111827");
                            leftCol.Item().PaddingTop(2).Text(_model.Options.Note).FontSize(8f).FontColor("#334155");
                        }

                        if (_model.Category.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
                        {
                            leftCol.Item().PaddingTop(10).Column(bankBox =>
                            {
                                bankBox.Item().Row(bHeader =>
                                {
                                    bHeader.RelativeItem().Text("BANK ACCOUNT DETAILS").Bold().FontSize(8.5f).FontColor(accentColor);
                                    if (!string.IsNullOrWhiteSpace(_model.Options.BankDetails.Currency))
                                    {
                                        bHeader.AutoItem().Text($"Currency: {_model.Options.BankDetails.Currency}").Bold().FontSize(8f).FontColor("#059669");
                                    }
                                });

                                bankBox.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor("#cbd5e1");

                                bankBox.Item().PaddingTop(4).Table(bTable =>
                                {
                                    bTable.ColumnsDefinition(bCols =>
                                    {
                                        bCols.ConstantColumn(85);
                                        bCols.RelativeColumn();
                                    });

                                    bTable.Cell().PaddingVertical(1.5f).Text("Account Name:").FontSize(8f).Bold().FontColor("#475569");
                                    bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.AccountName).FontSize(8f).Bold().FontColor("#0f172a");

                                    bTable.Cell().PaddingVertical(1.5f).Text("Bank Name:").FontSize(8f).Bold().FontColor("#475569");
                                    bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.BankName).FontSize(8f).FontColor("#334155");

                                    bTable.Cell().PaddingVertical(1.5f).Text("Account Number:").FontSize(8f).Bold().FontColor("#475569");
                                    bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.AccountNo).FontSize(8f).FontColor("#0f172a");

                                    bTable.Cell().PaddingVertical(1.5f).Text("IBAN:").FontSize(8f).Bold().FontColor("#475569");
                                    bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.Iban).FontSize(8f).FontColor("#0f172a");

                                    if (!string.IsNullOrWhiteSpace(_model.Options.BankDetails.SwiftCode))
                                    {
                                        bTable.Cell().PaddingVertical(1.5f).Text("Swift Code:").FontSize(8f).Bold().FontColor("#475569");
                                        bTable.Cell().PaddingVertical(1.5f).Text(_model.Options.BankDetails.SwiftCode).FontSize(8f).FontColor("#334155");
                                    }
                                });
                            });
                        }
                    });

                    row.ConstantItem(20);

                    // Right Column (Borderless sub-rows + solid brand-color Net Total highlight bar)
                    row.RelativeItem(1.0f).Column(rightCol =>
                    {
                        rightCol.Item().Table(totTable =>
                        {
                            totTable.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            if (_model.Totals.SubTotal.HasValue)
                            {
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(5).PaddingHorizontal(6).Text("Sub Total").FontSize(8.5f).FontColor("#334155");
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(5).PaddingHorizontal(6).AlignRight().Text($"{_model.Totals.SubTotal.Value:0.000}").FontSize(8.5f).FontColor("#0f172a");
                            }

                            if (_model.Totals.RoundOff.HasValue && _model.Totals.RoundOff.Value != 0)
                            {
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(5).PaddingHorizontal(6).Text("Round").FontSize(8.5f).FontColor("#334155");
                                totTable.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(5).PaddingHorizontal(6).AlignRight().Text($"{_model.Totals.RoundOff.Value:0.000}").FontSize(8.5f).FontColor("#0f172a");
                            }

                            // Net Total Highlight Bar (matching reference PDF solid bar in Texas primary color)
                            totTable.Cell().Background(accentColor).PaddingVertical(6).PaddingHorizontal(6).Text("Net Total").Bold().FontSize(9.5f).FontColor(Colors.White);
                            totTable.Cell().Background(accentColor).PaddingVertical(6).PaddingHorizontal(6).AlignRight().Text($"{_model.Totals.NetTotal:0.000} {_model.Totals.Currency}").Bold().FontSize(9.5f).FontColor(Colors.White);
                        });
                    });
                });
            }

            // Bottom Signature Section
            col.Item().PaddingTop(24);
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(sCol =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.SalesmanNameOrEmail))
                        sCol.Item().Text($"Salesman / Creator: {_model.SalesmanNameOrEmail}").FontSize(8f).FontColor("#475569");

                    if (!string.IsNullOrWhiteSpace(_model.AccountantName))
                        sCol.Item().Text($"Accountant: {_model.AccountantName}").FontSize(8f).FontColor("#475569");

                    sCol.Item().PaddingTop(8).Text("Thank You For Your Business").Bold().FontSize(9.5f).FontColor(accentColor);
                });

                row.RelativeItem().Column(sigCol =>
                {
                    sigCol.Item().AlignRight().Column(innerSig =>
                    {
                        innerSig.Item().AlignRight().Text($"For : {_model.Options.CompanyName}").FontSize(8f).FontColor("#334155");
                        innerSig.Item().PaddingTop(22).Width(120).LineHorizontal(0.5f).LineColor("#94a3b8");
                        innerSig.Item().PaddingTop(3).AlignCenter().Text("Authorized Signature").Bold().FontSize(8.5f).FontColor("#0f172a");
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

    private byte[]? ResolveLogoBytes() =>
        PdfAssetHelper.ResolveLogoBytes(_webRootPath, _model.Options.LogoPath);
}
