using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TexasPortal.Application.Common.Options;
using TexasPortal.Application.Features.Invoices.Models;
using TexasPortal.Application.Features.Invoices.Services;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;

namespace TexasPortal.Application.Features.Invoices.Builders;

public class ReceiptInvoiceBuilder : IInvoiceBuilder
{
    private readonly IOperationsReadService _operationsReadService;
    private readonly ISalesReadService _salesReadService;
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly InvoiceOptions _options;

    public ReceiptInvoiceBuilder(
        IOperationsReadService operationsReadService,
        ISalesReadService salesReadService,
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        IOptions<InvoiceOptions> options)
    {
        _operationsReadService = operationsReadService;
        _salesReadService = salesReadService;
        _db = db;
        _agentsApiClient = agentsApiClient;
        _options = options.Value;
    }

    public string SupportedEntityType => "receipt";

    public async Task<InvoiceDataModel?> BuildAsync(long id, long userId, CancellationToken ct = default)
    {
        string? serviceName = null;
        string? narration = null;
        decimal amount = 0;
        string? docNo = null;
        string? headName = null;
        string? clientType = null;
        string? salesmanName = null;
        DateTime createdAt = DateTime.UtcNow;

        var opReceipt = await _operationsReadService.GetReceiptByIdAsync(id, ct);
        if (opReceipt != null)
        {
            serviceName = opReceipt.Service;
            narration = opReceipt.Narration;
            amount = opReceipt.Amount;
            docNo = opReceipt.DocumentNumber;
            headName = opReceipt.HeadName;
            clientType = opReceipt.ClientType;
            salesmanName = opReceipt.SalesmanName;
            createdAt = opReceipt.CreatedAt;
        }
        else if (userId > 0)
        {
            var salesReceipt = await _salesReadService.GetReceiptByIdAsync(userId, id.ToString(), ct);
            if (salesReceipt != null)
            {
                serviceName = salesReceipt.Service;
                narration = salesReceipt.Narration;
                amount = salesReceipt.Amount;
                docNo = salesReceipt.DocumentNumber;
                headName = salesReceipt.HeadName;
                clientType = salesReceipt.ClientType;
                salesmanName = salesReceipt.SalesmanName;
                createdAt = salesReceipt.CreatedAt;
            }
        }

        if (opReceipt is null && serviceName is null) return null;

        var items = new List<InvoiceLineItem>();

        var desc = $"Payment Receipt: {serviceName ?? "General Service"} ({narration ?? "Payment Settlement"})";
        items.Add(new InvoiceLineItem
        {
            ItemNo = 1,
            Description = desc,
            Quantity = 1,
            Rate = amount,
            Price = amount,
            NetPrice = amount,
            TaxRateText = "VAT 0%",
            VatAmount = 0,
            Total = amount
        });

        decimal netTotal = amount;
        string prepBy = salesmanName ?? string.Empty;
        string accountantName = await _agentsApiClient.GetBookingApproverAsync("receipt", id.ToString(), docNo, ct) ?? string.Empty;

        return new InvoiceDataModel
        {
            InvoiceNo = string.IsNullOrWhiteSpace(docNo) ? $"RCP-{id}" : docNo,
            Date = createdAt,
            ServiceName = "PAYMENT RECEIPT",
            DocumentNumber = string.IsNullOrWhiteSpace(docNo) ? $"RCP-{id}" : docNo,
            CustomerName = headName ?? "Customer",
            CustomerHeadName = headName ?? string.Empty,
            ClientType = clientType ?? "customer",
            SalesmanEmail = prepBy,
            PreparedBy = prepBy,
            AccountantName = accountantName,
            RemarksNote = $"Payment Receipt - {headName ?? "Customer"}",
            Items = items,
            SubTotal = netTotal,
            TaxableAmount = 0,
            VatAmount = 0,
            RoundOff = 0,
            NetTotal = netTotal,
            AmountInWords = NumberToWordsConverter.ToBhdWords(netTotal),
            Options = _options
        };
    }

    private static bool IsPaymentApproved(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var s = status.Trim().ToLowerInvariant();
        return s == "success" || s == "approved" || s == "completed" || s == "paid";
    }

}