using System;
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

public class OtherServiceInvoiceBuilder : IInvoiceBuilder
{
    private readonly IOperationsReadService _operationsReadService;
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly InvoiceOptions _options;

    public OtherServiceInvoiceBuilder(
        IOperationsReadService operationsReadService,
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        IOptions<InvoiceOptions> options)
    {
        _operationsReadService = operationsReadService;
        _db = db;
        _agentsApiClient = agentsApiClient;
        _options = options.Value;
    }

    public string SupportedEntityType => "other";

    public async Task<InvoiceDataModel?> BuildAsync(long id, long userId, CancellationToken ct = default)
    {
        var booking = await _operationsReadService.GetOtherServiceByIdAsync(id, ct);

        if (booking is null) return null;

        var items = new List<InvoiceLineItem>();

        var desc = $"Service: {booking.ServiceName ?? "Other Service"} ({booking.Narration ?? "General Service"}) - {booking.Name ?? "Customer"}";
        items.Add(new InvoiceLineItem
        {
            ItemNo = 1,
            Description = desc,
            Quantity = 1,
            Rate = booking.PayableAmount,
            Price = booking.PayableAmount,
            NetPrice = booking.PayableAmount,
            TaxRateText = "VAT 0%",
            VatAmount = 0,
            Total = booking.PayableAmount
        });

        decimal netTotal = booking.PayableAmount;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == booking.CreatedBy, ct);
        string salesmanName = user?.Name ?? (booking.CreatedBy > 0 ? $"User #{booking.CreatedBy}" : string.Empty);

        string accountantName = await _agentsApiClient.GetBookingApproverAsync("other", id.ToString(), booking.DocumentNumber, ct) ?? string.Empty;

        return new InvoiceDataModel
        {
            InvoiceNo = string.IsNullOrWhiteSpace(booking.DocumentNumber) ? $"OTH-{booking.Id}" : booking.DocumentNumber,
            Date = booking.CreatedAt ?? DateTime.Now,
            ServiceName = "OTHER SERVICES",
            DocumentNumber = booking.DocumentNumber ?? $"OTH-{booking.Id}",
            CustomerName = booking.HeadName ?? booking.Agent ?? "Customer",
            CustomerHeadName = booking.HeadName ?? string.Empty,
            ClientType = booking.ClientType ?? "customer",
            SalesmanEmail = salesmanName,
            PreparedBy = salesmanName,
            AccountantName = accountantName,
            RemarksNote = $"Other Services - {booking.HeadName ?? booking.Agent ?? "Customer"}",
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