using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TexasPortal.Application.Features.Documents.Models;
using TexasPortal.Application.Sales.Queries;

namespace TexasPortal.Application.Features.Documents.Builders;

public class ListReportDocumentBuilder : IDocumentBuilder
{
    private readonly ISalesReadService _salesReadService;
    private readonly IOperationsReadService _operationsReadService;
    private readonly DocumentOptions _options;

    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "visa", "flight", "hotel", "holiday", "transport", "vfs", "other", "receipt", "ledger"
    };

    public ListReportDocumentBuilder(
        ISalesReadService salesReadService,
        IOperationsReadService operationsReadService,
        IOptions<DocumentOptions> options)
    {
        _salesReadService = salesReadService;
        _operationsReadService = operationsReadService;
        _options = options.Value ?? new DocumentOptions();
    }

    private static string NormalizeType(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return string.Empty;
        var key = type.Trim().ToLowerInvariant().Replace("-", "").Replace("_", "");
        if (key is "visa" or "visas" or "visaapplication" or "visaapplications" or "salesvisa") return "visa";
        if (key is "flight" or "flights" or "flightbooking" or "flightbookings") return "flight";
        if (key is "hotel" or "hotels" or "hotelbooking" or "hotelbookings") return "hotel";
        if (key is "holiday" or "holidays" or "holidaypackage" or "holidaypackages") return "holiday";
        if (key is "transport" or "transportation" or "transportations" or "transportbooking" or "transports") return "transport";
        if (key is "vfs" or "vfsappointment" or "vfsappointments") return "vfs";
        if (key is "other" or "others" or "otherservice" or "otherservices") return "other";
        if (key is "receipt" or "receipts" or "paymentreceipt") return "receipt";
        if (key is "ledger" or "ledgers") return "ledger";
        return key;
    }

    public bool CanBuild(string category, string type)
    {
        var norm = NormalizeType(type);
        return (string.Equals(category, "list", StringComparison.OrdinalIgnoreCase) || string.Equals(category, "report", StringComparison.OrdinalIgnoreCase)) && SupportedTypes.Contains(norm);
    }

    public async Task<DocumentDataModel?> BuildAsync(DocumentBuildArgs args, CancellationToken ct = default)
    {
        long userId = args.UserId;
        string type = NormalizeType(args.Type);
        var q = args.QueryParams;

        string? search = q.TryGetValue("searchTerm", out var s) ? s : null;
        string? payStatus = q.TryGetValue("paymentStatus", out var ps) ? ps : null;
        string? procStatus = q.TryGetValue("processingStatus", out var prs) ? prs : null;

        var model = new DocumentDataModel
        {
            Category = "List",
            Title = $"{type.ToUpper()} BOOKINGS REPORT",
            Subtitle = $"Generated Export Report on {DateTime.Now:dd/MM/yyyy HH:mm}",
            Date = DateTime.Now,
            Options = _options
        };

        if (!string.IsNullOrWhiteSpace(search))
            model.ActiveFilters.Add(new KeyValuePair<string, string>("Search", search));
        if (!string.IsNullOrWhiteSpace(payStatus))
            model.ActiveFilters.Add(new KeyValuePair<string, string>("Payment Status", payStatus));
        if (!string.IsNullOrWhiteSpace(procStatus))
            model.ActiveFilters.Add(new KeyValuePair<string, string>("Processing Status", procStatus));

        if (type == "visa")
        {
            model.Title = "VISA APPLICATIONS EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.VisaApplicationSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetVisaApplicationsAsync(userId, new VisaApplicationsListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetVisaApplicationsAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.VisaApplicationSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);
            decimal totalProfit = items.Sum(x => x.Profit);

            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Applications", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Profit", Value = $"{totalProfit:0.000} BHD", HighlightColor = "#2563eb" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "HEAD / APPLICANT", RelativeWidth = 2.2f, IsBold = true },
                new() { Header = "PACKAGE", RelativeWidth = 1.5f },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PROCESSING", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.HeadName ?? "Applicant",
                    x.VisaPackName ?? "Standard",
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.ProcessingStatus?.ToUpper() ?? "NEW",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "flight")
        {
            model.Title = "FLIGHT BOOKINGS EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.FlightBookingSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetFlightBookingsAsync(userId, new FlightBookingsListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetFlightBookingsAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.FlightBookingSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);

            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Flight Bookings", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "HEAD NAME", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "ROUTE", RelativeWidth = 2.0f },
                new() { Header = "PAX", RelativeWidth = 0.5f, Alignment = ColumnAlignment.Center },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.HeadName ?? "Passenger",
                    $"{x.FromLocation} -> {x.ToLocation}",
                    x.PassengerCount.ToString(),
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "hotel")
        {
            model.Title = "HOTEL BOOKINGS EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.HotelBookingSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetHotelBookingsAsync(userId, new HotelBookingsListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetHotelBookingsAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.HotelBookingSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);
            decimal totalProfit = items.Sum(x => x.Profit);

            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Hotel Bookings", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Profit", Value = $"{totalProfit:0.000} BHD", HighlightColor = "#2563eb" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "GUEST NAME", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "COUNTRY", RelativeWidth = 1.5f },
                new() { Header = "DAYS", RelativeWidth = 0.6f, Alignment = ColumnAlignment.Center },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.GuestName,
                    x.Country,
                    x.Days.ToString(),
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "holiday")
        {
            model.Title = "HOLIDAY PACKAGES EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.HolidayPackageSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetHolidayPackagesAsync(userId, new HolidayPackagesListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetHolidayPackagesAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.HolidayPackageSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);
            decimal totalProfit = items.Sum(x => x.Profit);

            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Holiday Packages", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "PASSENGER NAME", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "DESTINATION", RelativeWidth = 1.5f },
                new() { Header = "DAYS", RelativeWidth = 0.6f, Alignment = ColumnAlignment.Center },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.PassengerName,
                    x.Country,
                    x.Days.ToString(),
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "transport")
        {
            model.Title = "TRANSPORTATION BOOKINGS EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.TransportationSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetTransportationsAsync(userId, new TransportationsListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetTransportationsAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.TransportationSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Transportations", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "NAME", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "ROUTE", RelativeWidth = 2.0f },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.Name ?? x.HeadName ?? "Passenger",
                    $"{x.Pickup} -> {x.Drop}",
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "vfs")
        {
            model.Title = "VFS APPOINTMENTS EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.VfsAppointmentSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetVfsAppointmentsAsync(userId, new VfsAppointmentsListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetVfsAppointmentsAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.VfsAppointmentSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total VFS Appointments", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "APPLICANT NAME", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "COUNTRY", RelativeWidth = 1.5f },
                new() { Header = "VISA TYPE", RelativeWidth = 1.2f },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.Name,
                    x.Country,
                    x.VisaType,
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "other")
        {
            model.Title = "OTHER SERVICES EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Bookings.OtherServiceSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetOtherServicesAsync(userId, new OtherServicesListQuery(1, 1000, payStatus, procStatus, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetOtherServicesAsync(1, 1000, payStatus, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Bookings.OtherServiceSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.PayableAmount);
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Service Bookings", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Sales Value", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "ID", RelativeWidth = 0.8f, IsMonoFont = true },
                new() { Header = "CLIENT NAME", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "SERVICE NAME", RelativeWidth = 2.0f },
                new() { Header = "PAYMENT", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "PRICE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"#{x.Id}",
                    x.Name ?? x.HeadName ?? "Client",
                    x.ServiceName ?? "Custom Service",
                    x.PaymentStatus?.ToUpper() ?? "PENDING",
                    x.PayableAmount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "receipt")
        {
            model.Title = "PAYMENT RECEIPTS EXPORT REPORT";
            var items = new List<TexasPortal.Contracts.Sales.Receipts.ReceiptSummaryResponse>();

            if (userId > 0)
            {
                var res = await _salesReadService.GetReceiptsAsync(userId, new ReceiptsListQuery(1, 1000, search), ct);
                if (res.Items != null && res.Items.Count > 0) items = res.Items.ToList();
            }
            if (items.Count == 0)
            {
                var resOps = await _operationsReadService.GetReceiptsAsync(1, 1000, search, ct: ct);
                items = resOps.Items?.ToList() ?? new List<TexasPortal.Contracts.Sales.Receipts.ReceiptSummaryResponse>();
            }

            decimal totalSales = items.Sum(x => x.Amount);
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Receipts", Value = items.Count.ToString() });
            model.SummaryCards.Add(new DocumentSummaryCard { Label = "Total Collected", Value = $"{totalSales:0.000} BHD", HighlightColor = "#059669" });

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "RECEIPT NO", RelativeWidth = 1.0f, IsMonoFont = true },
                new() { Header = "CLIENT HEAD", RelativeWidth = 2.0f, IsBold = true },
                new() { Header = "DATE", RelativeWidth = 1.0f, Alignment = ColumnAlignment.Center },
                new() { Header = "AMOUNT (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.DocumentNumber ?? $"RCT-{x.ReceiptId}",
                    x.HeadName,
                    x.CreatedAt.ToString("dd/MM/yyyy"),
                    x.Amount.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Record(s)", "", $"{totalSales:0.000} BHD" };
        }
        else if (type == "ledger")
        {
            model.Title = "STATEMENT OF ACCOUNT";
            long headId = 0;
            if (q.TryGetValue("entityId", out var eidStr) && long.TryParse(eidStr, out var eid)) headId = eid;
            else if (q.TryGetValue("headId", out var hidStr) && long.TryParse(hidStr, out var hid)) headId = hid;

            DateTime? startDate = null;
            DateTime? endDate = null;
            if (q.TryGetValue("startDate", out var sStr) && DateTime.TryParse(sStr, out var sVal)) startDate = sVal;
            if (q.TryGetValue("endDate", out var eStr) && DateTime.TryParse(eStr, out var eVal)) endDate = eVal;
            string? headType = q.TryGetValue("type", out var tStr) ? tStr : null;
            string? sort = q.TryGetValue("sort", out var sortStr) ? sortStr : null;

            var items = new List<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>();
            if (headId > 0)
            {
                var ledgerRes = await _salesReadService.GetHeadLedgerAsync(userId, headId, 1, 1000, headType, startDate, endDate, null, null, sort, ct);
                if (ledgerRes.Items != null && ledgerRes.Items.Count > 0)
                {
                    items = ledgerRes.Items.ToList();
                }
            }

            decimal totalDebit = items.Sum(x => x.Debit);
            decimal totalCredit = items.Sum(x => x.Credit);
            decimal netBal = items.LastOrDefault()?.Balance ?? 0;

            model.Table.Columns = new List<DocumentColumn>
            {
                new() { Header = "DATE & TIME", RelativeWidth = 1.4f, IsMonoFont = true },
                new() { Header = "SERVICE TYPE", RelativeWidth = 1.3f },
                new() { Header = "DESCRIPTION", RelativeWidth = 2.2f },
                new() { Header = "METHOD", RelativeWidth = 0.9f, Alignment = ColumnAlignment.Center },
                new() { Header = "DEBIT (BHD)", RelativeWidth = 1.1f, Alignment = ColumnAlignment.Right },
                new() { Header = "CREDIT (BHD)", RelativeWidth = 1.1f, Alignment = ColumnAlignment.Right },
                new() { Header = "BALANCE (BHD)", RelativeWidth = 1.2f, Alignment = ColumnAlignment.Right, IsBold = true }
            };

            foreach (var x in items)
            {
                model.Table.Rows.Add(new List<string>
                {
                    x.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    x.Type ?? "Ledger",
                    x.Description ?? "—",
                    x.PaymentMethod?.ToUpper() ?? "—",
                    x.Debit > 0 ? x.Debit.ToString("0.000") : "—",
                    x.Credit > 0 ? x.Credit.ToString("0.000") : "—",
                    x.Balance.ToString("0.000")
                });
            }

            model.Table.TotalRow = new List<string> { "TOTAL", $"{items.Count} Entries", "", "", $"{totalDebit:0.000}", $"{totalCredit:0.000}", $"{netBal:0.000} BHD" };
        }

        return model;
    }
}
