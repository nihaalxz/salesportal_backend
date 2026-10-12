using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TexasPortal.Application.Features.Documents.Models;
using TexasPortal.Application.Features.Invoices.Factory;
using TexasPortal.Application.Features.Invoices.Models;

namespace TexasPortal.Application.Features.Documents.Builders;

public class InvoiceDocumentBuilder : IDocumentBuilder
{
    private readonly IInvoiceBuilderFactory _invoiceBuilderFactory;
    private readonly DocumentOptions _options;

    public InvoiceDocumentBuilder(
        IInvoiceBuilderFactory invoiceBuilderFactory,
        IOptions<DocumentOptions> options)
    {
        _invoiceBuilderFactory = invoiceBuilderFactory;
        _options = options.Value ?? new DocumentOptions();
    }

    public bool CanBuild(string category, string type)
    {
        if (!string.Equals(category, "invoice", StringComparison.OrdinalIgnoreCase)) return false;
        return _invoiceBuilderFactory.GetBuilder(type) != null;
    }

    public async Task<DocumentDataModel?> BuildAsync(DocumentBuildArgs args, CancellationToken ct = default)
    {
        if (!args.EntityId.HasValue) return null;
        long id = args.EntityId.Value;
        long userId = args.UserId;
        string type = args.Type;

        var builder = _invoiceBuilderFactory.GetBuilder(type);
        if (builder == null) return null;

        InvoiceDataModel? inv = await builder.BuildAsync(id, userId, ct);
        if (inv == null) return null;

        bool isReceipt = string.Equals(type, "receipt", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(inv.ServiceName, "PAYMENT RECEIPT", StringComparison.OrdinalIgnoreCase);

        var model = new DocumentDataModel
        {
            Category = isReceipt ? "Receipt" : "Invoice",
            Title = isReceipt ? "RECEIPT" : "INVOICE",
            Subtitle = string.IsNullOrWhiteSpace(inv.ServiceName) ? (isReceipt ? "PAYMENT RECEIPT" : "TAX INVOICE") : inv.ServiceName.ToUpper(),
            DocumentNo = inv.InvoiceNo,
            Date = inv.Date,
            CustomerName = inv.CustomerName,
            CustomerContact = inv.CustomerContact,
            ClientType = inv.ClientType,
            SalesmanNameOrEmail = inv.SalesmanEmail,
            PreparedBy = inv.PreparedBy,
            AccountantName = inv.AccountantName,
            RemarksNote = inv.RemarksNote,
            AmountInWords = inv.AmountInWords,
            Options = new DocumentOptions
            {
                CompanyName = string.IsNullOrWhiteSpace(inv.Options.CompanyName) ? _options.CompanyName : inv.Options.CompanyName,
                LogoPath = string.IsNullOrWhiteSpace(inv.Options.LogoPath) ? _options.LogoPath : inv.Options.LogoPath,
                CrNumber = string.IsNullOrWhiteSpace(inv.Options.CrNumber) ? _options.CrNumber : inv.Options.CrNumber,
                VatNumber = string.IsNullOrWhiteSpace(inv.Options.VatNumber) ? _options.VatNumber : inv.Options.VatNumber,
                OfficeDetails = string.IsNullOrWhiteSpace(inv.Options.OfficeDetails) ? _options.OfficeDetails : inv.Options.OfficeDetails,
                Phone = string.IsNullOrWhiteSpace(inv.Options.Phone) ? _options.Phone : inv.Options.Phone,
                Email = string.IsNullOrWhiteSpace(inv.Options.Email) ? _options.Email : inv.Options.Email,
                Website = string.IsNullOrWhiteSpace(inv.Options.Website) ? _options.Website : inv.Options.Website,
                Note = string.IsNullOrWhiteSpace(inv.Options.Note) ? _options.Note : inv.Options.Note,
                BankDetails = new DocumentBankDetails
                {
                    BankName = inv.Options.BankDetails.BankName,
                    AccountName = inv.Options.BankDetails.AccountName,
                    AccountNo = inv.Options.BankDetails.AccountNo,
                    Iban = inv.Options.BankDetails.Iban
                }
            },
            MetaData = new List<KeyValuePair<string, string>>
            {
                new(isReceipt ? "Receipt No" : "Invoice No", inv.InvoiceNo),
                new("Date", inv.Date.ToString("dd/MM/yyyy")),
                new("Service", inv.ServiceName ?? "General")
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
                Rows = inv.Items.Select(item => new List<string>
                {
                    item.ItemNo.ToString(),
                    item.Description,
                    item.Quantity.ToString("0"),
                    item.Rate.ToString("0.000"),
                    item.Price.ToString("0.000"),
                    item.NetPrice.ToString("0.000"),
                    item.Total.ToString("0.000")
                }).ToList()
            },
            Totals = new DocumentTotals
            {
                SubTotal = inv.SubTotal,
                TaxableAmount = inv.TaxableAmount,
                VatAmount = inv.VatAmount,
                RoundOff = inv.RoundOff,
                NetTotal = inv.NetTotal,
                Currency = "BHD"
            }
        };

        return model;
    }
}
