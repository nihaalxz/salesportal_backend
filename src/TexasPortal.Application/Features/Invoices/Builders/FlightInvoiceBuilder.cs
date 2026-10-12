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

public class FlightInvoiceBuilder : IInvoiceBuilder
{
    private readonly IOperationsReadService _operationsReadService;
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly InvoiceOptions _options;

    public FlightInvoiceBuilder(
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

    public string SupportedEntityType => "flight";

    public async Task<InvoiceDataModel?> BuildAsync(long id, long userId, CancellationToken ct = default)
    {
        var booking = await _operationsReadService.GetFlightBookingByIdAsync(id, ct);

        if (booking is null) return null;

        var items = new List<InvoiceLineItem>();
        int itemNo = 1;

        if (booking.Passengers != null && booking.Passengers.Count > 0)
        {
            foreach (var pax in booking.Passengers)
            {
                var desc = $"Flight: {booking.FromLocation} to {booking.ToLocation} - {pax.PassengerName}";
                items.Add(new InvoiceLineItem
                {
                    ItemNo = itemNo++,
                    Description = desc,
                    Quantity = 1,
                    Rate = pax.PayableAmount,
                    Price = pax.PayableAmount,
                    NetPrice = pax.PayableAmount,
                    TaxRateText = "VAT 0%",
                    VatAmount = 0,
                    Total = pax.PayableAmount
                });
            }
        }
        else
        {
            items.Add(new InvoiceLineItem
            {
                ItemNo = 1,
                Description = $"Flight Booking: {booking.FromLocation} to {booking.ToLocation}",
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

        string accountantName = await _agentsApiClient.GetBookingApproverAsync("flight", id.ToString(), booking.DocumentNumber, ct) ?? string.Empty;

        return new InvoiceDataModel
        {
            InvoiceNo = string.IsNullOrWhiteSpace(booking.DocumentNumber) ? $"FLT-{booking.Id}" : booking.DocumentNumber,
            Date = booking.CreatedAt ?? DateTime.Now,
            ServiceName = "FLIGHT BOOKING",
            DocumentNumber = booking.DocumentNumber ?? $"FLT-{booking.Id}",
            CustomerName = booking.HeadName ?? booking.Agent ?? "Customer",
            CustomerHeadName = booking.HeadName ?? string.Empty,
            ClientType = booking.ClientType ?? "customer",
            SalesmanEmail = salesmanName,
            PreparedBy = salesmanName,
            AccountantName = accountantName,
            RemarksNote = $"Flight Booking - {booking.HeadName ?? booking.Agent ?? "Customer"}",
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