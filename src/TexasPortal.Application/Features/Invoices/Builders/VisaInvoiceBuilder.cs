using System;
using System.Collections.Generic;
using System.Linq;
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

public class VisaInvoiceBuilder : IInvoiceBuilder
{
    private readonly IOperationsReadService _operationsReadService;
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly InvoiceOptions _options;

    public VisaInvoiceBuilder(
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

    public string SupportedEntityType => "visa";

    public async Task<InvoiceDataModel?> BuildAsync(long id, long userId, CancellationToken ct = default)
    {
        var booking = await _operationsReadService.GetVisaApplicationByIdAsync(id, ct);

        if (booking is null || (userId > 0 && booking.UserId != userId)) return null;

        var items = new List<InvoiceLineItem>();
        int itemNo = 1;
        var countryName = booking.CountryName?.Trim();
        var packName = string.IsNullOrWhiteSpace(booking.VisaPackName) ? "Visa Application" : booking.VisaPackName.Trim();
        var serviceTitle = !string.IsNullOrWhiteSpace(countryName) &&
                           !packName.Contains(countryName, StringComparison.OrdinalIgnoreCase)
            ? $"{countryName} - {packName}"
            : packName;

        if (booking.Applicants != null && booking.Applicants.Count > 0)
        {
            foreach (var app in booking.Applicants)
            {
                var desc = $"{serviceTitle} - {app.FName} {app.LName} ({app.PassportNo})";
                items.Add(new InvoiceLineItem
                {
                    ItemNo = itemNo++,
                    Description = desc,
                    Quantity = 1,
                    Rate = app.PayableAmount,
                    Price = app.PayableAmount,
                    NetPrice = app.PayableAmount,
                    TaxRateText = "VAT 0%",
                    VatAmount = 0,
                    Total = app.PayableAmount
                });
            }
        }
        else
        {
            items.Add(new InvoiceLineItem
            {
                ItemNo = 1,
                Description = serviceTitle,
                Quantity = 1,
                Rate = booking.PayableAmount,
                Price = booking.PayableAmount,
                NetPrice = booking.PayableAmount,
                TaxRateText = "VAT 0%",
                VatAmount = 0,
                Total = booking.PayableAmount
            });
        }

        decimal netTotal = items.Sum(i => i.Total);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == booking.UserId, ct);
        string salesmanName = user?.Name ?? (booking.UserId > 0 ? $"User #{booking.UserId}" : string.Empty);

        string accountantName = await _agentsApiClient.GetBookingApproverAsync("visa", id.ToString(), booking.DocumentNumber, ct) ?? string.Empty;

        return new InvoiceDataModel
        {
            InvoiceNo = string.IsNullOrWhiteSpace(booking.DocumentNumber) ? $"VIS-{booking.Id}" : booking.DocumentNumber,
            Date = booking.CreatedAt ?? DateTime.Now,
            ServiceName = "VISA APPLICATION",
            DocumentNumber = booking.DocumentNumber ?? $"VIS-{booking.Id}",
            CustomerName = booking.HeadName ?? booking.Agent ?? "Customer",
            CustomerHeadName = booking.HeadName ?? string.Empty,
            ClientType = booking.ClientType ?? "customer",
            SalesmanEmail = salesmanName,
            PreparedBy = salesmanName,
            AccountantName = accountantName,
            RemarksNote = !string.IsNullOrWhiteSpace(countryName)
                ? $"Visa Application ({countryName}) - {booking.HeadName ?? booking.Agent ?? "Customer"}"
                : $"Visa Application - {booking.HeadName ?? booking.Agent ?? "Customer"}",
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