using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Dashboard;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Contracts.Sales.Receipts;
using TexasPortal.Contracts.Sales.Salesmen;
using System.Collections.Generic;

namespace TexasPortal.Application.Sales.Queries;

public sealed class OperationsReadService : IOperationsReadService
{
    private readonly ISalesPortalDbContext _db;
    private readonly ISalesPortalDbContextFactory _dbContextFactory;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly IOperationalAssignmentClient _assignmentClient;
    private readonly TexasPortal.Application.Sales.Services.ISupplierCostReconciliationService _reconciliationService;
    private readonly Microsoft.Extensions.Logging.ILogger<OperationsReadService>? _logger;

    public OperationsReadService(
        ISalesPortalDbContext db,
        ISalesPortalDbContextFactory dbContextFactory,
        IAgentsApiClient agentsApiClient,
        IOperationalAssignmentClient assignmentClient,
        TexasPortal.Application.Sales.Services.ISupplierCostReconciliationService reconciliationService,
        Microsoft.Extensions.Logging.ILogger<OperationsReadService>? logger = null)
    {
        _db = db;
        _dbContextFactory = dbContextFactory;
        _agentsApiClient = agentsApiClient;
        _assignmentClient = assignmentClient;
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    public async Task<IEnumerable<SalesmanResponse>> GetSalesmenAsync(CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(x => x.IsActive && (x.UserType == null || x.UserType != "Guest"))
            .OrderBy(x => x.Name)
            .Select(x => new SalesmanResponse
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SalesmanServiceEntryResponse>> GetSalesmanServiceEntriesAsync(long salesmanId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var list = new List<SalesmanServiceEntryResponse>();

        using var dbFlights = _dbContextFactory.CreateDbContext();
        using var dbHotels = _dbContextFactory.CreateDbContext();
        using var dbVisas = _dbContextFactory.CreateDbContext();
        using var dbVfs = _dbContextFactory.CreateDbContext();
        using var dbHolidays = _dbContextFactory.CreateDbContext();
        using var dbTransports = _dbContextFactory.CreateDbContext();
        using var dbOthers = _dbContextFactory.CreateDbContext();

        var flightsQuery = dbFlights.FlightBookings.AsNoTracking().Include(x => x.Head).Include(x => x.Passengers).Where(x => x.UserId == salesmanId);
        var hotelsQuery = dbHotels.HotelBookings.AsNoTracking().Include(x => x.Head).Include(x => x.Guests).Where(x => x.UserId == salesmanId);
        var visasQuery = dbVisas.SalesVisaApplications.AsNoTracking().Include(x => x.Head).Include(x => x.Applicants).Where(x => x.UserId == salesmanId);
        var vfsQuery = dbVfs.VfsAppointments.AsNoTracking().Include(x => x.Head).Include(x => x.Applicants).Where(x => x.CreatedBy == salesmanId && x.DeletedAt == null);
        var holidayQuery = dbHolidays.HolidayPackages.AsNoTracking().Include(x => x.Head).Include(x => x.Passengers).Where(x => x.CreatedBy == salesmanId && x.DeletedAt == null);
        var transportQuery = dbTransports.Transportations.AsNoTracking().Include(x => x.Head).Include(x => x.Passengers).Where(x => x.CreatedBy == salesmanId && x.DeletedAt == null);
        var othersQuery = dbOthers.OtherServices.AsNoTracking().Include(x => x.Head).Include(x => x.Applicants).Where(x => x.CreatedBy == salesmanId && x.DeletedAt == null);

        if (startDate.HasValue)
        {
            var s = startDate.Value;
            flightsQuery = flightsQuery.Where(x => x.CreatedAt >= s);
            hotelsQuery = hotelsQuery.Where(x => x.CreatedAt >= s);
            visasQuery = visasQuery.Where(x => x.CreatedAt >= s);
            vfsQuery = vfsQuery.Where(x => x.CreatedAt >= s);
            holidayQuery = holidayQuery.Where(x => x.CreatedAt >= s);
            transportQuery = transportQuery.Where(x => x.CreatedAt >= s);
            othersQuery = othersQuery.Where(x => x.CreatedAt >= s);
        }

        if (endDate.HasValue)
        {
            var e = endDate.Value;
            flightsQuery = flightsQuery.Where(x => x.CreatedAt <= e);
            hotelsQuery = hotelsQuery.Where(x => x.CreatedAt <= e);
            visasQuery = visasQuery.Where(x => x.CreatedAt <= e);
            vfsQuery = vfsQuery.Where(x => x.CreatedAt <= e);
            holidayQuery = holidayQuery.Where(x => x.CreatedAt <= e);
            transportQuery = transportQuery.Where(x => x.CreatedAt <= e);
            othersQuery = othersQuery.Where(x => x.CreatedAt <= e);
        }

        var flightsTask = flightsQuery.ToListAsync(ct);
        var hotelsTask = hotelsQuery.ToListAsync(ct);
        var visasTask = visasQuery.ToListAsync(ct);
        var vfsTask = vfsQuery.ToListAsync(ct);
        var holidayTask = holidayQuery.ToListAsync(ct);
        var transportTask = transportQuery.ToListAsync(ct);
        var othersTask = othersQuery.ToListAsync(ct);

        await Task.WhenAll(flightsTask, hotelsTask, visasTask, vfsTask, holidayTask, transportTask, othersTask);

        foreach (var f in flightsTask.Result)
        {
            var passNames = f.Passengers?.Select(p => p.PassengerName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var passDisplay = passNames.Count switch
            {
                0 => "-",
                1 => passNames[0],
                2 => passNames[0] + ", " + passNames[1],
                _ => passNames[0] + " (+" + (passNames.Count - 1) + " more)"
            };
            var route = f.FromLocation + " -> " + f.ToLocation;
            if (passNames.Count > 1) route += " (" + passNames.Count + " pax)";

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = f.Id,
                ServiceType = "Flight",
                DocumentNumber = f.DocumentNumber,
                CustomerHead = f.Head != null ? (f.Head.FirstName + " " + f.Head.LastName).Trim() : f.Agent,
                BeneficiaryName = passDisplay,
                Details = route,
                Amount = f.PayableAmount,
                PaymentMethod = f.PaymentMethod,
                PaymentStatus = f.PaymentStatus,
                ProcessingStatus = f.ProcessingStatus,
                CreatedAt = f.CreatedAt ?? DateTime.UtcNow
            });
        }

        foreach (var h in hotelsTask.Result)
        {
            var guestNames = h.Guests?.Select(g => g.GuestName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var guestDisplay = !string.IsNullOrWhiteSpace(h.GuestName) ? h.GuestName : (guestNames.Count > 0 ? string.Join(", ", guestNames) : "-");
            var details = (h.HotelName ?? h.Country) + " (" + h.Days + " days)";

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = h.Id,
                ServiceType = "Hotel",
                DocumentNumber = h.DocumentNumber,
                CustomerHead = h.Head != null ? (h.Head.FirstName + " " + h.Head.LastName).Trim() : h.Agent,
                BeneficiaryName = guestDisplay,
                Details = details,
                Amount = h.PayableAmount,
                PaymentMethod = h.PaymentMethod,
                PaymentStatus = h.PaymentStatus,
                ProcessingStatus = h.ProcessingStatus,
                CreatedAt = h.CreatedAt ?? DateTime.UtcNow
            });
        }

        foreach (var v in visasTask.Result)
        {
            var appNames = v.Applicants?.Select(a => (a.FName + " " + a.LName).Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var appDisplay = appNames.Count switch
            {
                0 => "-",
                1 => appNames[0],
                2 => appNames[0] + ", " + appNames[1],
                _ => appNames[0] + " (+" + (appNames.Count - 1) + " more)"
            };
            var details = v.VisaPackName ?? "Visa Application";
            if (appNames.Count > 1) details += " (" + appNames.Count + " applicants)";

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = v.Id,
                ServiceType = "Visa",
                DocumentNumber = v.DocumentNumber,
                CustomerHead = v.Head != null ? (v.Head.FirstName + " " + v.Head.LastName).Trim() : v.Agent,
                BeneficiaryName = appDisplay,
                Details = details,
                Amount = v.PayableAmount,
                PaymentMethod = v.Applicants?.FirstOrDefault()?.PaymentMethod ?? "-",
                PaymentStatus = v.PaymentStatus,
                ProcessingStatus = v.ProcessingStatus,
                CreatedAt = v.CreatedAt ?? DateTime.UtcNow
            });
        }

        foreach (var vfs in vfsTask.Result)
        {
            var appNames = vfs.Applicants?.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var appDisplay = !string.IsNullOrWhiteSpace(vfs.Name) ? vfs.Name : (appNames.Count > 0 ? string.Join(", ", appNames) : "-");
            var details = vfs.Country + " - " + vfs.VisaType;

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = vfs.Id,
                ServiceType = "VFS",
                DocumentNumber = vfs.DocumentNumber,
                CustomerHead = vfs.Head != null ? (vfs.Head.FirstName + " " + vfs.Head.LastName).Trim() : vfs.Agent,
                BeneficiaryName = appDisplay,
                Details = details,
                Amount = vfs.PayableAmount,
                PaymentMethod = vfs.PaymentMode,
                PaymentStatus = vfs.PaymentStatus,
                ProcessingStatus = vfs.ProcessingStatus,
                CreatedAt = vfs.CreatedAt ?? DateTime.UtcNow
            });
        }

        foreach (var hol in holidayTask.Result)
        {
            var passNames = hol.Passengers?.Select(p => p.PassengerName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var passDisplay = !string.IsNullOrWhiteSpace(hol.PassengerName) ? hol.PassengerName : (passNames.Count > 0 ? string.Join(", ", passNames) : "-");
            var details = hol.Country + " (" + hol.Days + " days)";

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = hol.Id,
                ServiceType = "Holiday",
                DocumentNumber = hol.DocumentNumber,
                CustomerHead = hol.Head != null ? (hol.Head.FirstName + " " + hol.Head.LastName).Trim() : hol.Agent,
                BeneficiaryName = passDisplay,
                Details = details,
                Amount = hol.PayableAmount,
                PaymentMethod = hol.PaymentMode,
                PaymentStatus = hol.PaymentStatus,
                ProcessingStatus = hol.ProcessingStatus,
                CreatedAt = hol.CreatedAt ?? DateTime.UtcNow
            });
        }

        foreach (var tr in transportTask.Result)
        {
            var passNames = tr.Passengers?.Select(p => p.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var passDisplay = !string.IsNullOrWhiteSpace(tr.Name) ? tr.Name : (passNames.Count > 0 ? string.Join(", ", passNames) : "-");
            var details = tr.Pickup + " -> " + tr.Drop;

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = tr.Id,
                ServiceType = "Transport",
                DocumentNumber = tr.DocumentNumber,
                CustomerHead = tr.Head != null ? (tr.Head.FirstName + " " + tr.Head.LastName).Trim() : tr.Agent,
                BeneficiaryName = passDisplay,
                Details = details,
                Amount = tr.PayableAmount,
                PaymentMethod = tr.PaymentMethod,
                PaymentStatus = tr.PaymentStatus,
                ProcessingStatus = tr.ProcessingStatus,
                CreatedAt = tr.CreatedAt ?? DateTime.UtcNow
            });
        }

        foreach (var oth in othersTask.Result)
        {
            var appNames = oth.Applicants?.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            var appDisplay = !string.IsNullOrWhiteSpace(oth.Name) ? oth.Name : (appNames.Count > 0 ? string.Join(", ", appNames) : "-");
            var details = oth.ServiceName ?? "Other Service";

            list.Add(new SalesmanServiceEntryResponse
            {
                Id = oth.Id,
                ServiceType = "Other",
                DocumentNumber = oth.DocumentNumber,
                CustomerHead = oth.Head != null ? (oth.Head.FirstName + " " + oth.Head.LastName).Trim() : oth.Agent,
                BeneficiaryName = appDisplay,
                Details = details,
                Amount = oth.PayableAmount,
                PaymentMethod = oth.PaymentMethod,
                PaymentStatus = oth.PaymentStatus,
                ProcessingStatus = oth.ProcessingStatus,
                CreatedAt = oth.CreatedAt ?? DateTime.UtcNow
            });
        }

        return list.OrderByDescending(x => x.CreatedAt);
    }
    public async Task<List<BookingFinancialAggregate>> GetAllBookingsFinancialsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        using var dbFlights = _dbContextFactory.CreateDbContext();
        using var dbHotels = _dbContextFactory.CreateDbContext();
        using var dbVisas = _dbContextFactory.CreateDbContext();
        using var dbVfs = _dbContextFactory.CreateDbContext();
        using var dbHolidays = _dbContextFactory.CreateDbContext();
        using var dbTransports = _dbContextFactory.CreateDbContext();
        using var dbOthers = _dbContextFactory.CreateDbContext();

        var flightsQuery = dbFlights.FlightBookings.AsNoTracking();
        var hotelsQuery = dbHotels.HotelBookings.AsNoTracking();
        var visasQuery = dbVisas.SalesVisaApplications.AsNoTracking();
        var vfsQuery = dbVfs.VfsAppointments.AsNoTracking().Where(x => x.DeletedAt == null);
        var holidayQuery = dbHolidays.HolidayPackages.AsNoTracking().Where(x => x.DeletedAt == null);
        var transportQuery = dbTransports.Transportations.AsNoTracking().Where(x => x.DeletedAt == null);
        var othersQuery = dbOthers.OtherServices.AsNoTracking().Where(x => x.DeletedAt == null);

        if (startDate.HasValue)
        {
            var s = startDate.Value;
            flightsQuery = flightsQuery.Where(x => x.CreatedAt >= s);
            hotelsQuery = hotelsQuery.Where(x => x.CreatedAt >= s);
            visasQuery = visasQuery.Where(x => x.CreatedAt >= s);
            vfsQuery = vfsQuery.Where(x => x.CreatedAt >= s);
            holidayQuery = holidayQuery.Where(x => x.CreatedAt >= s);
            transportQuery = transportQuery.Where(x => x.CreatedAt >= s);
            othersQuery = othersQuery.Where(x => x.CreatedAt >= s);
        }

        if (endDate.HasValue)
        {
            var e = endDate.Value;
            flightsQuery = flightsQuery.Where(x => x.CreatedAt <= e);
            hotelsQuery = hotelsQuery.Where(x => x.CreatedAt <= e);
            visasQuery = visasQuery.Where(x => x.CreatedAt <= e);
            vfsQuery = vfsQuery.Where(x => x.CreatedAt <= e);
            holidayQuery = holidayQuery.Where(x => x.CreatedAt <= e);
            transportQuery = transportQuery.Where(x => x.CreatedAt <= e);
            othersQuery = othersQuery.Where(x => x.CreatedAt <= e);
        }

        static decimal CalculateBookingProfit(decimal payable, decimal expectedCost, decimal storedProfit, string? processingStatus, string? paymentStatus, decimal rejectionFee = 0m)
        {
            if (string.Equals(processingStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(paymentStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(processingStatus, "voided", StringComparison.OrdinalIgnoreCase))
            {
                return 0m;
            }
            if (string.Equals(processingStatus, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                return rejectionFee;
            }
            if (storedProfit > 0m)
            {
                return storedProfit;
            }
            return Math.Max(0m, payable - expectedCost);
        }

        var flightsTask = flightsQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.UserId, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentMethod, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed })
            .ToListAsync(ct);

        var hotelsTask = hotelsQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.UserId, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentMethod, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed })
            .ToListAsync(ct);

        var visasTask = visasQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.UserId, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed, x.RejectionFee })
            .ToListAsync(ct);

        var vfsTask = vfsQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.CreatedBy, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentMode, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed })
            .ToListAsync(ct);

        var holidayTask = holidayQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.CreatedBy, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentMode, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed })
            .ToListAsync(ct);

        var transportTask = transportQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.CreatedBy, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentMethod, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed })
            .ToListAsync(ct);

        var othersTask = othersQuery
            .Select(x => new { x.Id, x.DocumentNumber, x.CreatedBy, x.ClientType, x.AgentId, x.PayableAmount, x.ExpectedSupplierCost, x.Profit, x.PaymentMethod, x.PaymentStatus, x.ProcessingStatus, x.CreatedAt, x.IsDealClosed })
            .ToListAsync(ct);

        await Task.WhenAll(flightsTask, hotelsTask, visasTask, vfsTask, holidayTask, transportTask, othersTask);

        var all = new List<BookingFinancialAggregate>();

        foreach (var f in flightsTask.Result)
        {
            var p = CalculateBookingProfit(f.PayableAmount, f.ExpectedSupplierCost, f.Profit, f.ProcessingStatus, f.PaymentStatus, 0m);
            all.Add(new BookingFinancialAggregate("Flight", f.Id, f.DocumentNumber, f.UserId, f.ClientType, f.AgentId, f.PayableAmount, f.ExpectedSupplierCost, p, f.PaymentMethod, f.PaymentStatus, f.ProcessingStatus, f.CreatedAt ?? DateTime.UtcNow, f.IsDealClosed));
        }

        foreach (var h in hotelsTask.Result)
        {
            var p = CalculateBookingProfit(h.PayableAmount, h.ExpectedSupplierCost, h.Profit, h.ProcessingStatus, h.PaymentStatus, 0m);
            all.Add(new BookingFinancialAggregate("Hotel", h.Id, h.DocumentNumber, h.UserId, h.ClientType, h.AgentId, h.PayableAmount, h.ExpectedSupplierCost, p, h.PaymentMethod, h.PaymentStatus, h.ProcessingStatus, h.CreatedAt ?? DateTime.UtcNow, h.IsDealClosed));
        }

        foreach (var v in visasTask.Result)
        {
            var p = CalculateBookingProfit(v.PayableAmount, v.ExpectedSupplierCost, v.Profit, v.ProcessingStatus, v.PaymentStatus, v.RejectionFee);
            all.Add(new BookingFinancialAggregate("Visa", v.Id, v.DocumentNumber, v.UserId, v.ClientType, v.AgentId, v.PayableAmount, v.ExpectedSupplierCost, p, null, v.PaymentStatus, v.ProcessingStatus, v.CreatedAt ?? DateTime.UtcNow, v.IsDealClosed));
        }

        foreach (var vf in vfsTask.Result)
        {
            var p = CalculateBookingProfit(vf.PayableAmount, vf.ExpectedSupplierCost, vf.Profit, vf.ProcessingStatus, vf.PaymentStatus, 0m);
            all.Add(new BookingFinancialAggregate("VFS", vf.Id, vf.DocumentNumber, vf.CreatedBy, vf.ClientType, vf.AgentId, vf.PayableAmount, vf.ExpectedSupplierCost, p, vf.PaymentMode, vf.PaymentStatus, vf.ProcessingStatus, vf.CreatedAt ?? DateTime.UtcNow, vf.IsDealClosed));
        }

        foreach (var hl in holidayTask.Result)
        {
            var p = CalculateBookingProfit(hl.PayableAmount, hl.ExpectedSupplierCost, hl.Profit, hl.ProcessingStatus, hl.PaymentStatus, 0m);
            all.Add(new BookingFinancialAggregate("Holiday", hl.Id, hl.DocumentNumber, hl.CreatedBy, hl.ClientType, hl.AgentId, hl.PayableAmount, hl.ExpectedSupplierCost, p, hl.PaymentMode, hl.PaymentStatus, hl.ProcessingStatus, hl.CreatedAt ?? DateTime.UtcNow, hl.IsDealClosed));
        }

        foreach (var tr in transportTask.Result)
        {
            var p = CalculateBookingProfit(tr.PayableAmount, tr.ExpectedSupplierCost, tr.Profit, tr.ProcessingStatus, tr.PaymentStatus, 0m);
            all.Add(new BookingFinancialAggregate("Transport", tr.Id, tr.DocumentNumber, tr.CreatedBy, tr.ClientType, tr.AgentId, tr.PayableAmount, tr.ExpectedSupplierCost, p, tr.PaymentMethod, tr.PaymentStatus, tr.ProcessingStatus, tr.CreatedAt ?? DateTime.UtcNow, tr.IsDealClosed));
        }

        foreach (var ot in othersTask.Result)
        {
            var p = CalculateBookingProfit(ot.PayableAmount, ot.ExpectedSupplierCost, ot.Profit, ot.ProcessingStatus, ot.PaymentStatus, 0m);
            all.Add(new BookingFinancialAggregate("Other", ot.Id, ot.DocumentNumber, ot.CreatedBy, ot.ClientType, ot.AgentId, ot.PayableAmount, ot.ExpectedSupplierCost, p, ot.PaymentMethod, ot.PaymentStatus, ot.ProcessingStatus, ot.CreatedAt ?? DateTime.UtcNow, ot.IsDealClosed));
        }

        return all;
    }

    public async Task<TexasPortal.Contracts.Sales.Performance.OperationalBookingFinancialsSummaryResponse> GetBookingFinancialsSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? dealStatus = null,
        long? salesmanId = null,
        CancellationToken ct = default)
    {
        var allBookings = await GetAllBookingsFinancialsAsync(startDate, endDate, ct);

        if (salesmanId.HasValue)
        {
            using var dbSalesmen = _dbContextFactory.CreateDbContext();
            var salesmen = await dbSalesmen.Users
                .AsNoTracking()
                .Where(x => x.IsActive && (x.UserType == null || x.UserType != "Guest"))
                .ToListAsync(ct);
            var activeSalesmanIds = new HashSet<long>(salesmen.Select(x => x.Id));

            var agentSalesmanMap = new System.Collections.Concurrent.ConcurrentDictionary<long, long>();
            if (salesmen.Any())
            {
                var agentTasks = salesmen.Select(async s =>
                {
                    try
                    {
                        var agents = await _agentsApiClient.GetAgentsAsync(s.Id, null, 10000, ct);
                        if (agents != null)
                        {
                            foreach (var a in agents)
                            {
                                agentSalesmanMap[a.Id] = s.Id;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore individual agent fetch failures in mapping
                    }
                });
                await Task.WhenAll(agentTasks);
            }

            long ResolveSalesmanId(BookingFinancialAggregate b)
            {
                if (activeSalesmanIds.Contains(b.CreatorId))
                    return b.CreatorId;

                if (string.Equals(b.ClientType, "agent", StringComparison.OrdinalIgnoreCase) && b.AgentId.HasValue && agentSalesmanMap.TryGetValue(b.AgentId.Value, out var aSalesmanId) && activeSalesmanIds.Contains(aSalesmanId))
                {
                    return aSalesmanId;
                }

                return 0; // Unassigned
            }

            allBookings = allBookings.Where(b => ResolveSalesmanId(b) == salesmanId.Value).ToList();
        }

        var isClosedOnly = string.Equals(dealStatus, "closed_only", StringComparison.OrdinalIgnoreCase);
        var isOpenOnly = string.Equals(dealStatus, "open_only", StringComparison.OrdinalIgnoreCase);

        if (isClosedOnly)
        {
            allBookings = allBookings.Where(b => b.IsDealClosed).ToList();
        }
        else if (isOpenOnly)
        {
            allBookings = allBookings.Where(b => !b.IsDealClosed).ToList();
        }

        using var dbProfit = _dbContextFactory.CreateDbContext();
        var profitQuery = dbProfit.SalesmanProfitRecords.AsNoTracking().Where(x => x.IsFinal);
        if (startDate.HasValue) profitQuery = profitQuery.Where(x => x.BookingCreatedAt >= startDate.Value);
        if (endDate.HasValue) profitQuery = profitQuery.Where(x => x.BookingCreatedAt <= endDate.Value);
        if (salesmanId.HasValue) profitQuery = profitQuery.Where(x => x.SalesmanId == salesmanId.Value);
        var finalProfitRecords = await profitQuery
            .Select(x => new {
                x.SalesmanId,
                x.BookingType,
                x.BookingId,
                x.DocumentNumber,
                x.ProfitAmount,
                x.FinalSupplierCost,
                x.AccountsApprovedTotal,
                x.PayableAmount
            })
            .ToListAsync(ct);

        var serviceDefinitions = new (string Type, string Label)[]
        {
            ("Flight", "Flights"),
            ("Hotel", "Hotels"),
            ("Visa", "Visas"),
            ("Holiday", "Holiday Packages"),
            ("VFS", "VFS Appointments"),
            ("Transport", "Transportation"),
            ("Other", "Other Services")
        };

        var profitById = new Dictionary<string, (decimal Profit, decimal Cost, decimal Sales)>(StringComparer.OrdinalIgnoreCase);
        var profitByDoc = new Dictionary<string, (decimal Profit, decimal Cost, decimal Sales)>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in finalProfitRecords)
        {
            var approvedCost = r.AccountsApprovedTotal ?? r.FinalSupplierCost;
            if (r.BookingId > 0)
            {
                profitById[$"{r.BookingType}:{r.BookingId}"] = (r.ProfitAmount, approvedCost, r.PayableAmount);
            }
            if (!string.IsNullOrWhiteSpace(r.DocumentNumber))
            {
                profitByDoc[$"{r.BookingType}:{r.DocumentNumber.Trim()}"] = (r.ProfitAmount, approvedCost, r.PayableAmount);
            }
        }

        var serviceBreakdown = new List<TexasPortal.Contracts.Sales.Performance.ServiceBookingFinancialItem>();

        foreach (var def in serviceDefinitions)
        {
            var items = allBookings.Where(x => x.ServiceType == def.Type).ToList();
            var sales = items.Sum(x => x.PayableAmount);
            
            decimal cost = 0m;
            decimal finalProfit = 0m;
            decimal grossProfit = 0m;
            var matchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var idKey = $"{item.ServiceType}:{item.BookingId}";
                var docKey = !string.IsNullOrWhiteSpace(item.DocumentNumber) ? $"{item.ServiceType}:{item.DocumentNumber.Trim()}" : null;

                (decimal Profit, decimal Cost, decimal Sales)? match = null;
                if (profitById.TryGetValue(idKey, out var byId))
                {
                    match = byId;
                    matchedKeys.Add(idKey);
                    if (docKey != null) matchedKeys.Add(docKey);
                }
                else if (docKey != null && profitByDoc.TryGetValue(docKey, out var byDoc))
                {
                    match = byDoc;
                    matchedKeys.Add(idKey);
                    matchedKeys.Add(docKey);
                }

                if (isOpenOnly)
                {
                    cost += item.ExpectedSupplierCost;
                    grossProfit += item.ExpectedProfit;
                    finalProfit += 0m;
                }
                else if (isClosedOnly)
                {
                    if (match.HasValue)
                    {
                        cost += match.Value.Cost;
                        finalProfit += match.Value.Profit;
                        grossProfit += match.Value.Profit;
                    }
                    else
                    {
                        cost += item.ExpectedSupplierCost;
                        finalProfit += item.ExpectedProfit;
                        grossProfit += item.ExpectedProfit;
                    }
                }
                else
                {
                    // "all" deal status
                    if (match.HasValue)
                    {
                        cost += match.Value.Cost;
                        finalProfit += match.Value.Profit;
                        grossProfit += match.Value.Profit;
                    }
                    else
                    {
                        cost += item.ExpectedSupplierCost;
                        grossProfit += item.ExpectedProfit;
                        if (item.IsDealClosed)
                        {
                            finalProfit += item.ExpectedProfit;
                        }
                    }
                }
            }

            // Include any finalized profit records for this service type that were not in items (e.g. historical or unmapped entries)
            if (!isOpenOnly)
            {
                foreach (var r in finalProfitRecords.Where(x => string.Equals(x.BookingType, def.Type, StringComparison.OrdinalIgnoreCase)))
                {
                    var idKey = $"{r.BookingType}:{r.BookingId}";
                    var docKey = !string.IsNullOrWhiteSpace(r.DocumentNumber) ? $"{r.BookingType}:{r.DocumentNumber.Trim()}" : null;

                    bool alreadyCounted = matchedKeys.Contains(idKey) || (docKey != null && matchedKeys.Contains(docKey));
                    if (!alreadyCounted)
                    {
                        var approvedCost = r.AccountsApprovedTotal ?? r.FinalSupplierCost;
                        cost += approvedCost;
                        finalProfit += r.ProfitAmount;
                        grossProfit += r.ProfitAmount;
                        sales += r.PayableAmount;
                    }
                }
            }

            serviceBreakdown.Add(new TexasPortal.Contracts.Sales.Performance.ServiceBookingFinancialItem
            {
                ServiceType = def.Type,
                ServiceLabel = def.Label,
                BookingCount = items.Count,
                SalesAmount = sales,
                CostAmount = cost,
                ProfitAmount = grossProfit,
                FinalProfitAmount = finalProfit
            });
        }

        var totalBookings = allBookings.Count;
        var totalSales = serviceBreakdown.Sum(x => x.SalesAmount);
        var totalCost = serviceBreakdown.Sum(x => x.CostAmount);
        var totalProfit = serviceBreakdown.Sum(x => x.ProfitAmount);
        var totalFinalProfit = serviceBreakdown.Sum(x => x.FinalProfitAmount);
        var totalEstimatedProfit = Math.Max(0m, totalProfit - totalFinalProfit);

        return new TexasPortal.Contracts.Sales.Performance.OperationalBookingFinancialsSummaryResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            DealStatus = dealStatus ?? "all",
            SalesmanId = salesmanId,
            TotalBookingsCount = totalBookings,
            TotalSalesValue = totalSales,
            TotalSupplierCost = totalCost,
            TotalGrossProfit = totalProfit,
            TotalFinalProfit = totalFinalProfit,
            TotalEstimatedProfit = totalEstimatedProfit,
            ServiceBreakdown = serviceBreakdown
        };
    }

    public async Task<IEnumerable<SalesmanReportResponse>> GetSalesmenReportAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        using var dbSalesmen = _dbContextFactory.CreateDbContext();
        var salesmen = await dbSalesmen.Users
            .AsNoTracking()
            .Where(x => x.IsActive && (x.UserType == null || x.UserType != "Guest"))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        if (!salesmen.Any())
        {
            return Array.Empty<SalesmanReportResponse>();
        }

        var allBookings = await GetAllBookingsFinancialsAsync(startDate, endDate, ct);

        // Structured logging for financial anomalies (non-zero sales and costs but zero expected profit)
        if (_logger != null)
        {
            foreach (var b in allBookings)
            {
                if (b.PayableAmount > 0m && b.ExpectedSupplierCost > 0m && b.ExpectedProfit == 0m &&
                    !string.Equals(b.ProcessingStatus, "cancelled", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(b.ProcessingStatus, "rejected", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(b.ProcessingStatus, "voided", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Booking financial anomaly: Service {Service} #{BookingId} ({DocNo}) has Payable={Payable}, Cost={Cost}, but Profit=0",
                        b.ServiceType, b.BookingId, b.DocumentNumber, b.PayableAmount, b.ExpectedSupplierCost);
                }
            }
        }

        using var dbLeads = _dbContextFactory.CreateDbContext();
        var leadsQuery = dbLeads.Leads.AsNoTracking().Where(x => x.DeletedAt == null);
        if (startDate.HasValue) leadsQuery = leadsQuery.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) leadsQuery = leadsQuery.Where(x => x.CreatedAt <= endDate.Value);
        var leadsCounts = await leadsQuery
            .GroupBy(x => x.CreatedBy)
            .Select(g => new { CreatorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CreatorId, x => x.Count, ct);

        using var dbHeads = _dbContextFactory.CreateDbContext();
        var customerNegativeBalances = await dbHeads.Heads.AsNoTracking()
            .Where(x => x.DeletedAt == null && x.CreatedBy != null && x.Wallet < 0)
            .GroupBy(x => x.CreatedBy!.Value)
            .Select(g => new { SalesmanId = g.Key, Amount = g.Sum(x => -x.Wallet) })
            .ToDictionaryAsync(x => x.SalesmanId, x => x.Amount, ct);

        var agentSalesmanMap = new System.Collections.Concurrent.ConcurrentDictionary<long, long>();
        var agentTasks = salesmen.Select(async s =>
        {
            try
            {
                var agents = await _agentsApiClient.GetAgentsAsync(s.Id, null, 10000, ct);
                if (agents != null)
                {
                    foreach (var a in agents)
                    {
                        agentSalesmanMap[a.Id] = s.Id;
                    }
                }
                var sum = agents != null ? agents.Where(a => a.Wallet < 0).Sum(a => -a.Wallet) : 0m;
                return new { SalesmanId = s.Id, Amount = sum };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load agent receivables for salesman {SalesmanId}", s.Id);
                throw;
            }
        });
        var agentResults = await Task.WhenAll(agentTasks);
        var agentNegativeBalances = agentResults.GroupBy(x => x.SalesmanId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var activeSalesmanIds = new HashSet<long>(salesmen.Select(x => x.Id));
        long ResolveSalesmanId(BookingFinancialAggregate b)
        {
            if (activeSalesmanIds.Contains(b.CreatorId))
                return b.CreatorId;

            if (string.Equals(b.ClientType, "agent", StringComparison.OrdinalIgnoreCase) && b.AgentId.HasValue && agentSalesmanMap.TryGetValue(b.AgentId.Value, out var aSalesmanId) && activeSalesmanIds.Contains(aSalesmanId))
            {
                return aSalesmanId;
            }

            return 0; // Unassigned
        }

        var bookingsBySalesman = allBookings.GroupBy(ResolveSalesmanId).ToDictionary(g => g.Key, g => g.ToList());

        using var dbProfit = _dbContextFactory.CreateDbContext();
        var profitQuery = dbProfit.SalesmanProfitRecords.AsNoTracking().Where(x => x.IsFinal);
        if (startDate.HasValue) profitQuery = profitQuery.Where(x => x.BookingCreatedAt >= startDate.Value);
        if (endDate.HasValue) profitQuery = profitQuery.Where(x => x.BookingCreatedAt <= endDate.Value);
        var finalProfitRecords = await profitQuery
            .Select(x => new { x.SalesmanId, x.BookingType, x.BookingId, x.DocumentNumber, x.ProfitAmount })
            .ToListAsync(ct);

        var profitById = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var profitByDoc = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in finalProfitRecords)
        {
            if (r.BookingId > 0)
                profitById[$"{r.BookingType}:{r.BookingId}"] = r.ProfitAmount;
            if (!string.IsNullOrWhiteSpace(r.DocumentNumber))
                profitByDoc[$"{r.BookingType}:{r.DocumentNumber.Trim()}"] = r.ProfitAmount;
        }

        static decimal ComputeServiceFinalProfit(
            string serviceType,
            List<BookingFinancialAggregate> items,
            Dictionary<string, decimal> pById,
            Dictionary<string, decimal> pByDoc,
            IEnumerable<(string BookingType, long BookingId, string? DocumentNumber, decimal ProfitAmount)> sFinalRecords)
        {
            decimal finalProfit = 0m;
            var matchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var idKey = $"{item.ServiceType}:{item.BookingId}";
                var docKey = !string.IsNullOrWhiteSpace(item.DocumentNumber) ? $"{item.ServiceType}:{item.DocumentNumber.Trim()}" : null;

                if (pById.TryGetValue(idKey, out var valById))
                {
                    finalProfit += valById;
                    matchedKeys.Add(idKey);
                    if (docKey != null) matchedKeys.Add(docKey);
                }
                else if (docKey != null && pByDoc.TryGetValue(docKey, out var valByDoc))
                {
                    finalProfit += valByDoc;
                    matchedKeys.Add(idKey);
                    matchedKeys.Add(docKey);
                }
                else if (item.IsDealClosed)
                {
                    finalProfit += item.ExpectedProfit;
                }
            }

            foreach (var r in sFinalRecords.Where(x => string.Equals(x.BookingType, serviceType, StringComparison.OrdinalIgnoreCase)))
            {
                var idKey = $"{r.BookingType}:{r.BookingId}";
                var docKey = !string.IsNullOrWhiteSpace(r.DocumentNumber) ? $"{r.BookingType}:{r.DocumentNumber.Trim()}" : null;

                bool alreadyCounted = matchedKeys.Contains(idKey) || (docKey != null && matchedKeys.Contains(docKey));
                if (!alreadyCounted)
                {
                    finalProfit += r.ProfitAmount;
                }
            }

            return finalProfit;
        }

        var report = new List<SalesmanReportResponse>();

        foreach (var salesman in salesmen)
        {
            var sid = salesman.Id;
            var leadsCount = leadsCounts.GetValueOrDefault(sid, 0);

            var sBookings = bookingsBySalesman.GetValueOrDefault(sid, new List<BookingFinancialAggregate>());

            var flightItems = sBookings.Where(x => x.ServiceType == "Flight").ToList();
            var hotelItems = sBookings.Where(x => x.ServiceType == "Hotel").ToList();
            var visaItems = sBookings.Where(x => x.ServiceType == "Visa").ToList();
            var vfsItems = sBookings.Where(x => x.ServiceType == "VFS").ToList();
            var holidayItems = sBookings.Where(x => x.ServiceType == "Holiday").ToList();
            var transportItems = sBookings.Where(x => x.ServiceType == "Transport").ToList();
            var otherItems = sBookings.Where(x => x.ServiceType == "Other").ToList();

            var totalSalesValue = sBookings.Sum(x => x.PayableAmount);
            var totalExpectedProfit = sBookings.Sum(x => x.ExpectedProfit);

            var sFinalRecords = finalProfitRecords.Where(x => x.SalesmanId == sid).Select(x => (x.BookingType, x.BookingId, x.DocumentNumber, x.ProfitAmount)).ToList();

            var flightFinalProfit = ComputeServiceFinalProfit("Flight", flightItems, profitById, profitByDoc, sFinalRecords);
            var hotelFinalProfit = ComputeServiceFinalProfit("Hotel", hotelItems, profitById, profitByDoc, sFinalRecords);
            var visaFinalProfit = ComputeServiceFinalProfit("Visa", visaItems, profitById, profitByDoc, sFinalRecords);
            var vfsFinalProfit = ComputeServiceFinalProfit("VFS", vfsItems, profitById, profitByDoc, sFinalRecords);
            var holidayFinalProfit = ComputeServiceFinalProfit("Holiday", holidayItems, profitById, profitByDoc, sFinalRecords);
            var transportFinalProfit = ComputeServiceFinalProfit("Transport", transportItems, profitById, profitByDoc, sFinalRecords);
            var otherFinalProfit = ComputeServiceFinalProfit("Other", otherItems, profitById, profitByDoc, sFinalRecords);

            var totalFinalProfit = flightFinalProfit + hotelFinalProfit + visaFinalProfit + vfsFinalProfit + holidayFinalProfit + transportFinalProfit + otherFinalProfit;

            var customerCredit = customerNegativeBalances.GetValueOrDefault(sid, 0m);
            var agentCredit = agentNegativeBalances.GetValueOrDefault(sid, 0m);
            var creditToCollect = customerCredit + agentCredit;

            report.Add(new SalesmanReportResponse
            {
                Id = salesman.Id,
                Name = salesman.Name,
                Email = salesman.Email,
                LeadsCount = leadsCount,
                BookingsCount = sBookings.Count,
                TotalSalesValue = totalSalesValue,
                TotalProfit = totalExpectedProfit,
                TotalFinalProfit = totalFinalProfit,
                CreditToCollect = creditToCollect,
                FlightCount = flightItems.Count,
                FlightSales = flightItems.Sum(x => x.PayableAmount),
                FlightProfit = flightItems.Sum(x => x.ExpectedProfit),
                FlightFinalProfit = flightFinalProfit,
                HotelCount = hotelItems.Count,
                HotelSales = hotelItems.Sum(x => x.PayableAmount),
                HotelProfit = hotelItems.Sum(x => x.ExpectedProfit),
                HotelFinalProfit = hotelFinalProfit,
                VisaCount = visaItems.Count,
                VisaSales = visaItems.Sum(x => x.PayableAmount),
                VisaProfit = visaItems.Sum(x => x.ExpectedProfit),
                VisaFinalProfit = visaFinalProfit,
                VfsCount = vfsItems.Count,
                VfsSales = vfsItems.Sum(x => x.PayableAmount),
                VfsProfit = vfsItems.Sum(x => x.ExpectedProfit),
                VfsFinalProfit = vfsFinalProfit,
                HolidayCount = holidayItems.Count,
                HolidaySales = holidayItems.Sum(x => x.PayableAmount),
                HolidayProfit = holidayItems.Sum(x => x.ExpectedProfit),
                HolidayFinalProfit = holidayFinalProfit,
                TransportCount = transportItems.Count,
                TransportSales = transportItems.Sum(x => x.PayableAmount),
                TransportProfit = transportItems.Sum(x => x.ExpectedProfit),
                TransportFinalProfit = transportFinalProfit,
                OtherCount = otherItems.Count,
                OtherSales = otherItems.Sum(x => x.PayableAmount),
                OtherProfit = otherItems.Sum(x => x.ExpectedProfit),
                OtherFinalProfit = otherFinalProfit
            });
        }

        // Include Unassigned row if any bookings are not matched to active salesmen
        if (bookingsBySalesman.TryGetValue(0, out var unassignedBookings) && unassignedBookings.Count > 0)
        {
            var uFinalRecords = finalProfitRecords.Where(x => x.SalesmanId == 0).Select(x => (x.BookingType, x.BookingId, x.DocumentNumber, x.ProfitAmount)).ToList();

            var uFlightItems = unassignedBookings.Where(x => x.ServiceType == "Flight").ToList();
            var uHotelItems = unassignedBookings.Where(x => x.ServiceType == "Hotel").ToList();
            var uVisaItems = unassignedBookings.Where(x => x.ServiceType == "Visa").ToList();
            var uVfsItems = unassignedBookings.Where(x => x.ServiceType == "VFS").ToList();
            var uHolidayItems = unassignedBookings.Where(x => x.ServiceType == "Holiday").ToList();
            var uTransportItems = unassignedBookings.Where(x => x.ServiceType == "Transport").ToList();
            var uOtherItems = unassignedBookings.Where(x => x.ServiceType == "Other").ToList();

            var uFlightFinalProfit = ComputeServiceFinalProfit("Flight", uFlightItems, profitById, profitByDoc, uFinalRecords);
            var uHotelFinalProfit = ComputeServiceFinalProfit("Hotel", uHotelItems, profitById, profitByDoc, uFinalRecords);
            var uVisaFinalProfit = ComputeServiceFinalProfit("Visa", uVisaItems, profitById, profitByDoc, uFinalRecords);
            var uVfsFinalProfit = ComputeServiceFinalProfit("VFS", uVfsItems, profitById, profitByDoc, uFinalRecords);
            var uHolidayFinalProfit = ComputeServiceFinalProfit("Holiday", uHolidayItems, profitById, profitByDoc, uFinalRecords);
            var uTransportFinalProfit = ComputeServiceFinalProfit("Transport", uTransportItems, profitById, profitByDoc, uFinalRecords);
            var uOtherFinalProfit = ComputeServiceFinalProfit("Other", uOtherItems, profitById, profitByDoc, uFinalRecords);

            var uTotalFinalProfit = uFlightFinalProfit + uHotelFinalProfit + uVisaFinalProfit + uVfsFinalProfit + uHolidayFinalProfit + uTransportFinalProfit + uOtherFinalProfit;

            report.Add(new SalesmanReportResponse
            {
                Id = 0,
                Name = "Unassigned",
                Email = "unassigned@system",
                LeadsCount = 0,
                BookingsCount = unassignedBookings.Count,
                TotalSalesValue = unassignedBookings.Sum(x => x.PayableAmount),
                TotalProfit = unassignedBookings.Sum(x => x.ExpectedProfit),
                TotalFinalProfit = uTotalFinalProfit,
                CreditToCollect = 0m,
                FlightCount = uFlightItems.Count,
                FlightSales = uFlightItems.Sum(x => x.PayableAmount),
                FlightProfit = uFlightItems.Sum(x => x.ExpectedProfit),
                FlightFinalProfit = uFlightFinalProfit,
                HotelCount = uHotelItems.Count,
                HotelSales = uHotelItems.Sum(x => x.PayableAmount),
                HotelProfit = uHotelItems.Sum(x => x.ExpectedProfit),
                HotelFinalProfit = uHotelFinalProfit,
                VisaCount = uVisaItems.Count,
                VisaSales = uVisaItems.Sum(x => x.PayableAmount),
                VisaProfit = uVisaItems.Sum(x => x.ExpectedProfit),
                VisaFinalProfit = uVisaFinalProfit,
                VfsCount = uVfsItems.Count,
                VfsSales = uVfsItems.Sum(x => x.PayableAmount),
                VfsProfit = uVfsItems.Sum(x => x.ExpectedProfit),
                VfsFinalProfit = uVfsFinalProfit,
                HolidayCount = uHolidayItems.Count,
                HolidaySales = uHolidayItems.Sum(x => x.PayableAmount),
                HolidayProfit = uHolidayItems.Sum(x => x.ExpectedProfit),
                HolidayFinalProfit = uHolidayFinalProfit,
                TransportCount = uTransportItems.Count,
                TransportSales = uTransportItems.Sum(x => x.PayableAmount),
                TransportProfit = uTransportItems.Sum(x => x.ExpectedProfit),
                TransportFinalProfit = uTransportFinalProfit,
                OtherCount = uOtherItems.Count,
                OtherSales = uOtherItems.Sum(x => x.PayableAmount),
                OtherProfit = uOtherItems.Sum(x => x.ExpectedProfit),
                OtherFinalProfit = uOtherFinalProfit
            });
        }

        // Reconciliation Check: Sum of salesman rows must equal the total expected profit for the same filters
        var totalAllExpectedProfit = allBookings.Sum(x => x.ExpectedProfit);
        var totalReportExpectedProfit = report.Sum(x => x.TotalProfit);
        var reconciliationDifference = Math.Abs(totalAllExpectedProfit - totalReportExpectedProfit);
        var isReconciled = reconciliationDifference < 0.001m;

        return report.Select(r => new SalesmanReportResponse
        {
            Id = r.Id,
            Name = r.Name,
            Email = r.Email,
            LeadsCount = r.LeadsCount,
            BookingsCount = r.BookingsCount,
            TotalSalesValue = r.TotalSalesValue,
            TotalProfit = r.TotalProfit,
            TotalFinalProfit = r.TotalFinalProfit,
            FlightCount = r.FlightCount,
            FlightSales = r.FlightSales,
            FlightProfit = r.FlightProfit,
            FlightFinalProfit = r.FlightFinalProfit,
            HotelCount = r.HotelCount,
            HotelSales = r.HotelSales,
            HotelProfit = r.HotelProfit,
            HotelFinalProfit = r.HotelFinalProfit,
            VisaCount = r.VisaCount,
            VisaSales = r.VisaSales,
            VisaProfit = r.VisaProfit,
            VisaFinalProfit = r.VisaFinalProfit,
            VfsCount = r.VfsCount,
            VfsSales = r.VfsSales,
            VfsProfit = r.VfsProfit,
            VfsFinalProfit = r.VfsFinalProfit,
            HolidayCount = r.HolidayCount,
            HolidaySales = r.HolidaySales,
            HolidayProfit = r.HolidayProfit,
            HolidayFinalProfit = r.HolidayFinalProfit,
            TransportCount = r.TransportCount,
            TransportSales = r.TransportSales,
            TransportProfit = r.TransportProfit,
            TransportFinalProfit = r.TransportFinalProfit,
            OtherCount = r.OtherCount,
            OtherSales = r.OtherSales,
            OtherProfit = r.OtherProfit,
            OtherFinalProfit = r.OtherFinalProfit,
            CreditToCollect = r.CreditToCollect,
            IsReconciled = isReconciled,
            ReconciliationDifference = reconciliationDifference
        }).ToList();
    }

    public async Task<PagedResponse<OpenDealEntryResponse>> GetOpenDealsAsync(
        int page, int pageSize, string? serviceType = null, string? search = null,
        long? salesmanId = null, string? paymentStatus = null,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var allDeals = await FetchOpenDealsInternalAsync(serviceType, search, salesmanId, paymentStatus, startDate, endDate, ct);

        var totalCount = allDeals.Count;
        var currentPage = page <= 0 ? 1 : page;
        var currentPageSize = pageSize <= 0 ? 20 : pageSize;

        var pagedItems = allDeals
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToList();

        return PagedResponse<OpenDealEntryResponse>.Create(pagedItems, totalCount, currentPage, currentPageSize);
    }

    public async Task<OpenDealsSummaryResponse> GetOpenDealsSummaryAsync(
        string? serviceType = null, string? search = null,
        long? salesmanId = null, string? paymentStatus = null,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var allDeals = await FetchOpenDealsInternalAsync(serviceType, search, salesmanId, paymentStatus, startDate, endDate, ct);

        var breakdown = allDeals
            .GroupBy(x => x.ServiceType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new OpenDealsSummaryResponse
        {
            TotalOpenDeals = allDeals.Count,
            TotalSalesValue = allDeals.Sum(x => x.PayableAmount),
            TotalExpectedCost = allDeals.Sum(x => x.ExpectedSupplierCost),
            TotalExpectedProfit = allDeals.Sum(x => x.ExpectedProfit),
            ServiceBreakdown = breakdown
        };
    }

    private async Task<List<OpenDealEntryResponse>> FetchOpenDealsInternalAsync(
        string? serviceType, string? search,
        long? salesmanId, string? paymentStatus,
        DateTime? startDate, DateTime? endDate,
        CancellationToken ct)
    {
        using var dbSalesmen = _dbContextFactory.CreateDbContext();
        var salesmanMap = await dbSalesmen.Users.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var st = serviceType?.Trim().ToLowerInvariant();
        bool queryAll = string.IsNullOrWhiteSpace(st) || st == "all";

        var deals = new List<OpenDealEntryResponse>();
        var term = search?.Trim().ToLower();
        var pStatus = (!string.IsNullOrWhiteSpace(paymentStatus) && !string.Equals(paymentStatus, "all", StringComparison.OrdinalIgnoreCase))
            ? paymentStatus.Trim().ToLower()
            : null;

        DateTime? endOfDay = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddTicks(-1) : null;

        static decimal CalcProfit(decimal payable, decimal cost, decimal storedProfit, string? procStatus, string? payStatus, decimal rejFee = 0m)
        {
            if (string.Equals(procStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(procStatus, "voided", StringComparison.OrdinalIgnoreCase))
            {
                return 0m;
            }
            if (string.Equals(procStatus, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                return rejFee;
            }
            if (storedProfit > 0m)
            {
                return storedProfit;
            }
            return Math.Max(0m, payable - cost);
        }

        // 1. Flights
        if (queryAll || st == "flight" || st == "flights")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.FlightBookings.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Passengers)
                .Where(x => !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.UserId == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.FromLocation != null && x.FromLocation.ToLower().Contains(term)) ||
                    (x.ToLocation != null && x.ToLocation.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)) ||
                    x.Passengers.Any(p => p.PassengerName.ToLower().Contains(term)));
            }

            var list = await q.ToListAsync(ct);
            foreach (var f in list)
            {
                var passNames = f.Passengers?.Select(p => p.PassengerName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
                var details = $"{f.FromLocation} -> {f.ToLocation}";
                if (passNames.Count > 1) details += $" ({passNames.Count} pax)";
                else if (passNames.Count == 1) details += $" ({passNames[0]})";

                var headName = f.Head != null ? (f.Head.FirstName + " " + f.Head.LastName).Trim() : f.Agent;
                salesmanMap.TryGetValue(f.UserId, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = f.Id,
                    ServiceType = "Flight",
                    DocumentNumber = f.DocumentNumber,
                    CreatedAt = f.CreatedAt ?? DateTime.UtcNow,
                    HeadId = f.HeadId,
                    HeadName = headName,
                    ClientType = f.ClientType,
                    SalesmanId = f.UserId,
                    SalesmanName = smName ?? "Salesman #" + f.UserId,
                    MainDetails = details,
                    PayableAmount = f.PayableAmount,
                    ExpectedSupplierCost = f.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(f.PayableAmount, f.ExpectedSupplierCost, f.Profit, f.ProcessingStatus, f.PaymentStatus),
                    PaymentMethod = f.PaymentMethod,
                    PaymentStatus = f.PaymentStatus,
                    ProcessingStatus = f.ProcessingStatus,
                    IsDealClosed = f.IsDealClosed,
                    OperationClosed = f.OperationClosed
                });
            }
        }

        // 2. Hotels
        if (queryAll || st == "hotel" || st == "hotels")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.HotelBookings.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Guests)
                .Where(x => !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.UserId == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.GuestName != null && x.GuestName.ToLower().Contains(term)) ||
                    (x.HotelName != null && x.HotelName.ToLower().Contains(term)) ||
                    (x.Country != null && x.Country.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)) ||
                    x.Guests.Any(g => g.GuestName.ToLower().Contains(term)));
            }

            var list = await q.ToListAsync(ct);
            foreach (var h in list)
            {
                var details = $"{(h.HotelName ?? h.Country)} ({h.Days} days)";
                if (!string.IsNullOrWhiteSpace(h.GuestName)) details = $"{h.GuestName} · " + details;

                var headName = h.Head != null ? (h.Head.FirstName + " " + h.Head.LastName).Trim() : h.Agent;
                salesmanMap.TryGetValue(h.UserId, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = h.Id,
                    ServiceType = "Hotel",
                    DocumentNumber = h.DocumentNumber,
                    CreatedAt = h.CreatedAt ?? DateTime.UtcNow,
                    HeadId = h.HeadId,
                    HeadName = headName,
                    ClientType = h.ClientType,
                    SalesmanId = h.UserId,
                    SalesmanName = smName ?? "Salesman #" + h.UserId,
                    MainDetails = details,
                    PayableAmount = h.PayableAmount,
                    ExpectedSupplierCost = h.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(h.PayableAmount, h.ExpectedSupplierCost, h.Profit, h.ProcessingStatus, h.PaymentStatus),
                    PaymentMethod = h.PaymentMethod,
                    PaymentStatus = h.PaymentStatus,
                    ProcessingStatus = h.ProcessingStatus,
                    IsDealClosed = h.IsDealClosed,
                    OperationClosed = h.OperationClosed
                });
            }
        }

        // 3. Visas
        if (queryAll || st == "visa" || st == "visas")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.SalesVisaApplications.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Applicants)
                .Where(x => !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.UserId == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.VisaPackName != null && x.VisaPackName.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)) ||
                    x.Applicants.Any(a => ((a.FName ?? "") + " " + (a.LName ?? "")).ToLower().Contains(term) || (a.PassportNo != null && a.PassportNo.ToLower().Contains(term))));
            }

            var list = await q.ToListAsync(ct);
            foreach (var v in list)
            {
                var appNames = v.Applicants?.Select(a => (a.FName + " " + a.LName).Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
                var details = v.VisaPackName ?? "Visa Application";
                if (appNames.Count > 1) details += $" ({appNames.Count} applicants)";
                else if (appNames.Count == 1) details = $"{appNames[0]} · {details}";

                var headName = v.Head != null ? (v.Head.FirstName + " " + v.Head.LastName).Trim() : v.Agent;
                salesmanMap.TryGetValue(v.UserId, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = v.Id,
                    ServiceType = "Visa",
                    DocumentNumber = v.DocumentNumber,
                    CreatedAt = v.CreatedAt ?? DateTime.UtcNow,
                    HeadId = v.HeadId,
                    HeadName = headName,
                    ClientType = v.ClientType,
                    SalesmanId = v.UserId,
                    SalesmanName = smName ?? "Salesman #" + v.UserId,
                    MainDetails = details,
                    PayableAmount = v.PayableAmount,
                    ExpectedSupplierCost = v.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(v.PayableAmount, v.ExpectedSupplierCost, v.Profit, v.ProcessingStatus, v.PaymentStatus, v.RejectionFee),
                    PaymentMethod = v.Applicants?.FirstOrDefault()?.PaymentMethod ?? "-",
                    PaymentStatus = v.PaymentStatus,
                    ProcessingStatus = v.ProcessingStatus,
                    IsDealClosed = v.IsDealClosed,
                    OperationClosed = v.OperationClosed
                });
            }
        }

        // 4. VFS Appointments
        if (queryAll || st == "vfs" || st == "vfs-appointments")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.VfsAppointments.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Applicants)
                .Where(x => x.DeletedAt == null &&
                            !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.CreatedBy == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.Name != null && x.Name.ToLower().Contains(term)) ||
                    (x.Country != null && x.Country.ToLower().Contains(term)) ||
                    (x.VisaType != null && x.VisaType.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)) ||
                    x.Applicants.Any(a => a.Name.ToLower().Contains(term)));
            }

            var list = await q.ToListAsync(ct);
            foreach (var vfs in list)
            {
                var details = $"{vfs.Country} - {vfs.VisaType}";
                if (!string.IsNullOrWhiteSpace(vfs.Name)) details = $"{vfs.Name} · {details}";

                var headName = vfs.Head != null ? (vfs.Head.FirstName + " " + vfs.Head.LastName).Trim() : vfs.Agent;
                salesmanMap.TryGetValue(vfs.CreatedBy, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = vfs.Id,
                    ServiceType = "VFS",
                    DocumentNumber = vfs.DocumentNumber,
                    CreatedAt = vfs.CreatedAt ?? DateTime.UtcNow,
                    HeadId = vfs.HeadId,
                    HeadName = headName,
                    ClientType = vfs.ClientType,
                    SalesmanId = vfs.CreatedBy,
                    SalesmanName = smName ?? "Salesman #" + vfs.CreatedBy,
                    MainDetails = details,
                    PayableAmount = vfs.PayableAmount,
                    ExpectedSupplierCost = vfs.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(vfs.PayableAmount, vfs.ExpectedSupplierCost, vfs.Profit, vfs.ProcessingStatus, vfs.PaymentStatus),
                    PaymentMethod = vfs.PaymentMode,
                    PaymentStatus = vfs.PaymentStatus,
                    ProcessingStatus = vfs.ProcessingStatus,
                    IsDealClosed = vfs.IsDealClosed,
                    OperationClosed = vfs.OperationClosed
                });
            }
        }

        // 5. Holiday Packages
        if (queryAll || st == "holiday" || st == "holidays" || st == "holiday-packages")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.HolidayPackages.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Passengers)
                .Where(x => x.DeletedAt == null &&
                            !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.CreatedBy == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.PassengerName != null && x.PassengerName.ToLower().Contains(term)) ||
                    (x.Country != null && x.Country.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)) ||
                    x.Passengers.Any(p => p.PassengerName.ToLower().Contains(term)));
            }

            var list = await q.ToListAsync(ct);
            foreach (var hol in list)
            {
                var details = $"{hol.Country} ({hol.Days} days)";
                if (!string.IsNullOrWhiteSpace(hol.PassengerName)) details = $"{hol.PassengerName} · {details}";

                var headName = hol.Head != null ? (hol.Head.FirstName + " " + hol.Head.LastName).Trim() : hol.Agent;
                salesmanMap.TryGetValue(hol.CreatedBy, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = hol.Id,
                    ServiceType = "Holiday",
                    DocumentNumber = hol.DocumentNumber,
                    CreatedAt = hol.CreatedAt ?? DateTime.UtcNow,
                    HeadId = hol.HeadId,
                    HeadName = headName,
                    ClientType = hol.ClientType,
                    SalesmanId = hol.CreatedBy,
                    SalesmanName = smName ?? "Salesman #" + hol.CreatedBy,
                    MainDetails = details,
                    PayableAmount = hol.PayableAmount,
                    ExpectedSupplierCost = hol.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(hol.PayableAmount, hol.ExpectedSupplierCost, hol.Profit, hol.ProcessingStatus, hol.PaymentStatus),
                    PaymentMethod = hol.PaymentMode,
                    PaymentStatus = hol.PaymentStatus,
                    ProcessingStatus = hol.ProcessingStatus,
                    IsDealClosed = hol.IsDealClosed,
                    OperationClosed = hol.OperationClosed
                });
            }
        }

        // 6. Transportation
        if (queryAll || st == "transport" || st == "transportation")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.Transportations.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Passengers)
                .Where(x => x.DeletedAt == null &&
                            !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.CreatedBy == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.Name != null && x.Name.ToLower().Contains(term)) ||
                    (x.Pickup != null && x.Pickup.ToLower().Contains(term)) ||
                    (x.Drop != null && x.Drop.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)));
            }

            var list = await q.ToListAsync(ct);
            foreach (var tr in list)
            {
                var details = $"{tr.Pickup} -> {tr.Drop}";
                if (!string.IsNullOrWhiteSpace(tr.Name)) details = $"{tr.Name} · {details}";

                var headName = tr.Head != null ? (tr.Head.FirstName + " " + tr.Head.LastName).Trim() : tr.Agent;
                salesmanMap.TryGetValue(tr.CreatedBy, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = tr.Id,
                    ServiceType = "Transport",
                    DocumentNumber = tr.DocumentNumber,
                    CreatedAt = tr.CreatedAt ?? DateTime.UtcNow,
                    HeadId = tr.HeadId,
                    HeadName = headName,
                    ClientType = tr.ClientType,
                    SalesmanId = tr.CreatedBy,
                    SalesmanName = smName ?? "Salesman #" + tr.CreatedBy,
                    MainDetails = details,
                    PayableAmount = tr.PayableAmount,
                    ExpectedSupplierCost = tr.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(tr.PayableAmount, tr.ExpectedSupplierCost, tr.Profit, tr.ProcessingStatus, tr.PaymentStatus),
                    PaymentMethod = tr.PaymentMethod,
                    PaymentStatus = tr.PaymentStatus,
                    ProcessingStatus = tr.ProcessingStatus,
                    IsDealClosed = tr.IsDealClosed,
                    OperationClosed = tr.OperationClosed
                });
            }
        }

        // 7. Other Services
        if (queryAll || st == "other" || st == "other-services")
        {
            using var db = _dbContextFactory.CreateDbContext();
            var q = db.OtherServices.AsNoTracking()
                .Include(x => x.Head)
                .Include(x => x.Applicants)
                .Where(x => x.DeletedAt == null &&
                            !x.IsDealClosed &&
                            (x.PaymentStatus == null || x.PaymentStatus.ToLower() != "cancelled") &&
                            (x.ProcessingStatus == null || (x.ProcessingStatus.ToLower() != "cancelled" && x.ProcessingStatus.ToLower() != "voided")));

            if (salesmanId.HasValue) q = q.Where(x => x.CreatedBy == salesmanId.Value);
            if (pStatus != null) q = q.Where(x => x.PaymentStatus != null && x.PaymentStatus.ToLower() == pStatus);
            if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
            if (endOfDay.HasValue) q = q.Where(x => x.CreatedAt <= endOfDay.Value);
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(x =>
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                    (x.Name != null && x.Name.ToLower().Contains(term)) ||
                    (x.ServiceName != null && x.ServiceName.ToLower().Contains(term)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                    (x.Agent != null && x.Agent.ToLower().Contains(term)));
            }

            var list = await q.ToListAsync(ct);
            foreach (var oth in list)
            {
                var details = oth.ServiceName ?? "Other Service";
                if (!string.IsNullOrWhiteSpace(oth.Name)) details = $"{oth.Name} · {details}";

                var headName = oth.Head != null ? (oth.Head.FirstName + " " + oth.Head.LastName).Trim() : oth.Agent;
                salesmanMap.TryGetValue(oth.CreatedBy, out var smName);

                deals.Add(new OpenDealEntryResponse
                {
                    Id = oth.Id,
                    ServiceType = "Other",
                    DocumentNumber = oth.DocumentNumber,
                    CreatedAt = oth.CreatedAt ?? DateTime.UtcNow,
                    HeadId = oth.HeadId,
                    HeadName = headName,
                    ClientType = oth.ClientType,
                    SalesmanId = oth.CreatedBy,
                    SalesmanName = smName ?? "Salesman #" + oth.CreatedBy,
                    MainDetails = details,
                    PayableAmount = oth.PayableAmount,
                    ExpectedSupplierCost = oth.ExpectedSupplierCost,
                    ExpectedProfit = CalcProfit(oth.PayableAmount, oth.ExpectedSupplierCost, oth.Profit, oth.ProcessingStatus, oth.PaymentStatus),
                    PaymentMethod = oth.PaymentMethod,
                    PaymentStatus = oth.PaymentStatus,
                    ProcessingStatus = oth.ProcessingStatus,
                    IsDealClosed = oth.IsDealClosed,
                    OperationClosed = oth.OperationClosed
                });
            }
        }

        return deals.OrderByDescending(x => x.CreatedAt).ToList();
    }

    private static string NormalizeStatus(string status)
    {
        var s = status.Trim().ToLower().Replace("_", " ");
        if (s is "underreview" or "processing") return "under review";
        return s;
    }

    public async Task<PagedResponse<VisaApplicationSummaryResponse>> GetVisaApplicationsAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        using var dbCount = _dbContextFactory.CreateDbContext();
        using var dbList = _dbContextFactory.CreateDbContext();

        var qCount = dbCount.SalesVisaApplications.AsNoTracking();
        var qList = dbList.SalesVisaApplications.AsNoTracking();

        if (startDate.HasValue)
        {
            qCount = qCount.Where(x => x.CreatedAt >= startDate.Value);
            qList = qList.Where(x => x.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            qCount = qCount.Where(x => x.CreatedAt <= endOfDay);
            qList = qList.Where(x => x.CreatedAt <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            var targetStatus = NormalizeStatus(status);
            if (targetStatus == "pending")
            {
                qCount = qCount.Where(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || x.ProcessingStatus.ToLower() == "pending");
                qList = qList.Where(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || x.ProcessingStatus.ToLower() == "pending");
            }
            else if (targetStatus == "under review")
            {
                qCount = qCount.Where(x => x.ProcessingStatus.ToLower() == "under review" || x.ProcessingStatus.ToLower() == "processing" || x.ProcessingStatus.ToLower() == "underreview");
                qList = qList.Where(x => x.ProcessingStatus.ToLower() == "under review" || x.ProcessingStatus.ToLower() == "processing" || x.ProcessingStatus.ToLower() == "underreview");
            }
            else
            {
                qCount = qCount.Where(x => x.ProcessingStatus.ToLower() == targetStatus);
                qList = qList.Where(x => x.ProcessingStatus.ToLower() == targetStatus);
            }
        }

        List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>? visaCountries = null;
        var countryMap = new Dictionary<long, string>();
        try
        {
            visaCountries = await _agentsApiClient.GetVisaCountriesAsync(ct);
            if (visaCountries != null)
            {
                foreach (var c in visaCountries) countryMap[c.Id] = c.Name;
            }
        }
        catch { }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var tok = token.ToLower();
                var matchingCountryIds = visaCountries?
                    .Where(c => c.Name.Contains(tok, StringComparison.OrdinalIgnoreCase))
                    .Select(c => (int)c.Id)
                    .ToList() ?? new List<int>();

                qCount = qCount.Where(x =>
                    (matchingCountryIds.Count > 0 && matchingCountryIds.Contains(x.CountryId)) ||
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(tok)) ||
                    (x.VisaPackName != null && x.VisaPackName.ToLower().Contains(tok)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(tok)) ||
                    x.Applicants.Any(a => 
                        (a.Nationality != null && a.Nationality.ToLower().Contains(tok)) ||
                        (a.FName != null && a.FName.ToLower().Contains(tok)) || 
                        (a.LName != null && a.LName.ToLower().Contains(tok)) || 
                        (((a.FName ?? "") + " " + (a.LName ?? "")).ToLower().Contains(tok)) ||
                        (a.PassportNo != null && a.PassportNo.ToLower().Contains(tok)) || 
                        (a.VisaAppId != null && a.VisaAppId.ToLower().Contains(tok)) || 
                        (a.GovtFeeReference != null && a.GovtFeeReference.ToLower().Contains(tok)))
                );
                qList = qList.Where(x =>
                    (matchingCountryIds.Count > 0 && matchingCountryIds.Contains(x.CountryId)) ||
                    (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(tok)) ||
                    (x.VisaPackName != null && x.VisaPackName.ToLower().Contains(tok)) ||
                    (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(tok)) ||
                    x.Applicants.Any(a => 
                        (a.Nationality != null && a.Nationality.ToLower().Contains(tok)) ||
                        (a.FName != null && a.FName.ToLower().Contains(tok)) || 
                        (a.LName != null && a.LName.ToLower().Contains(tok)) || 
                        (((a.FName ?? "") + " " + (a.LName ?? "")).ToLower().Contains(tok)) ||
                        (a.PassportNo != null && a.PassportNo.ToLower().Contains(tok)) || 
                        (a.VisaAppId != null && a.VisaAppId.ToLower().Contains(tok)) || 
                        (a.GovtFeeReference != null && a.GovtFeeReference.ToLower().Contains(tok)))
                );
            }
        }

        qList = qList.OrderByDescending(x => x.CreatedAt);

        if (isDealClosed.HasValue)
        {
            qList = qList.Where(x => x.IsDealClosed == isDealClosed.Value);
            qCount = qCount.Where(x => x.IsDealClosed == isDealClosed.Value);
        }

        var totalTask = qCount.CountAsync(ct);
        var rawItemsTask = qList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.User.Name,
                x.CountryId,
                PassportNo = x.Applicants.OrderBy(a => a.Id).Select(a => a.PassportNo).FirstOrDefault(),
                GovtTrackingId = x.Applicants.Where(a => a.VisaAppId != null || a.GovtFeeReference != null).OrderBy(a => a.Id).Select(a => a.VisaAppId ?? a.GovtFeeReference).FirstOrDefault(),
                ApplicantFirstName = x.Applicants.OrderBy(a => a.Id).Select(a => a.FName).FirstOrDefault(),
                ApplicantFirstNames = x.Applicants.Select(a => a.FName).ToList(),
                x.VisaPackName,
                x.ClientType,
                x.ProcessingStatus,
                x.IsDealClosed,
                x.OperationClosed,
                x.PaymentStatus,
                PaymentMethod = x.Applicants.OrderBy(a => a.Id).Select(a => a.PaymentMethod).FirstOrDefault(),
                x.PayableAmount,
                x.Profit,
                ApplicantCount = x.Applicants.Count(),
                ApplicantNames = x.Applicants.Select(a => ((a.FName ?? "") + " " + (a.LName ?? "")).Trim()).ToList(),
                x.CreatedAt,
                x.VisaDocument,
                x.SupplierId,
                x.SupplierName
            })
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, rawItemsTask);



        var items = rawItemsTask.Result.Select(x =>
        {
            var countryName = countryMap.TryGetValue(x.CountryId, out var cName) ? cName : x.VisaPackName;
            var firstNames = x.ApplicantFirstNames?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList() ?? new List<string>();
            var applicantFirstNamesStr = string.Join(", ", firstNames);
            var applicantFirstName = firstNames.FirstOrDefault() ?? x.ApplicantFirstName;
            return new VisaApplicationSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                HeadName = x.HeadName,
                SalesmanName = x.SalesmanName,
                VisaPackName = x.VisaPackName,
                CountryName = countryName,
                Country = countryName,
                PassportNumber = x.PassportNo,
                PassportNo = x.PassportNo,
                GovtId = x.GovtTrackingId,
                GovtTrackingId = x.GovtTrackingId,
                ApplicantFirstName = applicantFirstName,
                ApplicantFirstNames = applicantFirstNamesStr,
            ClientType = x.ClientType,
            ProcessingStatus = x.ProcessingStatus,
            IsDealClosed = x.IsDealClosed,
            OperationClosed = x.OperationClosed,
            PaymentStatus = x.PaymentStatus,
            PaymentMethod = x.PaymentMethod,
            PayableAmount = x.PayableAmount,
            Profit = x.Profit,
            ApplicantCount = x.ApplicantCount,
            ApplicantNames = string.Join(", ", x.ApplicantNames.Where(n => !string.IsNullOrWhiteSpace(n))),
            CreatedAt = x.CreatedAt,
            VisaDocument = x.VisaDocument,
            SupplierId = x.SupplierId,
            SupplierName = x.SupplierName,
            Supplier = x.SupplierName
        };
        }).ToList();

        if (items.Count > 0)
        {
            var assignments = await _assignmentClient.GetActiveAssignmentsAsync("Visa", items.Select(x => x.Id), ct);
            foreach (var item in items)
            {
                if (assignments.TryGetValue(item.Id, out var a))
                {
                    item.AssignedOperatorName = a.AssignedOperatorName;
                    item.AssignedAt = a.AssignedAt;
                    item.AssignmentStatus = a.Status;
                }
            }
        }

        return PagedResponse<VisaApplicationSummaryResponse>.Create(items, totalTask.Result, page, pageSize);
    }

    public async Task<VisaApplicationDetailResponse?> GetVisaApplicationByIdAsync(
        long id, CancellationToken ct)
    {
        var result = await _db.SalesVisaApplications
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VisaApplicationDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                UserId = x.UserId,
                CountryId = x.CountryId,
                VisaPackId = x.VisaPackId,
                VisaPackName = x.VisaPackName,
                ClientType = x.ClientType,
                Agent = x.Agent,
                AgentId = x.AgentId,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PaymentStatus = x.PaymentStatus,
                VisaDocument = x.VisaDocument,
                ApprovalNote = x.ApprovalNote,
                RejectionDocument = x.RejectionDocument,
                RejectionNote = x.RejectionNote,
                RefundNote = x.RefundNote,
                DocumentRequestNote = x.DocumentRequestNote,
                DocumentRequestDate = x.DocumentRequestDate,
                PayableAmount = x.PayableAmount,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                Profit = x.Profit,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                RejectionFee = x.RejectionFee,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Applicants = x.Applicants.Select(p => new VisaApplicantResponse(
                    p.Id,
                    p.Prefix,
                    p.FName,
                    p.LName,
                    p.Dob,
                    p.Nationality,
                    p.CareOf,
                    p.ContactNo,
                    p.EmailId,
                    p.PassportNo,
                    p.IqamaHotelNo,
                    p.PayableAmount,
                    p.ExpectedSupplierCost,
                    p.Profit,
                    p.IsInhouse,
                    p.PaymentMethod,
                    p.PaymentStatus,
                    p.PaymentProof,
                    p.PaymentReference,
                    p.BankId,
                    p.BankName,
                    p.PassportCopy,
                    p.BankDoc,
                    p.VisaAppId ?? p.GovtFeeReference
                )).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (result != null)
        {
            string? countryName = null;
            try
            {
                var countries = await _agentsApiClient.GetVisaCountriesAsync(ct);
                var match = countries.Find(c => c.Id == result.CountryId);
                countryName = match?.Name;
            }
            catch
            {
                // Fallback silently if master api fails
            }

            var assignments = await _assignmentClient.GetActiveAssignmentsAsync("Visa", new[] { result.Id }, ct);
            string? assignedOpName = null;
            DateTime? assignedAt = null;
            OperationalAssignmentStatus? assignStatus = null;
            if (assignments.TryGetValue(result.Id, out var a))
            {
                assignedOpName = a.AssignedOperatorName;
                assignedAt = a.AssignedAt;
                assignStatus = a.Status;
            }

            var firstApp = result.Applicants.FirstOrDefault();
            var applicantIds = result.Applicants.Select(p => p.Id).ToList();
            var govtFees = await _db.SalesVisaGovtFees
                .AsNoTracking()
                .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
                .OrderBy(g => g.CreatedAt)
                .Select(g => new GovtFeeDetailResponse
                {
                    Id = g.Id,
                    RowKey = g.RowKey,
                    AccountsPaymentId = g.AccountsPaymentId,
                    SupplierId = g.SupplierId,
                    SupplierName = g.SupplierName,
                    Description = g.Description,
                    Amount = g.Amount,
                    PaymentReference = g.PaymentReference,
                    Status = g.Status,
                    PaymentProof = g.PaymentProof,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync(ct);

            var localFeeItems = govtFees.Select(g => new TexasPortal.Application.Sales.Services.ReconciledFeeItem(
                g.Id, g.Amount, g.Description, g.PaymentReference, g.Status
            )).ToList();

            var reconciliation = await _reconciliationService.ReconcileVisaFeesAsync(result.Id, localFeeItems, ct);

            govtFees = reconciliation.ReconciledFees.Select(f =>
            {
                var orig = govtFees.FirstOrDefault(l => l.Id == f.LocalId);
                return new GovtFeeDetailResponse
                {
                    Id = f.LocalId,
                    RowKey = orig?.RowKey,
                    AccountsPaymentId = orig?.AccountsPaymentId,
                    SupplierId = orig?.SupplierId,
                    SupplierName = orig?.SupplierName,
                    Amount = f.Amount,
                    Description = f.Description,
                    PaymentReference = f.PaymentReference,
                    Status = f.Status,
                    PaymentProof = orig?.PaymentProof,
                    CreatedAt = orig?.CreatedAt ?? DateTime.UtcNow
                };
            }).ToList();

            var effectiveStatus = result.ProcessingStatus;
            if (string.Equals(effectiveStatus, "under review", StringComparison.OrdinalIgnoreCase) &&
                (!string.IsNullOrWhiteSpace(result.ApprovalNote) || !string.IsNullOrWhiteSpace(result.VisaDocument)))
            {
                effectiveStatus = "approved";
            }

            var effectiveSupplierCost = reconciliation.EffectiveSupplierCost;
            var effectiveProfit = string.Equals(effectiveStatus, "rejected", StringComparison.OrdinalIgnoreCase)
                ? result.RejectionFee
                : (result.PayableAmount - effectiveSupplierCost);

            return new VisaApplicationDetailResponse
            {
                Id = result.Id,
                DocumentNumber = result.DocumentNumber,
                UserId = result.UserId,
                CountryId = result.CountryId,
                CountryName = countryName ?? $"Country #{result.CountryId}",
                VisaPackId = result.VisaPackId,
                VisaPackName = result.VisaPackName,
                ClientType = result.ClientType,
                Agent = result.Agent,
                AgentId = result.AgentId,
                HeadId = result.HeadId,
                HeadName = result.HeadName,
                IsDealClosed = result.IsDealClosed,
                OperationClosed = result.OperationClosed,
                ProcessingStatus = effectiveStatus,
                PaymentStatus = result.PaymentStatus,
                VisaDocument = result.VisaDocument,
                ApprovalNote = result.ApprovalNote,
                RejectionDocument = result.RejectionDocument,
                RejectionNote = result.RejectionNote,
                RefundNote = result.RefundNote,
                DocumentRequestNote = result.DocumentRequestNote,
                DocumentRequestDate = result.DocumentRequestDate,
                PayableAmount = result.PayableAmount,
                ExpectedSupplierCost = effectiveSupplierCost,
                Profit = effectiveProfit,
                SupplierId = result.SupplierId,
                SupplierName = result.SupplierName,
                RejectionFee = result.RejectionFee,
                PaymentMethod = firstApp?.PaymentMethod,
                BankId = firstApp?.BankId,
                BankName = firstApp?.BankName,
                PaymentProof = firstApp?.PaymentProof,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
                AssignedOperatorName = assignedOpName,
                AssignedAt = assignedAt,
                AssignmentStatus = assignStatus,
                Applicants = result.Applicants,
                GovtFees = govtFees
            };
        }

        return result;
    }

    public async Task<PagedResponse<VfsAppointmentSummaryResponse>> GetVfsAppointmentsAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        using var dbCount = _dbContextFactory.CreateDbContext();
        using var dbList = _dbContextFactory.CreateDbContext();

        var qCount = dbCount.VfsAppointments.AsNoTracking().Where(x => x.DeletedAt == null);
        var qList = dbList.VfsAppointments.AsNoTracking().Where(x => x.DeletedAt == null);

        if (startDate.HasValue)
        {
            qCount = qCount.Where(x => x.CreatedAt >= startDate.Value);
            qList = qList.Where(x => x.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            qCount = qCount.Where(x => x.CreatedAt <= endOfDay);
            qList = qList.Where(x => x.CreatedAt <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            var targetStatus = NormalizeStatus(status);
            if (targetStatus == "pending")
            {
                qCount = qCount.Where(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || x.ProcessingStatus.ToLower() == "pending");
                qList = qList.Where(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || x.ProcessingStatus.ToLower() == "pending");
            }
            else if (targetStatus == "under review")
            {
                qCount = qCount.Where(x => x.ProcessingStatus.ToLower() == "under review" || x.ProcessingStatus.ToLower() == "processing" || x.ProcessingStatus.ToLower() == "underreview");
                qList = qList.Where(x => x.ProcessingStatus.ToLower() == "under review" || x.ProcessingStatus.ToLower() == "processing" || x.ProcessingStatus.ToLower() == "underreview");
            }
            else
            {
                qCount = qCount.Where(x => x.ProcessingStatus.ToLower() == targetStatus);
                qList = qList.Where(x => x.ProcessingStatus.ToLower() == targetStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            qCount = qCount.Where(x =>
                (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                x.Name.ToLower().Contains(term) ||
                x.Country.ToLower().Contains(term) ||
                (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term))
            );
            qList = qList.Where(x =>
                (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                x.Name.ToLower().Contains(term) ||
                x.Country.ToLower().Contains(term) ||
                (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term))
            );
        }

        qList = qList.OrderByDescending(x => x.CreatedAt);

        if (isDealClosed.HasValue)
        {
            qList = qList.Where(x => x.IsDealClosed == isDealClosed.Value);
            qCount = qCount.Where(x => x.IsDealClosed == isDealClosed.Value);
        }

        var totalTask = qCount.CountAsync(ct);
        var itemsTask = qList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VfsAppointmentSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.Creator.Name,
                Name = x.Name,
                Country = x.Country,
                VisaType = x.VisaType,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PayableAmount = x.PayableAmount,
                Profit = x.Profit,
                AppointmentDate = x.AppointmentDate,
                CreatedAt = x.CreatedAt,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                Supplier = x.Supplier
            })
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itemsTask);
        var vfsItems = itemsTask.Result;

        if (vfsItems.Count > 0)
        {
            var assignments = await _assignmentClient.GetActiveAssignmentsAsync("Vfs", vfsItems.Select(x => x.Id), ct);
            foreach (var item in vfsItems)
            {
                if (assignments.TryGetValue(item.Id, out var a))
                {
                    item.AssignedOperatorName = a.AssignedOperatorName;
                    item.AssignedAt = a.AssignedAt;
                    item.AssignmentStatus = a.Status;
                }
            }
        }

        return PagedResponse<VfsAppointmentSummaryResponse>.Create(vfsItems, totalTask.Result, page, pageSize);
    }

    public async Task<VfsAppointmentDetailResponse?> GetVfsAppointmentByIdAsync(
        long id, CancellationToken ct)
    {
        var result = await _db.VfsAppointments
            .AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new VfsAppointmentDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedBy = x.CreatedBy,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ClientType = x.ClientType,
                Agent = x.Agent,
                AgentId = x.AgentId,
                Prefix = x.Prefix,
                Name = x.Name,
                Supplier = x.Supplier,
                SupplierId = x.SupplierId,
                Country = x.Country,
                VisaType = x.VisaType,
                PayableAmount = x.PayableAmount,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                Profit = x.Profit,
                PaymentMode = string.IsNullOrWhiteSpace(x.PaymentMode) ? "not filled by staff" : x.PaymentMode,
                BankId = x.BankId,
                BankName = x.BankName,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                AppointmentDate = x.AppointmentDate,
                AppointmentTime = x.AppointmentTime,
                VfsReceipt = x.VfsReceipt,
                PassportCopy = x.PassportCopy,
                PaymentProof = x.PaymentProof,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (result != null)
        {
            var govtFees = await _db.VfsAppointmentGovtFees
                .AsNoTracking()
                .Where(g => g.VfsAppointmentId == id)
                .OrderBy(g => g.CreatedAt)
                .Select(g => new GovtFeeDetailResponse
                {
                    Id = g.Id,
                    RowKey = g.RowKey,
                    AccountsPaymentId = g.AccountsPaymentId,
                    SupplierId = g.SupplierId,
                    SupplierName = g.SupplierName,
                    Amount = g.Amount,
                    Description = "VFS Government Fee",
                    PaymentReference = g.PaymentReference,
                    Status = g.Status,
                    PaymentProof = g.PaymentProof,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync(ct);

            var localFeeItems = govtFees.Select(g => new TexasPortal.Application.Sales.Services.ReconciledFeeItem(
                g.Id, g.Amount, g.Description, g.PaymentReference, g.Status
            )).ToList();

            var reconciliation = await _reconciliationService.ReconcileVfsFeesAsync(result.Id, localFeeItems, ct);

            govtFees = reconciliation.ReconciledFees.Select(f =>
            {
                var orig = govtFees.FirstOrDefault(l => l.Id == f.LocalId);
                return new GovtFeeDetailResponse
                {
                    Id = f.LocalId,
                    RowKey = orig?.RowKey,
                    AccountsPaymentId = orig?.AccountsPaymentId,
                    SupplierId = orig?.SupplierId,
                    SupplierName = orig?.SupplierName,
                    Amount = f.Amount,
                    Description = f.Description,
                    PaymentReference = f.PaymentReference,
                    Status = f.Status,
                    PaymentProof = orig?.PaymentProof,
                    CreatedAt = orig?.CreatedAt ?? DateTime.UtcNow
                };
            }).ToList();

            result.ExpectedSupplierCost = reconciliation.EffectiveSupplierCost;
            result.Profit = result.PayableAmount - result.ExpectedSupplierCost;
            result.GovtFees = govtFees;
        }

        return result;
    }

    public async Task<PagedResponse<HolidayPackageSummaryResponse>> GetHolidayPackagesAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        using var dbCount = _dbContextFactory.CreateDbContext();
        using var dbList = _dbContextFactory.CreateDbContext();

        var qCount = dbCount.HolidayPackages.AsNoTracking().Where(x => x.DeletedAt == null);
        var qList = dbList.HolidayPackages.AsNoTracking().Where(x => x.DeletedAt == null);

        if (startDate.HasValue)
        {
            qCount = qCount.Where(x => x.CreatedAt >= startDate.Value);
            qList = qList.Where(x => x.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            qCount = qCount.Where(x => x.CreatedAt <= endOfDay);
            qList = qList.Where(x => x.CreatedAt <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            var targetStatus = NormalizeStatus(status);
            if (targetStatus == "pending")
            {
                qCount = qCount.Where(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || x.ProcessingStatus.ToLower() == "pending");
                qList = qList.Where(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || x.ProcessingStatus.ToLower() == "pending");
            }
            else if (targetStatus == "under review")
            {
                qCount = qCount.Where(x => x.ProcessingStatus.ToLower() == "under review" || x.ProcessingStatus.ToLower() == "processing" || x.ProcessingStatus.ToLower() == "underreview");
                qList = qList.Where(x => x.ProcessingStatus.ToLower() == "under review" || x.ProcessingStatus.ToLower() == "processing" || x.ProcessingStatus.ToLower() == "underreview");
            }
            else
            {
                qCount = qCount.Where(x => x.ProcessingStatus.ToLower() == targetStatus);
                qList = qList.Where(x => x.ProcessingStatus.ToLower() == targetStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            qCount = qCount.Where(x =>
                (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                x.PassengerName.ToLower().Contains(term) ||
                x.Country.ToLower().Contains(term) ||
                (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term))
            );
            qList = qList.Where(x =>
                (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                x.PassengerName.ToLower().Contains(term) ||
                x.Country.ToLower().Contains(term) ||
                (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term))
            );
        }

        qList = qList.OrderByDescending(x => x.CreatedAt);

        if (isDealClosed.HasValue)
        {
            qList = qList.Where(x => x.IsDealClosed == isDealClosed.Value);
            qCount = qCount.Where(x => x.IsDealClosed == isDealClosed.Value);
        }

        var totalTask = qCount.CountAsync(ct);
        var itemsTask = qList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HolidayPackageSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.Creator.Name,
                PassengerName = x.PassengerName,
                Country = x.Country,
                Days = x.Days,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PayableAmount = x.PayableAmount,
                Profit = x.Profit,
                CreatedAt = x.CreatedAt,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                Supplier = x.Supplier
            })
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itemsTask);

        var holItems = itemsTask.Result;

        if (holItems.Count > 0)
        {
            var assignments = await _assignmentClient.GetActiveAssignmentsAsync("Holiday", holItems.Select(x => x.Id), ct);
            foreach (var item in holItems)
            {
                if (assignments.TryGetValue(item.Id, out var a))
                {
                    item.AssignedOperatorName = a.AssignedOperatorName;
                    item.AssignedAt = a.AssignedAt;
                    item.AssignmentStatus = a.Status;
                }
            }
        }

        return PagedResponse<HolidayPackageSummaryResponse>.Create(holItems, totalTask.Result, page, pageSize);
    }

    public async Task<HolidayPackageDetailResponse?> GetHolidayPackageByIdAsync(
        long id, CancellationToken ct)
    {
        var result = await _db.HolidayPackages
            .AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new HolidayPackageDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedBy = x.CreatedBy,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ClientType = x.ClientType,
                Agent = x.Agent,
                AgentId = x.AgentId,
                Prefix = x.Prefix,
                PassengerName = x.PassengerName,
                Supplier = x.Supplier,
                SupplierId = x.SupplierId,
                Country = x.Country,
                Days = x.Days,
                PayableAmount = x.PayableAmount,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                Profit = x.Profit,
                PaymentMode = string.IsNullOrWhiteSpace(x.PaymentMode) ? "not filled by staff" : x.PaymentMode,
                BankId = x.BankId,
                BankName = x.BankName,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PaymentProof = x.PaymentProof,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (result != null)
        {
            var docs = await _db.HolidayPackageDocuments
                .AsNoTracking()
                .Where(d => d.HolidayPackageId == id)
                .OrderBy(d => d.CreatedAt)
                .Select(d => new HolidayPackageDocumentResponse
                {
                    Id = d.Id,
                    DocumentPath = d.DocumentPath,
                    DocumentType = d.DocumentType,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync(ct);

            result.Documents = docs;

            var govtFees = await _db.HolidayPackageGovtFees
                .AsNoTracking()
                .Where(g => g.HolidayPackageId == id)
                .OrderBy(g => g.CreatedAt)
                .Select(g => new GovtFeeDetailResponse
                {
                    Id = g.Id,
                    RowKey = g.RowKey,
                    AccountsPaymentId = g.AccountsPaymentId,
                    Amount = g.Amount,
                    Description = g.Description ?? "Holiday Package Fee",
                    SupplierId = g.SupplierId,
                    SupplierName = g.SupplierName,
                    PaymentReference = g.PaymentReference,
                    Status = g.Status,
                    PaymentProof = g.PaymentProof,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync(ct);

            var localFeeItems = govtFees.Select(g => new TexasPortal.Application.Sales.Services.ReconciledFeeItem(
                g.Id, g.Amount, g.Description, g.PaymentReference, g.Status
            )).ToList();

            var reconciliation = await _reconciliationService.ReconcileHolidayFeesAsync(result.Id, localFeeItems, ct);

            govtFees = reconciliation.ReconciledFees.Select(f =>
            {
                var orig = govtFees.FirstOrDefault(l => l.Id == f.LocalId);
                return new GovtFeeDetailResponse
                {
                    Id = f.LocalId,
                    RowKey = orig?.RowKey,
                    AccountsPaymentId = orig?.AccountsPaymentId,
                    SupplierId = orig?.SupplierId,
                    SupplierName = orig?.SupplierName,
                    Amount = f.Amount,
                    Description = f.Description,
                    PaymentReference = f.PaymentReference,
                    Status = f.Status,
                    PaymentProof = orig?.PaymentProof,
                    CreatedAt = orig?.CreatedAt ?? DateTime.UtcNow
                };
            }).ToList();

            result.ExpectedSupplierCost = reconciliation.EffectiveSupplierCost;
            result.Profit = result.PayableAmount - result.ExpectedSupplierCost;
            result.GovtFees = govtFees;
        }

        return result;
    }

    // Global queries for Flights
    public async Task<PagedResponse<FlightBookingSummaryResponse>> GetFlightBookingsAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        var q = _db.FlightBookings.AsNoTracking();
        if (isDealClosed.HasValue) q = q.Where(x => x.IsDealClosed == isDealClosed.Value);
        if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(x => x.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all") q = q.Where(x => x.PaymentStatus.ToLower() == status.ToLower());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x => (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                             (x.FromLocation != null && x.FromLocation.ToLower().Contains(term)) ||
                             (x.ToLocation != null && x.ToLocation.ToLower().Contains(term)) ||
                             (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)) ||
                             x.Passengers.Any(p => p.PassengerName != null && p.PassengerName.ToLower().Contains(term)));
        }
        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var bookingIds = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(ct);
        var rawItems = await _db.FlightBookings.AsNoTracking()
            .Where(x => bookingIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.User.Name,
                x.FromLocation,
                x.ToLocation,
                x.TravelDate,
                x.AirlineCompany,
                x.TicketPnr,
                x.PaymentMethod,
                x.PaymentStatus,
                x.ProcessingStatus,
                x.IsDealClosed,
                x.OperationClosed,
                x.PayableAmount,
                PassengerCount = x.Passengers.Count(),
                PassengerNames = x.Passengers.Select(p => p.PassengerName).ToList(),
                x.CreatedAt,
                x.SupplierId,
                x.SupplierName,
                x.Supplier
            })
            .ToListAsync(ct);

        var items = rawItems.Select(x => new FlightBookingSummaryResponse
        {
            Id = x.Id,
            DocumentNumber = x.DocumentNumber,
            HeadName = x.HeadName,
            SalesmanName = x.SalesmanName,
            FromLocation = x.FromLocation,
            ToLocation = x.ToLocation,
            TravelDate = x.TravelDate,
            AirlineCompany = x.AirlineCompany,
            TicketPnr = x.TicketPnr,
            PaymentMethod = x.PaymentMethod,
            PaymentStatus = x.PaymentStatus,
            ProcessingStatus = x.ProcessingStatus,
            IsDealClosed = x.IsDealClosed,
            OperationClosed = x.OperationClosed,
            PayableAmount = x.PayableAmount,
            PassengerCount = x.PassengerCount,
            PassengerNames = string.Join(", ", x.PassengerNames.Where(p => !string.IsNullOrWhiteSpace(p))),
            CreatedAt = x.CreatedAt,
            SupplierId = x.SupplierId,
            SupplierName = x.SupplierName,
            Supplier = x.Supplier
        }).ToList();

        return PagedResponse<FlightBookingSummaryResponse>.Create(items, total, page, pageSize);
    }

    public async Task<FlightBookingDetailResponse?> GetFlightBookingByIdAsync(
        long id, CancellationToken ct)
    {
        return await _db.FlightBookings
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new FlightBookingDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                UserId = x.UserId,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ClientType = x.ClientType,
                Agent = x.Agent,
                AgentId = x.AgentId,
                CareOf = x.CareOf,
                Supplier = x.Supplier,
                IsInhouse = x.IsInhouse,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                Profit = x.Profit,
                FromLocation = x.FromLocation,
                ToLocation = x.ToLocation,
                TravelDate = x.TravelDate,
                AirlineCompany = x.AirlineCompany,
                TicketPnr = x.TicketPnr,
                PaymentMethod = x.PaymentMethod,
                BankId = x.BankId,
                BankName = x.BankName,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PayableAmount = x.PayableAmount,
                PaymentProof = x.PaymentProof,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Passengers = x.Passengers.Select(p => new FlightPassengerResponse(
                    p.Id,
                    p.Prefix,
                    p.PassengerName,
                    p.PayableAmount,
                    p.ExpectedSupplierCost,
                    p.Profit,
                    p.IsInhouse,
                    p.SupplierId,
                    p.SupplierName,
                    p.TicketPnr
                )).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    // Global queries for Hotels
    public async Task<PagedResponse<HotelBookingSummaryResponse>> GetHotelBookingsAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        var q = _db.HotelBookings.AsNoTracking();
        if (isDealClosed.HasValue) q = q.Where(x => x.IsDealClosed == isDealClosed.Value);
        if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(x => x.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all") q = q.Where(x => x.PaymentStatus.ToLower() == status.ToLower());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x => (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                             (x.GuestName != null && x.GuestName.ToLower().Contains(term)) ||
                             (x.Country != null && x.Country.ToLower().Contains(term)) ||
                             (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)));
        }
        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HotelBookingSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.User.Name,
                GuestName = x.GuestName,
                HotelName = x.HotelName,
                Country = x.Country,
                Days = x.Days,
                CheckInDate = x.CheckInDate,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                OrderStatus = x.OrderStatus,
                PayableAmount = x.PayableAmount,
                Profit = x.Profit,
                CreatedAt = x.CreatedAt,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                Supplier = x.Supplier
            })
            .ToListAsync(ct);

        return PagedResponse<HotelBookingSummaryResponse>.Create(items, total, page, pageSize);
    }

    public async Task<HotelBookingDetailResponse?> GetHotelBookingByIdAsync(
        long id, CancellationToken ct)
    {
        return await _db.HotelBookings
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new HotelBookingDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                UserId = x.UserId,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ClientType = x.ClientType,
                Agent = x.Agent,
                AgentId = x.AgentId,
                IsInhouse = x.IsInhouse,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                PayableAmount = x.PayableAmount,
                Profit = x.Profit,
                Prefix = x.Prefix,
                GuestName = x.GuestName,
                HotelName = x.HotelName,
                CareOf = x.CareOf,
                Country = x.Country,
                Days = x.Days,
                CheckInDate = x.CheckInDate,
                PaymentMethod = x.PaymentMethod,
                BankId = x.BankId,
                BankName = x.BankName,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                OrderStatus = x.OrderStatus,
                PaymentProof = x.PaymentProof,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Guests = x.Guests.Select(g => new HotelGuestResponse(
                    g.Id,
                    g.Prefix,
                    g.GuestName,
                    g.PayableAmount,
                    g.ExpectedSupplierCost,
                    g.Profit,
                    g.IsInhouse,
                    g.SupplierId,
                    g.SupplierName
                )).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    // Global queries for Transportation
    public async Task<PagedResponse<TransportationSummaryResponse>> GetTransportationsAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        var q = _db.Transportations.AsNoTracking().Where(x => x.DeletedAt == null);
        if (isDealClosed.HasValue) q = q.Where(x => x.IsDealClosed == isDealClosed.Value);
        if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(x => x.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all") q = q.Where(x => x.PaymentStatus.ToLower() == status.ToLower());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x => (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                             (x.Name != null && x.Name.ToLower().Contains(term)) ||
                             (x.Pickup != null && x.Pickup.ToLower().Contains(term)) ||
                             (x.Drop != null && x.Drop.ToLower().Contains(term)) ||
                             (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)));
        }
        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TransportationSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.Creator.Name,
                Name = x.Name,
                Pickup = x.Pickup,
                Drop = x.Drop,
                DropDate = x.DropDate,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PayableAmount = x.PayableAmount,
                Profit = x.Profit,
                CreatedAt = x.CreatedAt,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                Supplier = x.Supplier
            })
            .ToListAsync(ct);

        return PagedResponse<TransportationSummaryResponse>.Create(items, total, page, pageSize);
    }

    public async Task<TransportationDetailResponse?> GetTransportationByIdAsync(
        long id, CancellationToken ct)
    {
        return await _db.Transportations
            .AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new TransportationDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedBy = x.CreatedBy,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ClientType = x.ClientType,
                Agent = x.Agent,
                AgentId = x.AgentId,
                Prefix = x.Prefix,
                Name = x.Name,
                CareOf = x.CareOf,
                Supplier = x.Supplier,
                IsInhouse = x.IsInhouse,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                Pickup = x.Pickup,
                Drop = x.Drop,
                DropDate = x.DropDate,
                DropTime = x.DropTime,
                PayableAmount = x.PayableAmount,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                Profit = x.Profit,
                PaymentMethod = x.PaymentMethod,
                BankId = x.BankId,
                BankName = x.BankName,
                Vehicle = x.Vehicle,
                PaymentProof = x.PaymentProof,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Passengers = x.Passengers.Select(p => new TransportPassengerResponse(
                    p.Id,
                    p.Prefix,
                    p.Name,
                    p.PayableAmount,
                    p.ExpectedSupplierCost,
                    p.Profit,
                    p.IsInhouse,
                    p.SupplierId,
                    p.SupplierName
                )).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    // Global queries for Other Services
    public async Task<PagedResponse<OtherServiceSummaryResponse>> GetOtherServicesAsync(
        int page, int pageSize, string? status, string? search, DateTime? startDate = null, DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default)
    {
        var q = _db.OtherServices.AsNoTracking().Where(x => x.DeletedAt == null);
        if (isDealClosed.HasValue) q = q.Where(x => x.IsDealClosed == isDealClosed.Value);
        if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(x => x.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all") q = q.Where(x => x.PaymentStatus.ToLower() == status.ToLower());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x => (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                             (x.ServiceName != null && x.ServiceName.ToLower().Contains(term)) ||
                             (x.Name != null && x.Name.ToLower().Contains(term)) ||
                             (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term)));
        }
        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OtherServiceSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                SalesmanName = x.Creator.Name,
                ServiceName = x.ServiceName,
                Name = x.Name,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                PayableAmount = x.PayableAmount,
                Profit = x.Profit,
                CreatedAt = x.CreatedAt,
                SupplierId = x.SupplierId,
                SupplierName = x.SupplierName,
                Supplier = x.Supplier
            })
            .ToListAsync(ct);

        return PagedResponse<OtherServiceSummaryResponse>.Create(items, total, page, pageSize);
    }

    public async Task<OtherServiceDetailResponse?> GetOtherServiceByIdAsync(
        long id, CancellationToken ct)
    {
        return await _db.OtherServices
            .AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new OtherServiceDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedBy = x.CreatedBy,
                HeadId = x.HeadId,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : x.Agent,
                ClientType = x.ClientType,
                Prefix = x.Prefix,
                Name = x.Name,
                CareOf = x.CareOf,
                Agent = x.Agent,
                AgentId = x.AgentId,
                SupplierName = x.SupplierName,
                SupplierId = x.SupplierId,
                ServiceName = x.ServiceName,
                Narration = x.Narration,
                PayableAmount = x.PayableAmount,
                ExpectedSupplierCost = x.ExpectedSupplierCost,
                Profit = x.Profit,
                PaymentMethod = x.PaymentMethod,
                BankId = x.BankId,
                BankName = x.BankName,
                PaymentStatus = x.PaymentStatus,
                ProcessingStatus = x.ProcessingStatus,
                IsDealClosed = x.IsDealClosed,
                OperationClosed = x.OperationClosed,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    // Global queries for Customer Heads
    // Global queries for Customer Heads & Agents
    public async Task<PagedResponse<HeadSummaryResponse>> GetHeadsAsync(
        int page, int pageSize, string? search, string? type = null, long? salesmanId = null, CancellationToken ct = default)
    {
        var q = _db.Heads.AsNoTracking().Where(h => h.DeletedAt == null);
        if (salesmanId.HasValue && salesmanId.Value > 0)
        {
            q = q.Where(h => h.CreatedBy == salesmanId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(h => (h.FirstName != null && h.FirstName.ToLower().Contains(term)) ||
                             (h.LastName != null && h.LastName.ToLower().Contains(term)) ||
                             h.Email.ToLower().Contains(term) ||
                             h.MobileNo.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var filterType = type.Trim().ToLower();
            q = q.Where(h => h.Type.ToLower() == filterType);
        }

        q = q.OrderByDescending(h => h.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HeadSummaryResponse
            {
                Id = h.Id,
                FirstName = h.FirstName,
                LastName = h.LastName,
                Email = h.Email,
                MobileNo = h.MobileNo,
                Nationality = h.Nationality,
                Type = string.IsNullOrWhiteSpace(h.Type) ? "customer" : h.Type,
                Status = h.Status,
                Wallet = h.Wallet,
                Address = h.Address,
                SalesmanId = h.CreatedBy,
                SalesmanName = h.Creator != null ? h.Creator.Name : null,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync(ct);

        return PagedResponse<HeadSummaryResponse>.Create(items, total, page, pageSize);
    }

    public async Task<HeadSummaryStatsResponse> GetHeadsSummaryAsync(long? salesmanId = null, CancellationToken ct = default)
    {
        var q = _db.Heads.AsNoTracking().Where(h => h.DeletedAt == null);
        if (salesmanId.HasValue && salesmanId.Value > 0)
        {
            q = q.Where(h => h.CreatedBy == salesmanId.Value);
        }

        var totalCount = await q.CountAsync(ct);
        var customerCount = await q.CountAsync(h => string.IsNullOrWhiteSpace(h.Type) || h.Type.ToLower() == "customer" || h.Type.ToLower() == "head", ct);
        var agentCount = await q.CountAsync(h => h.Type.ToLower() == "agent", ct);
        var totalWallet = await q.SumAsync(h => (decimal?)h.Wallet, ct) ?? 0m;

        return new HeadSummaryStatsResponse
        {
            TotalCount = totalCount,
            CustomerCount = customerCount,
            AgentCount = agentCount,
            TotalWallet = totalWallet
        };
    }

    public async Task<HeadDetailResponse?> GetHeadByIdAsync(
        long id, string? type = null, CancellationToken ct = default)
    {
        return await _db.Heads
            .AsNoTracking()
            .Where(h => h.Id == id && h.DeletedAt == null)
            .Select(h => new HeadDetailResponse
            {
                Id = h.Id,
                FirstName = h.FirstName,
                LastName = h.LastName,
                Email = h.Email,
                MobileNo = h.MobileNo,
                Nationality = h.Nationality,
                Type = string.IsNullOrWhiteSpace(h.Type) ? "customer" : h.Type,
                Status = h.Status,
                Wallet = h.Wallet,
                Address = h.Address,
                SalesmanName = h.Creator != null ? h.Creator.Name : null,
                CreatedAt = h.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    private static bool IsSameServiceType(string? type1, string? type2)
    {
        if (string.Equals(type1, type2, StringComparison.OrdinalIgnoreCase)) return true;
        var norm1 = NormalizeServiceType(type1);
        var norm2 = NormalizeServiceType(type2);
        return string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeServiceType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "";
        return type.ToLowerInvariant() switch
        {
            "visa" or "visas" or "visa_application" or "visaapplication" => "visa",
            "vfs" or "vfsappointment" or "vfs_appointment" => "vfs",
            "holiday" or "holidays" or "holidaypackage" or "holiday_package" => "holiday",
            "flight" or "flights" or "flightbooking" or "flight_booking" => "flight",
            "hotel" or "hotels" or "hotelbooking" or "hotel_booking" => "hotel",
            "transport" or "transportation" or "transportations" => "transportation",
            "other" or "others" or "otherservice" or "other_service" => "other",
            "receipt" or "receipts" or "deposit" or "credit_settlement" => "receipt",
            _ => type.ToLowerInvariant()
        };
    }

    public async Task<PagedResponse<HeadLedgerEntryResponse>> GetHeadLedgerAsync(
        long headId,
        int page,
        int pageSize,
        string? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? serviceType = null,
        string? search = null,
        string? sort = null,
        CancellationToken ct = default)
    {
        var entries = new List<HeadLedgerEntryResponse>();
        bool isAgent = string.Equals(type, "agent", StringComparison.OrdinalIgnoreCase);

        DateTime? effectiveStart = startDate.HasValue ? startDate.Value.Date : null;
        DateTime? effectiveEnd = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddTicks(-1) : null;

        List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>? visaCountries = null;
        var countryMap = new Dictionary<long, string>();
        try
        {
            visaCountries = await _agentsApiClient.GetVisaCountriesAsync(ct);
            if (visaCountries != null)
            {
                foreach (var c in visaCountries) countryMap[c.Id] = c.Name;
            }
        }
        catch { }

        // 2. Flight Bookings
        var flightsQuery = _db.FlightBookings.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) flightsQuery = flightsQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) flightsQuery = flightsQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var flights = await flightsQuery
            .Select(b => new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "flight_booking",
                Description = $"Flight Booking #{b.Id}: {b.FromLocation} -> {b.ToLocation}",
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        // 3. Hotel Bookings
        var hotelsQuery = _db.HotelBookings.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) hotelsQuery = hotelsQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) hotelsQuery = hotelsQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var hotels = await hotelsQuery
            .Select(b => new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "hotel_booking",
                Description = $"Hotel Booking #{b.Id}: {b.GuestName} ({b.Country})",
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        // 4. Visa Applications
        var visasQuery = _db.SalesVisaApplications.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) visasQuery = visasQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) visasQuery = visasQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var visasRaw = await visasQuery
            .Select(b => new
            {
                b.Id,
                b.CountryId,
                b.VisaPackName,
                b.PayableAmount,
                PaymentMethod = b.Applicants.Select(a => a.PaymentMethod).FirstOrDefault(),
                Applicants = b.Applicants.Select(a => new { a.FName, a.LName }).ToList(),
                b.CreatedAt
            })
            .ToListAsync(ct);

        var visas = visasRaw.Select(b =>
        {
            var appNames = b.Applicants.Select(a => ((a.FName ?? "") + " " + (a.LName ?? "")).Trim()).Where(n => !string.IsNullOrEmpty(n)).ToList();
            var applicantDisplay = appNames.Count switch
            {
                0 => "Applicant",
                1 => appNames[0],
                2 => $"{appNames[0]}, {appNames[1]}",
                _ => $"{appNames[0]} (+{appNames.Count - 1} more)"
            };

            var countryName = countryMap.TryGetValue(b.CountryId, out var cName) ? cName : null;
            var description = !string.IsNullOrWhiteSpace(countryName)
                ? $"Visa Application #{b.Id}: {applicantDisplay} - {countryName} ({b.VisaPackName})"
                : $"Visa Application #{b.Id}: {applicantDisplay} ({b.VisaPackName})";

            return new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "visa_application",
                Description = description,
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt
            };
        }).ToList();

        // 5. Holiday Packages
        var holidaysQuery = _db.HolidayPackages.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) holidaysQuery = holidaysQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) holidaysQuery = holidaysQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var holidays = await holidaysQuery
            .Select(b => new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "holiday_package",
                Description = $"Holiday Package #{b.Id}: {b.PassengerName} ({b.Country})",
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMode,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        // 6. Transportations
        var transportsQuery = _db.Transportations.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) transportsQuery = transportsQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) transportsQuery = transportsQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var transports = await transportsQuery
            .Select(b => new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "transportation",
                Description = $"Transportation #{b.Id}: {b.Name} ({b.Pickup} -> {b.Drop})",
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        // 7. Other Services
        var othersQuery = _db.OtherServices.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) othersQuery = othersQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) othersQuery = othersQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var others = await othersQuery
            .Select(b => new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "other_service",
                Description = $"Other Service #{b.Id}: {b.ServiceName} ({b.Name})",
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        // 8. VFS Appointments
        var vfsQuery = _db.VfsAppointments.AsNoTracking()
            .Where(b => isAgent ? b.AgentId == headId : b.HeadId == headId);
        if (effectiveStart.HasValue) vfsQuery = vfsQuery.Where(b => b.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) vfsQuery = vfsQuery.Where(b => b.CreatedAt <= effectiveEnd.Value);

        var vfsList = await vfsQuery
            .Select(b => new HeadLedgerEntryResponse
            {
                Id = b.Id,
                ServiceId = b.Id,
                Type = "vfs_appointment",
                Description = $"VFS Service #{b.Id}: {b.Name} ({b.Country})",
                Debit = b.PayableAmount,
                Credit = 0,
                Balance = -b.PayableAmount,
                PaymentMethod = b.PaymentMode,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        // 9. Receipts
        var receiptsQuery = _db.Receipts.AsNoTracking()
            .Where(r => (isAgent ? r.AgentId == headId : r.HeadId == headId) && (r.Status.ToLower() == "approved" || r.Status.ToLower() == "success") && r.DeletedAt == null);
        if (effectiveStart.HasValue) receiptsQuery = receiptsQuery.Where(r => r.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) receiptsQuery = receiptsQuery.Where(r => r.CreatedAt <= effectiveEnd.Value);

        var receiptsRaw = await receiptsQuery
            .Select(r => new
            {
                r.Id,
                r.DocumentNumber,
                r.ReceiptId,
                r.Service,
                r.Amount,
                r.PaymentMode,
                r.BankName,
                r.CreatedAt
            })
            .ToListAsync(ct);

        var receipts = receiptsRaw.Select(r =>
        {
            string accountName;
            var modeLower = (r.PaymentMode ?? "").ToLowerInvariant();
            if (modeLower == "cash")
            {
                accountName = !string.IsNullOrWhiteSpace(r.BankName) ? r.BankName : "Cash Account";
            }
            else if (modeLower == "wallet")
            {
                accountName = "Customer Wallet";
            }
            else
            {
                accountName = !string.IsNullOrWhiteSpace(r.BankName) ? r.BankName : "Bank Account";
            }

            var serviceLower = (r.Service ?? "").ToLowerInvariant();
            string description;
            if (serviceLower is "deposit" or "wallet_deposit")
            {
                description = $"Deposit #{r.DocumentNumber ?? r.ReceiptId}: via {accountName} ({r.PaymentMode})";
            }
            else
            {
                description = $"Receipt #{r.DocumentNumber ?? r.ReceiptId}: {r.Service} via {accountName} ({r.PaymentMode})";
            }

            return new HeadLedgerEntryResponse
            {
                Id = r.Id,
                ServiceId = r.Id,
                Type = "receipt",
                Description = description,
                Debit = 0,
                Credit = r.Amount,
                Balance = r.Amount,
                PaymentMethod = r.PaymentMode,
                CreatedAt = r.CreatedAt
            };
        }).ToList();

        // 1. Explicit HeadLedgers (Only for Customers, Agents are handled via Accounts Portal)
        var hlQuery = _db.HeadLedgers.AsNoTracking()
            .Where(l => !isAgent && l.HeadId == headId);
        if (effectiveStart.HasValue) hlQuery = hlQuery.Where(l => l.CreatedAt >= effectiveStart.Value);
        if (effectiveEnd.HasValue) hlQuery = hlQuery.Where(l => l.CreatedAt <= effectiveEnd.Value);

        var hlRaw = await hlQuery
            .Select(l => new
            {
                l.Id,
                l.ServiceType,
                l.ServiceId,
                l.Description,
                l.Debit,
                l.Credit,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var flightPayMap = flights.ToDictionary(f => f.Id, f => f.PaymentMethod);
        var hotelPayMap = hotels.ToDictionary(h => h.Id, h => h.PaymentMethod);
        var visaPayMap = visas.ToDictionary(v => v.Id, v => v.PaymentMethod);
        var holidayPayMap = holidays.ToDictionary(h => h.Id, h => h.PaymentMethod);
        var transportPayMap = transports.ToDictionary(t => t.Id, t => t.PaymentMethod);
        var otherPayMap = others.ToDictionary(o => o.Id, o => o.PaymentMethod);
        var vfsPayMap = vfsList.ToDictionary(v => v.Id, v => v.PaymentMethod);
        var receiptPayMap = receipts.ToDictionary(r => r.Id, r => r.PaymentMethod);

        var visaDescMap = visas.ToDictionary(v => v.Id, v => v.Description);
        var receiptDescMap = receipts.ToDictionary(r => r.Id, r => r.Description);
        var vfsDescMap = vfsList.ToDictionary(v => v.Id, v => v.Description);

        foreach (var l in hlRaw)
        {
            var sType = l.ServiceType;
            if (string.Equals(sType, "journal_entry", StringComparison.OrdinalIgnoreCase) &&
                l.Description != null &&
                (l.Description.Contains("Refund", StringComparison.OrdinalIgnoreCase) ||
                 l.Description.Contains("Return", StringComparison.OrdinalIgnoreCase)))
            {
                sType = "sales_return";
            }

            var typeLower = sType?.ToLowerInvariant();
            bool isBooking = typeLower is "visa" or "flight" or "hotel" or "holiday" or "transport" or "transportation" or "vfs" or "other" or "visa_application" or "flight_booking" or "hotel_booking" or "holiday_package" or "vfs_appointment" or "other_service" or "otherservice";

            decimal debit = l.Debit;
            decimal credit = isBooking ? 0m : l.Credit;

            string? pm = sType?.ToLowerInvariant() switch
            {
                "flight" or "flights" or "flight_booking" => flightPayMap.GetValueOrDefault(l.ServiceId),
                "hotel" or "hotels" or "hotel_booking" => hotelPayMap.GetValueOrDefault(l.ServiceId),
                "visa" or "visas" or "visa_application" => visaPayMap.GetValueOrDefault(l.ServiceId),
                "holiday" or "holidays" or "holiday_package" => holidayPayMap.GetValueOrDefault(l.ServiceId),
                "transport" or "transportation" => transportPayMap.GetValueOrDefault(l.ServiceId),
                "other" or "otherservice" or "other_service" => otherPayMap.GetValueOrDefault(l.ServiceId),
                "vfs" or "vfs_appointment" => vfsPayMap.GetValueOrDefault(l.ServiceId),
                "receipt" or "receipts" or "deposit" or "credit_settlement" => receiptPayMap.GetValueOrDefault(l.ServiceId),
                _ => null
            };

            var desc = l.Description;
            if (typeLower is "visa" or "visas" or "visa_application")
            {
                if (visaDescMap.TryGetValue(l.ServiceId, out var richVisaDesc))
                {
                    desc = richVisaDesc;
                }
            }
            else if (typeLower is "receipt" or "receipts" or "deposit" or "credit_settlement")
            {
                if (receiptDescMap.TryGetValue(l.ServiceId, out var richReceiptDesc))
                {
                    desc = richReceiptDesc;
                }
            }
            else if (typeLower is "vfs" or "vfs_appointment")
            {
                if (vfsDescMap.TryGetValue(l.ServiceId, out var richVfsDesc))
                {
                    desc = richVfsDesc;
                }
            }

            entries.Add(new HeadLedgerEntryResponse
            {
                Id = l.Id,
                ServiceId = l.ServiceId,
                Type = sType,
                Description = desc,
                Debit = debit,
                Credit = credit,
                Balance = credit - debit,
                PaymentMethod = pm,
                CreatedAt = l.CreatedAt
            });
        }

        bool IsAlreadyPresent(long bookingId, string targetType)
        {
            return entries.Any(e =>
                IsSameServiceType(e.Type, targetType) &&
                ((e.ServiceId.HasValue && e.ServiceId.Value == bookingId) || e.Id == bookingId));
        }

        foreach (var f in flights)
        {
            if (!IsAlreadyPresent(f.Id, "flight_booking")) entries.Add(f);
        }
        foreach (var h in hotels)
        {
            if (!IsAlreadyPresent(h.Id, "hotel_booking")) entries.Add(h);
        }
        foreach (var v in visas)
        {
            if (!IsAlreadyPresent(v.Id, "visa_application")) entries.Add(v);
        }
        foreach (var hp in holidays)
        {
            if (!IsAlreadyPresent(hp.Id, "holiday_package")) entries.Add(hp);
        }
        foreach (var tr in transports)
        {
            if (!IsAlreadyPresent(tr.Id, "transportation")) entries.Add(tr);
        }
        foreach (var os in others)
        {
            if (!IsAlreadyPresent(os.Id, "other_service")) entries.Add(os);
        }
        foreach (var vf in vfsList)
        {
            if (!IsAlreadyPresent(vf.Id, "vfs_appointment")) entries.Add(vf);
        }
        foreach (var rc in receipts)
        {
            if (!IsAlreadyPresent(rc.Id, "receipt")) entries.Add(rc);
        }

        // Apply serviceType filter if specified
        if (!string.IsNullOrWhiteSpace(serviceType) && !string.Equals(serviceType, "all", StringComparison.OrdinalIgnoreCase))
        {
            entries = entries.Where(e => IsSameServiceType(e.Type, serviceType)).ToList();
        }

        // Apply search term filter if specified
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            entries = entries.Where(e =>
                tokens.All(tok =>
                    (e.Description != null && e.Description.Contains(tok, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Type != null && e.Type.Contains(tok, StringComparison.OrdinalIgnoreCase)) ||
                    (e.PaymentMethod != null && e.PaymentMethod.Contains(tok, StringComparison.OrdinalIgnoreCase)) ||
                    (e.ServiceId.HasValue && e.ServiceId.Value.ToString().Contains(tok)) ||
                    e.Id.ToString().Contains(tok)
                )
            ).ToList();
        }

        bool isDesc = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase) || string.Equals(sort, "newest", StringComparison.OrdinalIgnoreCase);
        var sorted = isDesc
            ? entries.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id).ToList()
            : entries.OrderBy(l => l.CreatedAt).ThenBy(l => l.Id).ToList();
        var total = sorted.Count;
        var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return PagedResponse<HeadLedgerEntryResponse>.Create(items, total, page, pageSize);
    }

    // Global queries for Leads
    public async Task<PagedResponse<LeadSummaryResponse>> GetLeadsAsync(
        int page, int pageSize, string? status, string? search, CancellationToken ct)
    {
        var q = _db.Leads.AsNoTracking().Where(l => l.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(l => l.Name.ToLower().Contains(term) || l.MobileNo.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            q = q.Where(l => l.Status == status);
        }
        q = q.OrderByDescending(l => l.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadSummaryResponse
            {
                Id = l.Id,
                Name = l.Name,
                CountryCode = l.CountryCode,
                MobileNo = l.MobileNo,
                Services = l.Services,
                LeadSource = l.LeadSource,
                Status = l.Status,
                ReminderActive = l.ReminderActive,
                ReminderDate = l.ReminderDate,
                CreatedAt = l.CreatedAt,
                CreatedById = l.CreatedBy,
                CreatedByName = l.Creator != null ? l.Creator.Name : null
            })
            .ToListAsync(ct);

        return PagedResponse<LeadSummaryResponse>.Create(items, total, page, pageSize);
    }


    public async Task<LeadDetailResponse?> GetLeadByIdAsync(
        long id, CancellationToken ct)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.Id == id && l.DeletedAt == null)
            .Select(l => new LeadDetailResponse
            {
                Id = l.Id,
                Name = l.Name,
                CountryCode = l.CountryCode,
                MobileNo = l.MobileNo,
                Services = l.Services,
                LeadSource = l.LeadSource,
                Description = l.Description,
                Status = l.Status,
                ReminderActive = l.ReminderActive,
                ReminderDate = l.ReminderDate,
                CreatedAt = l.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    // Global queries for Receipts
    public async Task<PagedResponse<ReceiptSummaryResponse>> GetReceiptsAsync(
        int page, int pageSize, string? search, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var q = _db.Receipts.AsNoTracking().Include(x => x.Head).Where(x => x.DeletedAt == null);
        if (startDate.HasValue) q = q.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(x => x.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x => (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(term)) ||
                             (x.ReceiptId != null && x.ReceiptId.ToLower().Contains(term)) ||
                             x.Id.ToString().Contains(term) ||
                             (x.AgentId != null && x.AgentId.ToString().Contains(term)) ||
                             (x.Head != null && ((x.Head.FirstName + " " + x.Head.LastName).ToLower().Contains(term))) ||
                             (_db.Heads.Where(h => h.Id == x.AgentId && ((h.FirstName + " " + h.LastName).ToLower().Contains(term))).Any()) ||
                             (x.Creator != null && x.Creator.Name.ToLower().Contains(term)) ||
                             (x.Narration != null && x.Narration.ToLower().Contains(term)) ||
                             (x.Service != null && x.Service.ToLower().Contains(term)) ||
                             (x.PaymentMode != null && x.PaymentMode.ToLower().Contains(term)));
        }
        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReceiptSummaryResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                ReceiptId = x.ReceiptId,
                HeadName = x.Head != null 
                    ? (x.Head.FirstName + " " + x.Head.LastName).Trim() 
                    : (x.AgentId != null ? _db.Heads.Where(h => h.Id == x.AgentId).Select(h => h.FirstName + " " + h.LastName).FirstOrDefault() : null),
                ClientType = x.Head != null 
                    ? x.Head.Type 
                    : (x.AgentId != null ? "agent" : "customer"),
                AgentName = x.Head != null && x.Head.Type == "agent" 
                    ? (x.Head.FirstName + " " + x.Head.LastName).Trim() 
                    : (x.AgentId != null ? _db.Heads.Where(h => h.Id == x.AgentId).Select(h => h.FirstName + " " + h.LastName).FirstOrDefault() : null),
                Service = x.Service,
                Amount = x.Amount,
                PaymentMode = x.PaymentMode,
                Status = x.Status,
                CreatedAt = x.CreatedAt ?? DateTime.UtcNow,
                AgentId = x.AgentId,
                SalesmanName = x.Creator != null ? x.Creator.Name : null,
                BankName = x.BankName,
                PaymentSource = x.BankName ?? x.PaymentMode
            })
            .ToListAsync(ct);

        return PagedResponse<ReceiptSummaryResponse>.Create(items, total, page, pageSize);
    }

    public async Task<ReceiptDetailResponse?> GetReceiptByIdAsync(
        long id, CancellationToken ct)
    {
        return await _db.Receipts.AsNoTracking()
            .Include(x => x.Head)
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new ReceiptDetailResponse
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                ReceiptId = x.ReceiptId,
                HeadName = x.Head != null 
                    ? (x.Head.FirstName + " " + x.Head.LastName).Trim() 
                    : (x.AgentId != null ? _db.Heads.Where(h => h.Id == x.AgentId).Select(h => h.FirstName + " " + h.LastName).FirstOrDefault() : null),
                ClientType = x.Head != null 
                    ? x.Head.Type 
                    : (x.AgentId != null ? "agent" : "customer"),
                AgentName = x.Head != null && x.Head.Type == "agent" 
                    ? (x.Head.FirstName + " " + x.Head.LastName).Trim() 
                    : (x.AgentId != null ? _db.Heads.Where(h => h.Id == x.AgentId).Select(h => h.FirstName + " " + h.LastName).FirstOrDefault() : null),
                Service = x.Service,
                Amount = x.Amount,
                PaymentMode = x.PaymentMode,
                Status = x.Status,
                CreatedAt = x.CreatedAt ?? DateTime.UtcNow,
                Narration = x.Narration,
                HeadId = x.HeadId,
                BookingId = string.IsNullOrEmpty(x.BookingId) ? null : long.Parse(x.BookingId),
                BankId = x.BankId,
                BankName = x.BankName,
                PaymentProof = x.PaymentProof,
                PaymentReference = x.PaymentReference,
                AgentId = x.AgentId
            })
            .FirstOrDefaultAsync(ct);
    }

    // Global Dashboard Summary
    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(CancellationToken ct)
    {
        using var db1 = _dbContextFactory.CreateDbContext();
        using var db2 = _dbContextFactory.CreateDbContext();
        using var db3 = _dbContextFactory.CreateDbContext();
        using var db4 = _dbContextFactory.CreateDbContext();
        using var db5 = _dbContextFactory.CreateDbContext();
        using var db6 = _dbContextFactory.CreateDbContext();
        using var db7 = _dbContextFactory.CreateDbContext();
        using var db8 = _dbContextFactory.CreateDbContext();
        using var db9 = _dbContextFactory.CreateDbContext();
        using var db10 = _dbContextFactory.CreateDbContext();

        var flightTask = db1.FlightBookings
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var hotelTask = db2.HotelBookings
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var visaTask = db3.SalesVisaApplications
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var vfsTask = db4.VfsAppointments.Where(x => x.DeletedAt == null)
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var holidayTask = db5.HolidayPackages.Where(x => x.DeletedAt == null)
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var transportTask = db6.Transportations.Where(x => x.DeletedAt == null)
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var otherTask = db7.OtherServices.Where(x => x.DeletedAt == null)
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Payable = g.Sum(x => (decimal?)x.PayableAmount) ?? 0m,
                Profit = g.Sum(x => (decimal?)x.Profit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var cashTask = db8.CashInHands
            .SumAsync(x => (decimal?)x.Amount, ct);

        var leadsTask = db9.Leads.Where(x => x.DeletedAt == null)
            .CountAsync(ct);

        var headsTask = db10.Heads.Where(x => x.DeletedAt == null)
            .GroupBy(x => 1)
            .Select(g => new {
                Count = g.Count(),
                Receivables = g.Sum(x => x.Wallet < 0 ? -x.Wallet : 0m),
                Wallets = g.Sum(x => x.Wallet > 0 ? x.Wallet : 0m)
            })
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(
            flightTask, hotelTask, visaTask, vfsTask, holidayTask,
            transportTask, otherTask, cashTask, leadsTask, headsTask
        );

        decimal totalAgentReceivables = 0m;
        try
        {
            var agents = await _agentsApiClient.GetAgentsAsync(0, null, 10000, ct);
            if (agents != null && agents.Any())
            {
                using var dbAgentsCheck = _dbContextFactory.CreateDbContext();
                var existingHeadIds = await dbAgentsCheck.Heads.Where(x => x.DeletedAt == null && x.Type == "agent").Select(x => x.Id).ToListAsync(ct);
                totalAgentReceivables = agents
                    .Where(a => a.Wallet < 0 && !existingHeadIds.Contains(a.Id))
                    .Sum(a => -a.Wallet);
            }
        }
        catch
        {
            // Master accounts API offline fallback
        }

        var fl = flightTask.Result;
        var ht = hotelTask.Result;
        var vs = visaTask.Result;
        var vf = vfsTask.Result;
        var hl = holidayTask.Result;
        var tr = transportTask.Result;
        var ot = otherTask.Result;
        var cash = cashTask.Result ?? 0m;
        var leads = leadsTask.Result;
        var hd = headsTask.Result;

        return new DashboardSummaryResponse
        {
            TotalFlightBookings = fl?.Count ?? 0,
            TotalHotelBookings = ht?.Count ?? 0,
            TotalVisaApplications = vs?.Count ?? 0,
            TotalVfsAppointments = vf?.Count ?? 0,
            TotalHolidayPackages = hl?.Count ?? 0,
            TotalTransportations = tr?.Count ?? 0,
            TotalOtherServices = ot?.Count ?? 0,
            TotalPayableAmount = (fl?.Payable ?? 0m) + (ht?.Payable ?? 0m) + (vs?.Payable ?? 0m) + (vf?.Payable ?? 0m) + (hl?.Payable ?? 0m) + (tr?.Payable ?? 0m) + (ot?.Payable ?? 0m),
            TotalProfit = (fl?.Profit ?? 0m) + (ht?.Profit ?? 0m) + (vs?.Profit ?? 0m) + (vf?.Profit ?? 0m) + (hl?.Profit ?? 0m) + (tr?.Profit ?? 0m) + (ot?.Profit ?? 0m),
            CashInHand = cash,
            ActiveLeads = leads,
            TotalHeads = hd?.Count ?? 0,
            TotalReceivables = (hd?.Receivables ?? 0m) + totalAgentReceivables,
            TotalCustomerWallets = hd?.Wallets ?? 0m
        };
    }

    public async Task<TexasPortal.Contracts.Sales.Dashboard.OperationalStatusCountsResponse> GetOperationalStatusCountsAsync(CancellationToken ct)
    {
        using var dbVisa1 = _dbContextFactory.CreateDbContext();
        using var dbVisa2 = _dbContextFactory.CreateDbContext();
        using var dbVfs1  = _dbContextFactory.CreateDbContext();
        using var dbVfs2  = _dbContextFactory.CreateDbContext();
        using var dbHol1  = _dbContextFactory.CreateDbContext();
        using var dbHol2  = _dbContextFactory.CreateDbContext();

        var pendingVisasTask       = dbVisa1.SalesVisaApplications
            .CountAsync(x => string.IsNullOrWhiteSpace(x.ProcessingStatus) || 
                             x.ProcessingStatus.ToLower() == "pending" || 
                             x.ProcessingStatus.ToLower() == "documents_resubmitted" || 
                             x.ProcessingStatus.ToLower() == "document_update_requested", ct);
        var underReviewVisasTask   = dbVisa2.SalesVisaApplications
            .CountAsync(x => x.ProcessingStatus.ToLower() == "under review" || 
                             x.ProcessingStatus.ToLower() == "processing" || 
                             x.ProcessingStatus.ToLower() == "underreview", ct);

        var pendingVfsTask         = dbVfs1.VfsAppointments
            .CountAsync(x => x.DeletedAt == null && (string.IsNullOrWhiteSpace(x.ProcessingStatus) || 
                                                    x.ProcessingStatus.ToLower() == "pending" || 
                                                    x.ProcessingStatus.ToLower() == "documents_resubmitted" || 
                                                    x.ProcessingStatus.ToLower() == "document_update_requested"), ct);
        var underReviewVfsTask     = dbVfs2.VfsAppointments
            .CountAsync(x => x.DeletedAt == null && (x.ProcessingStatus.ToLower() == "under review" || 
                                                    x.ProcessingStatus.ToLower() == "processing" || 
                                                    x.ProcessingStatus.ToLower() == "underreview"), ct);

        var pendingHolidaysTask    = dbHol1.HolidayPackages
            .CountAsync(x => x.DeletedAt == null && (string.IsNullOrWhiteSpace(x.ProcessingStatus) || 
                                                    x.ProcessingStatus.ToLower() == "pending" || 
                                                    x.ProcessingStatus.ToLower() == "documents_resubmitted" || 
                                                    x.ProcessingStatus.ToLower() == "document_update_requested"), ct);
        var underReviewHolidaysTask = dbHol2.HolidayPackages
            .CountAsync(x => x.DeletedAt == null && (x.ProcessingStatus.ToLower() == "under review" || 
                                                    x.ProcessingStatus.ToLower() == "processing" || 
                                                    x.ProcessingStatus.ToLower() == "underreview"), ct);

        await Task.WhenAll(
            pendingVisasTask, underReviewVisasTask,
            pendingVfsTask,   underReviewVfsTask,
            pendingHolidaysTask, underReviewHolidaysTask);

        return new TexasPortal.Contracts.Sales.Dashboard.OperationalStatusCountsResponse(
            PendingVisasCount:       pendingVisasTask.Result,
            UnderReviewVisasCount:   underReviewVisasTask.Result,
            PendingVfsCount:         pendingVfsTask.Result,
            UnderReviewVfsCount:     underReviewVfsTask.Result,
            PendingHolidaysCount:    pendingHolidaysTask.Result,
            UnderReviewHolidaysCount: underReviewHolidaysTask.Result
        );
    }

    public async Task<PagedResponse<TexasPortal.Contracts.Sales.Disputes.DisputeResponse>> GetDisputesAsync(
        int page, int pageSize, string? status, string? search, string? sort, CancellationToken ct)
    {
        var q = _db.Disputes.AsNoTracking()
            .Include(x => x.Head)
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var statusLower = status.Trim().ToLowerInvariant();
            if (statusLower == "pending" || statusLower == "open")
            {
                q = q.Where(x => x.Status.ToLower() == "open" || x.Status.ToLower() == "pending");
            }
            else
            {
                q = q.Where(x => x.Status.ToLower() == statusLower);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x =>
                x.Id.ToString().Contains(term) ||
                x.DisputableId.ToString().Contains(term) ||
                x.Subject.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term) ||
                x.DisputableType.ToLower().Contains(term) ||
                (x.User != null && x.User.Name.ToLower().Contains(term)) ||
                (x.Head != null && ((x.Head.FirstName ?? "") + " " + (x.Head.LastName ?? "")).ToLower().Contains(term))
            );
        }

        var sortOption = (sort ?? "newest").Trim().ToLowerInvariant();
        q = sortOption switch
        {
            "oldest" => q.OrderBy(x => x.CreatedAt),
            "highest_refund" => q.OrderByDescending(x => x.RefundAmount ?? 0m),
            "lowest_refund" => q.OrderBy(x => x.RefundAmount ?? 0m),
            "pending_first" => q.OrderBy(x => x.Status == "open" || x.Status == "pending" ? 0 : 1).ThenByDescending(x => x.CreatedAt),
            _ => q.OrderByDescending(x => x.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var rawItems = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Dispute = x,
                HeadName = x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : null,
                SalesmanName = x.User != null ? x.User.Name : null,
                HeadWallet = x.Head != null ? (decimal?)x.Head.Wallet : null
            })
            .ToListAsync(ct);

        var flightIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("flight")).Select(i => i.Dispute.DisputableId).ToList();
        var hotelIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("hotel")).Select(i => i.Dispute.DisputableId).ToList();
        var visaIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("visa")).Select(i => i.Dispute.DisputableId).ToList();
        var vfsIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("vfs")).Select(i => i.Dispute.DisputableId).ToList();
        var holidayIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("holiday")).Select(i => i.Dispute.DisputableId).ToList();
        var transportIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("transport")).Select(i => i.Dispute.DisputableId).ToList();
        var otherIds = rawItems.Where(i => (i.Dispute.DisputableType ?? "").ToLower().Contains("other")).Select(i => i.Dispute.DisputableId).ToList();

        var flightDocs = await _db.FlightBookings.AsNoTracking().Where(b => flightIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);
        var hotelDocs = await _db.HotelBookings.AsNoTracking().Where(b => hotelIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);
        var visaDocs = await _db.SalesVisaApplications.AsNoTracking().Where(b => visaIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);
        var vfsDocs = await _db.VfsAppointments.AsNoTracking().Where(b => vfsIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);
        var holidayDocs = await _db.HolidayPackages.AsNoTracking().Where(b => holidayIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);
        var transportDocs = await _db.Transportations.AsNoTracking().Where(b => transportIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);
        var otherDocs = await _db.OtherServices.AsNoTracking().Where(b => otherIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.DocumentNumber, ct);

        var items = rawItems.Select(i =>
        {
            var x = i.Dispute;
            var docNum = !string.IsNullOrWhiteSpace(x.DocumentNumber) ? x.DocumentNumber : $"TXSDISP-{x.Id:D6}";
            var dt = (x.DisputableType ?? "").ToLower();
            string? dispDocNo = null;
            if (dt.Contains("flight")) flightDocs.TryGetValue(x.DisputableId, out dispDocNo);
            else if (dt.Contains("hotel")) hotelDocs.TryGetValue(x.DisputableId, out dispDocNo);
            else if (dt.Contains("visa")) visaDocs.TryGetValue(x.DisputableId, out dispDocNo);
            else if (dt.Contains("vfs")) vfsDocs.TryGetValue(x.DisputableId, out dispDocNo);
            else if (dt.Contains("holiday")) holidayDocs.TryGetValue(x.DisputableId, out dispDocNo);
            else if (dt.Contains("transport")) transportDocs.TryGetValue(x.DisputableId, out dispDocNo);
            else if (dt.Contains("other")) otherDocs.TryGetValue(x.DisputableId, out dispDocNo);

            return new TexasPortal.Contracts.Sales.Disputes.DisputeResponse(
                x.Id,
                docNum,
                x.UserId,
                x.HeadId,
                i.HeadName,
                x.DisputableId,
                x.DisputableType,
                x.Subject,
                x.Description,
                x.Status,
                x.Resolution,
                x.ResolutionNote,
                x.ResolvedBy,
                x.ResolvedAt,
                x.IsRefund,
                x.RefundAmount,
                x.ApprovedRefundAmount,
                x.RefundProcessed,
                x.RefundStatus,
                x.RefundDate,
                x.CreatedAt,
                i.SalesmanName,
                null,
                null,
                i.HeadWallet,
                dispDocNo,
                x.Iban,
                x.Name
            );
        }).ToList();

        return PagedResponse<TexasPortal.Contracts.Sales.Disputes.DisputeResponse>.Create(items, total, page, pageSize);
    }

    public async Task<TexasPortal.Contracts.Sales.Disputes.DisputeResponse?> GetDisputeByIdAsync(long id, CancellationToken ct)
    {
        var x = await _db.Disputes.AsNoTracking()
            .Include(d => d.Head)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (x is null) return null;

        decimal? bookingPayable = null;
        decimal? bookingSupplierCost = null;
        string? disputableDocNo = null;

        try
        {
            var dt = (x.DisputableType ?? "").ToLowerInvariant();
            if (dt.Contains("flight"))
            {
                var f = await _db.FlightBookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (f != null) { bookingPayable = f.PayableAmount; disputableDocNo = f.DocumentNumber; }
            }
            else if (dt.Contains("hotel"))
            {
                var h = await _db.HotelBookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (h != null) { bookingPayable = h.PayableAmount; bookingSupplierCost = h.ExpectedSupplierCost; disputableDocNo = h.DocumentNumber; }
            }
            else if (dt.Contains("visa"))
            {
                var v = await _db.SalesVisaApplications.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (v != null) { bookingPayable = v.PayableAmount; bookingSupplierCost = v.ExpectedSupplierCost; disputableDocNo = v.DocumentNumber; }
            }
            else if (dt.Contains("vfs"))
            {
                var vf = await _db.VfsAppointments.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (vf != null) { bookingPayable = vf.PayableAmount; bookingSupplierCost = vf.ExpectedSupplierCost; disputableDocNo = vf.DocumentNumber; }
            }
            else if (dt.Contains("holiday"))
            {
                var hol = await _db.HolidayPackages.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (hol != null) { bookingPayable = hol.PayableAmount; bookingSupplierCost = hol.ExpectedSupplierCost; disputableDocNo = hol.DocumentNumber; }
            }
            else if (dt.Contains("transport"))
            {
                var tr = await _db.Transportations.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (tr != null) { bookingPayable = tr.PayableAmount; bookingSupplierCost = tr.ExpectedSupplierCost; disputableDocNo = tr.DocumentNumber; }
            }
            else if (dt.Contains("other"))
            {
                var ot = await _db.OtherServices.AsNoTracking().FirstOrDefaultAsync(b => b.Id == x.DisputableId, ct);
                if (ot != null) { bookingPayable = ot.PayableAmount; bookingSupplierCost = ot.ExpectedSupplierCost; disputableDocNo = ot.DocumentNumber; }
            }
        }
        catch { }

        return new TexasPortal.Contracts.Sales.Disputes.DisputeResponse(
            x.Id,
            !string.IsNullOrWhiteSpace(x.DocumentNumber) ? x.DocumentNumber : $"TXSDISP-{x.Id:D6}",
            x.UserId,
            x.HeadId,
            x.Head != null ? (x.Head.FirstName + " " + x.Head.LastName).Trim() : null,
            x.DisputableId,
            x.DisputableType,
            x.Subject,
            x.Description,
            x.Status,
            x.Resolution,
            x.ResolutionNote,
            x.ResolvedBy,
            x.ResolvedAt,
            x.IsRefund,
            x.RefundAmount,
            x.ApprovedRefundAmount,
            x.RefundProcessed,
            x.RefundStatus,
            x.RefundDate,
            x.CreatedAt,
            x.User != null ? x.User.Name : null,
            bookingPayable,
            bookingSupplierCost,
            x.Head != null ? x.Head.Wallet : null,
            disputableDocNo,
            x.Iban,
            x.Name);
    }

    public async Task<TexasPortal.Contracts.Sales.Disputes.DisputesDashboardResponse> GetDisputesDashboardAsync(CancellationToken ct)
    {
        var disputes = await _db.Disputes.AsNoTracking().ToListAsync(ct);

        var total = disputes.Count;
        var pending = disputes.Count(d => string.Equals(d.Status, "open", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Status, "pending", StringComparison.OrdinalIgnoreCase));
        var resolved = disputes.Count(d => string.Equals(d.Status, "resolved", StringComparison.OrdinalIgnoreCase));
        var rejected = disputes.Count(d => string.Equals(d.Status, "rejected", StringComparison.OrdinalIgnoreCase));

        var requestedRefundAmount = disputes.Where(d => d.IsRefund && d.RefundAmount.HasValue).Sum(d => d.RefundAmount!.Value);
        var approvedRefundAmount = disputes.Where(d => d.IsRefund && d.ApprovedRefundAmount.HasValue).Sum(d => d.ApprovedRefundAmount!.Value);

        return new TexasPortal.Contracts.Sales.Disputes.DisputesDashboardResponse(
            total,
            pending,
            resolved,
            rejected,
            requestedRefundAmount,
            approvedRefundAmount
        );
    }

    public async Task<PagedResponse<TexasPortal.Contracts.Sales.Quotations.QuotationSummaryResponse>> GetQuotationsAsync(
        int page, int pageSize, string? searchTerm = null, string? clientType = null, long? salesmanId = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null, CancellationToken ct = default)
    {
        var q = _db.Quotations
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .AsQueryable();

        if (salesmanId.HasValue && salesmanId.Value > 0)
        {
            q = q.Where(x => x.UserId == salesmanId.Value);
        }

        if (!string.IsNullOrWhiteSpace(clientType) && !string.Equals(clientType, "all", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.ClientType == clientType.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.Status == status.Trim().ToLower());
        }

        if (startDate.HasValue)
        {
            q = q.Where(x => x.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            q = q.Where(x => x.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            q = q.Where(x =>
                EF.Functions.Like(x.DocumentNumber, $"%{term}%") ||
                EF.Functions.Like(x.CustomerName, $"%{term}%") ||
                (x.CustomerContact != null && EF.Functions.Like(x.CustomerContact, $"%{term}%")) ||
                (x.CustomerEmail != null && EF.Functions.Like(x.CustomerEmail, $"%{term}%")) ||
                (x.User != null && EF.Functions.Like(x.User.Name, $"%{term}%")));
        }

        var total = await q.CountAsync(ct);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 1000);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => new TexasPortal.Contracts.Sales.Quotations.QuotationSummaryResponse(
                x.Id,
                x.DocumentNumber,
                x.UserId,
                x.User != null ? x.User.Name : "Sales Representative",
                x.HeadId,
                x.ClientType,
                x.CustomerName,
                x.CustomerContact,
                x.CustomerEmail,
                x.Items.Count,
                x.TotalAmount,
                x.Status,
                x.CreatedAt
            ))
            .ToListAsync(ct);

        return PagedResponse<TexasPortal.Contracts.Sales.Quotations.QuotationSummaryResponse>.Create(
            items,
            total,
            p,
            ps
        );
    }

    public async Task<TexasPortal.Contracts.Sales.Quotations.QuotationDetailResponse?> GetQuotationByIdAsync(
        string idOrDoc, 
        CancellationToken ct = default)
    {
        var q = _db.Quotations
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .AsQueryable();

        if (long.TryParse(idOrDoc, out var id))
        {
            q = q.Where(x => x.Id == id || x.DocumentNumber == idOrDoc);
        }
        else
        {
            q = q.Where(x => x.DocumentNumber == idOrDoc);
        }

        var entity = await q.FirstOrDefaultAsync(ct);
        if (entity == null) return null;

        return new TexasPortal.Contracts.Sales.Quotations.QuotationDetailResponse(
            entity.Id,
            entity.DocumentNumber,
            entity.UserId,
            entity.User != null ? entity.User.Name : "Sales Representative",
            entity.HeadId,
            entity.ClientType,
            entity.CustomerName,
            entity.CustomerContact,
            entity.CustomerEmail,
            entity.TotalAmount,
            entity.Status,
            entity.Remarks,
            entity.CreatedAt,
            entity.Items.OrderBy(i => i.Id).Select(i => new TexasPortal.Contracts.Sales.Quotations.QuotationItemDto(
                i.Id,
                i.ServiceType,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.TotalAmount
            )).ToList()
        );
    }

    
}