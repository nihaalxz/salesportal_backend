using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using System;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Receipts;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Dashboard;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Contracts.Sales.Receipts;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Route("api/internal/operations")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "OperationsServiceAccess")]
[Produces("application/json")]
public sealed class InternalOperationsController : ControllerBase
{
    private readonly IOperationsReadService _readService;
    private readonly IOperationsWriteService _writeService;
    private readonly ISalesPortalDbContext _db;

    public InternalOperationsController(IOperationsReadService readService, IOperationsWriteService writeService, ISalesPortalDbContext db)
    {
        _readService = readService;
        _writeService = writeService;
        _db = db;
    }



    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken ct)
    {
        var agentsApiClient = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Queries.IAgentsApiClient>();
        if (agentsApiClient != null)
        {
            var suppliers = await agentsApiClient.GetSuppliersAsync(ct);
            return Ok(suppliers);
        }
        return Ok(new List<object>());
    }

    // â”€â”€â”€ Sales Visa Applications â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("visas")]
    [ProducesResponseType(typeof(PagedResponse<VisaApplicationSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetVisas(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");

        var result = await _readService.GetVisaApplicationsAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("visas/{id:long}")]
    [ProducesResponseType(typeof(VisaApplicationDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetVisaDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetVisaApplicationByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ VFS Appointments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("vfs-appointments")]
    [ProducesResponseType(typeof(PagedResponse<VfsAppointmentSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetVfsAppointments(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");

        var result = await _readService.GetVfsAppointmentsAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("vfs-appointments/{id:long}")]
    [ProducesResponseType(typeof(VfsAppointmentDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetVfsAppointmentDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetVfsAppointmentByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Holiday Packages â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("holiday-packages")]
    [ProducesResponseType(typeof(PagedResponse<HolidayPackageSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetHolidayPackages(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");

        var result = await _readService.GetHolidayPackagesAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("holiday-packages/{id:long}")]
    [ProducesResponseType(typeof(HolidayPackageDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetHolidayPackageDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetHolidayPackageByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Flights â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("flights")]
    [ProducesResponseType(typeof(PagedResponse<FlightBookingSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetFlights(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetFlightBookingsAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("flights/{id:long}")]
    [ProducesResponseType(typeof(FlightBookingDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetFlightDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetFlightBookingByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Hotels â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("hotels")]
    [ProducesResponseType(typeof(PagedResponse<HotelBookingSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetHotels(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetHotelBookingsAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("hotels/{id:long}")]
    [ProducesResponseType(typeof(HotelBookingDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetHotelDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetHotelBookingByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Transportations â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("transportations")]
    [ProducesResponseType(typeof(PagedResponse<TransportationSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetTransportations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetTransportationsAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("transportations/{id:long}")]
    [ProducesResponseType(typeof(TransportationDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetTransportationDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetTransportationByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Other Services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("other-services")]
    [ProducesResponseType(typeof(PagedResponse<OtherServiceSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetOtherServices(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetOtherServicesAsync(page, pageSize, status, search, startDate, endDate, ct: ct);
        return Ok(result);
    }

    [HttpGet("other-services/{id:long}")]
    [ProducesResponseType(typeof(OtherServiceDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetOtherServiceDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetOtherServiceByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Heads â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("heads")]
    [ProducesResponseType(typeof(PagedResponse<HeadSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetHeads(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 5000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetHeadsAsync(page, pageSize, search, type, ct);
        return Ok(result);
    }

    [HttpGet("heads/{id:long}")]
    [ProducesResponseType(typeof(HeadDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetHeadDetails([FromRoute] long id, [FromQuery] string? type = null, CancellationToken ct = default)
    {
        var result = await _readService.GetHeadByIdAsync(id, type, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("heads/{id:long}/ledger")]
    [ProducesResponseType(typeof(PagedResponse<HeadLedgerEntryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetHeadLedger(
        [FromRoute] long id,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] System.DateTime? startDate = null,
        [FromQuery] System.DateTime? endDate = null,
        [FromQuery] string? serviceType = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 10000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetHeadLedgerAsync(id, page, pageSize, type, startDate, endDate, serviceType, search, sort, ct);
        return Ok(result);
    }

    // â”€â”€â”€ Leads â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("leads")]
    [ProducesResponseType(typeof(PagedResponse<LeadSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetLeads(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetLeadsAsync(page, pageSize, status, search, ct);
        return Ok(result);
    }

    [HttpGet("leads/{id:long}")]
    [ProducesResponseType(typeof(LeadDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetLeadDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetLeadByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Receipts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("receipts")]
    [ProducesResponseType(typeof(PagedResponse<ReceiptSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetReceipts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");
        var result = await _readService.GetReceiptsAsync(page, pageSize, search, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("receipts/{id:long}")]
    [ProducesResponseType(typeof(ReceiptDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetReceiptDetails([FromRoute] long id, CancellationToken ct)
    {
        var result = await _readService.GetReceiptByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // â”€â”€â”€ Dashboard Summary â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("dashboard/summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var result = await _readService.GetDashboardSummaryAsync(ct);
        return Ok(result);
    }

    [HttpGet("dashboard/operational-counts")]
    [ProducesResponseType(typeof(TexasPortal.Contracts.Sales.Dashboard.OperationalStatusCountsResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetOperationalStatusCounts(CancellationToken ct)
    {
        var result = await _readService.GetOperationalStatusCountsAsync(ct);
        return Ok(result);
    }
    // â”€â”€â”€ Salesmen â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("salesmen")]
    [ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<TexasPortal.Contracts.Sales.Salesmen.SalesmanResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSalesmen(CancellationToken ct)
    {
        var result = await _readService.GetSalesmenAsync(ct);
        return Ok(result);
    }

    [HttpGet("salesmen/report")]
    [ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<TexasPortal.Contracts.Sales.Salesmen.SalesmanReportResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSalesmenReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var result = await _readService.GetSalesmenReportAsync(startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("salesmen/{salesmanId:long}/service-entries")]
    [ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<TexasPortal.Contracts.Sales.Salesmen.SalesmanServiceEntryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSalesmanServiceEntries(
        [FromRoute] long salesmanId,
        [FromQuery] string? salesmanName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (salesmanId <= 0 && !string.IsNullOrWhiteSpace(salesmanName))
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Name.ToLower() == salesmanName.ToLower(), ct);
            if (u != null) salesmanId = u.Id;
        }
        var result = await _readService.GetSalesmanServiceEntriesAsync(salesmanId, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("salesmen/{salesmanId:long}/profit-records")]
    public async Task<IActionResult> GetSalesmanProfitRecords(
        [FromRoute] long salesmanId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var profitService = HttpContext.RequestServices.GetRequiredService<TexasPortal.Application.Sales.Profit.ISalesmanProfitService>();
        var records = await profitService.GetSalesmanProfitItemDtosAsync(salesmanId, startDate, endDate, ct);
        return Ok(records);
    }

    public record UpdateStatusRequest(string Status, decimal? Cost, string? PaymentReference = null);

    public record SyncActualCostRequest(
        string? Service_Type,
        string? ServiceType,
        string? Service_Id,
        string? ServiceId,
        decimal Actual_Cost,
        decimal ActualCost,
        decimal Sales_Returns,
        decimal SalesReturns
    );

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/flight-bookings/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateFlightPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.FlightBookings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.UserId, "flight_booking", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.FromLocation} -> {booking.ToLocation}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("Flight", id.ToString(), ct);
        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/hotel-bookings/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateHotelPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.HotelBookings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.UserId, "hotel_booking", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.GuestName} - {booking.Country}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("Hotel", id.ToString(), ct);
        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/holiday-packages/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateHolidayPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.CreatedBy, "holiday_package", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.PassengerName} - {booking.Country}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("Holiday", id.ToString(), ct);
        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/vfs-appointments/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateVfsPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.CreatedBy, "vfs_appointment", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.Name} - {booking.Country}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("VFS", id.ToString(), ct);
        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/transportations/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateTransportationPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.Transportations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.CreatedBy, "transportation", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.Name} - {booking.Vehicle}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("Transport", id.ToString(), ct);
        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/other-services/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateOtherServicePaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.OtherServices.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.CreatedBy, "other_service", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.Name} - {booking.ServiceName}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("Other", id.ToString(), ct);
        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("/sales-visa/applications/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateVisaPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();
        
        var oldStatus = booking.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(request.Status)) booking.PaymentStatus = request.Status;
        if (request.Cost.HasValue && request.Cost.Value > 0)
        {
            booking.ExpectedSupplierCost = request.Cost.Value;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            if (booking.SupplierId.HasValue && booking.SupplierId.Value > 0) booking.IsInhouse = false;
        }
        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, booking.PaymentStatus, StringComparison.OrdinalIgnoreCase))
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, booking.UserId, "visa_application", id, booking.PaymentStatus, booking.PayableAmount,
                $"{booking.VisaPackName}", ct, booking.DocumentNumber);
        }

        await FinalizeProfitHelperAsync("Visa", id.ToString(), ct);
        return Ok();
    }

    [HttpPost("receipts/{id:long}/payment-status")]
    public async Task<IActionResult> UpdateReceiptPaymentStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var receipt = await _db.Receipts
            .Include(x => x.Head)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (receipt == null) return NotFound();

        var oldStatus = receipt.Status;
        var targetStatus = (request.Status ?? "").Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            receipt.PaymentReference = request.PaymentReference.Trim();
        }

        if (string.Equals(oldStatus, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(request.PaymentReference))
            {
                await _db.SaveChangesAsync(ct);
            }
            return Ok();
        }

        // 1. Approval path: delegate directly to the single authoritative orchestrator
        if (targetStatus == "success" || targetStatus == "approved")
        {
            var orchestrator = HttpContext.RequestServices.GetRequiredService<TexasPortal.Application.Sales.Receipts.Approval.ReceiptApprovalOrchestrator>();
            var currentUser = HttpContext.RequestServices.GetRequiredService<TexasPortal.Application.Common.Interfaces.ICurrentUser>();
            var accountantUserId = currentUser.UserId ?? 1;

            var approvalResult = await orchestrator.HandleAsync(
                new TexasPortal.Application.Sales.Receipts.Approval.ReceiptApprovalCommand(id, accountantUserId), ct);

            if (!approvalResult.IsSuccess && approvalResult.StatusCode != 200)
            {
                return StatusCode(approvalResult.StatusCode, new { Message = approvalResult.Message });
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentReference))
            {
                receipt.PaymentReference = request.PaymentReference.Trim();
                await _db.SaveChangesAsync(ct);
            }

            return Ok();
        }

        // 2. Reversal path: transitioning from success to non-success
        if (string.Equals(oldStatus, "success", StringComparison.OrdinalIgnoreCase)
            && targetStatus != "success" && targetStatus != "approved"
            && receipt.HeadId.HasValue
            && receipt.Head != null)
        {
            // Reverse wallet balance ONLY for wallet deposits
            if (receipt.Service.Equals("wallet_deposit", StringComparison.OrdinalIgnoreCase))
            {
                receipt.Head.Wallet -= receipt.Amount;
            }

            // Reversing ledger entry: Debit (reversal of credit)
            _db.HeadLedgers.Add(new HeadLedger
            {
                HeadId = receipt.HeadId.Value,
                ServiceType = receipt.Service,
                ServiceId = receipt.Id,
                Description = $"Payment reversed/cancelled via Receipt {receipt.ReceiptId}: {receipt.Narration}",
                Debit = receipt.Amount,
                Credit = 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        receipt.Status = request.Status;
        receipt.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, request.Status, StringComparison.OrdinalIgnoreCase) && receipt.CreatedBy.HasValue)
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, receipt.CreatedBy.Value, "receipt", id, request.Status, receipt.Amount,
                $"{receipt.Narration}", ct, receipt.DocumentNumber ?? receipt.ReceiptId);
        }

        return Ok();
    }

    [HttpPost("/services/sync-actual-cost")]
    public async Task<IActionResult> SyncActualCost([FromBody] SyncActualCostRequest request, CancellationToken ct)
    {
        var serviceId = !string.IsNullOrWhiteSpace(request.Service_Id) ? request.Service_Id : request.ServiceId ?? string.Empty;
        var serviceType = !string.IsNullOrWhiteSpace(request.Service_Type) ? request.Service_Type : request.ServiceType ?? string.Empty;
        var actualCost = request.Actual_Cost != 0 ? request.Actual_Cost : request.ActualCost;
        var salesReturns = request.Sales_Returns != 0 ? request.Sales_Returns : request.SalesReturns;

        if (string.IsNullOrWhiteSpace(serviceId))
        {
            return BadRequest("Invalid service_id.");
        }

        long.TryParse(serviceId, out var parsedId);
        var serviceTypeLower = serviceType.ToLowerInvariant();

        if (serviceTypeLower.Contains("visa"))
        {
            var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (serviceTypeLower.Contains("vfs"))
        {
            var booking = await _db.VfsAppointments.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (serviceTypeLower.Contains("holiday"))
        {
            var booking = await _db.HolidayPackages.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (serviceTypeLower.Contains("flight"))
        {
            var booking = await _db.FlightBookings.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (serviceTypeLower.Contains("hotel"))
        {
            var booking = await _db.HotelBookings.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (serviceTypeLower.Contains("transport"))
        {
            var booking = await _db.Transportations.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (serviceTypeLower.Contains("other"))
        {
            var booking = await _db.OtherServices.FirstOrDefaultAsync(
                x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == serviceId || (serviceId != null && serviceId.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(serviceId)), ct);
            if (booking != null)
            {
                booking.ExpectedSupplierCost = actualCost;
                var netSelling = salesReturns > 0 ? Math.Max(0, booking.PayableAmount - salesReturns) : booking.PayableAmount;
                booking.Profit = netSelling - actualCost;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        await FinalizeProfitHelperAsync(serviceType, serviceId, ct);
        return Ok();
    }

    // â”€â”€â”€ Visa Processing Actions â”€â”€â”€

    [HttpPost("sales-visa/applicants/{firstApplicantId:long}/update-visa-id")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateVisaId(long firstApplicantId, [FromForm] ProcessVisaRequest request, CancellationToken ct)
    {
        var applicant = await _db.SalesVisaApplicants.FirstOrDefaultAsync(x => x.Id == firstApplicantId, ct);
        if (applicant == null) return NotFound("Applicant not found.");

        var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == applicant.SalesVisaApplicationId, ct);
        if (booking == null) return NotFound("Application not found.");

        if (!string.IsNullOrWhiteSpace(request.VisaAppId))
        {
            var trimmedRef = request.VisaAppId.Trim();
            var exists = await _db.SalesVisaApplicants
                .AnyAsync(x => x.SalesVisaApplicationId != booking.Id && x.GovtFeeReference == trimmedRef, ct);
            if (exists)
            {
                return BadRequest($"Government Application / Ref No '{trimmedRef}' is already used by another visa application.");
            }
        }

        var allApplicants = await _db.SalesVisaApplicants
            .Where(x => x.SalesVisaApplicationId == booking.Id)
            .ToListAsync(ct);

        foreach (var app in allApplicants)
        {
            app.VisaAppId = request.VisaAppId;
            app.GovtFeeReference = request.VisaAppId;
            app.ProcessedBy = request.ProcessedBy;
            app.UpdatedAt = DateTime.UtcNow;
        }

        var isAlreadyApproved = string.Equals(booking.ProcessingStatus, "approved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(booking.ApprovalNote)
            || !string.IsNullOrWhiteSpace(booking.VisaDocument);

        if (isAlreadyApproved)
        {
            booking.ProcessingStatus = "approved";
        }
        else
        {
            booking.ProcessingStatus = "under review";
        }
        booking.UpdatedAt = DateTime.UtcNow;

        var agentsApiClient = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Queries.IAgentsApiClient>();
        var govtFees = GetEffectiveGovtFees(request.GovtFees);

        var existingVisaFees = await _db.SalesVisaGovtFees
            .Where(g => g.SalesVisaApplicantId == firstApplicantId)
            .OrderBy(g => g.Id)
            .ToListAsync(ct);

        var updatedRowKeys = new HashSet<string>();

        foreach (var fee in govtFees)
        {
            var path = await SaveFileAsync(fee.PaymentProof, "govt_fee_proofs", ct);
            SalesVisaGovtFee? existingRow = null;

            if (!string.IsNullOrWhiteSpace(fee.RowKey))
            {
                existingRow = existingVisaFees.FirstOrDefault(e => e.RowKey == fee.RowKey);
            }
            if (existingRow == null && !string.IsNullOrWhiteSpace(fee.TransactionNo))
            {
                existingRow = existingVisaFees.FirstOrDefault(e => !string.IsNullOrEmpty(e.PaymentReference) && string.Equals(e.PaymentReference, fee.TransactionNo, StringComparison.OrdinalIgnoreCase));
            }

            if (existingRow != null)
            {
                existingRow.Amount = fee.Amount;
                if (fee.SupplierId.HasValue && fee.SupplierId.Value > 0) existingRow.SupplierId = fee.SupplierId.Value;
                if (!string.IsNullOrWhiteSpace(fee.SupplierName)) existingRow.SupplierName = fee.SupplierName;
                if (!string.IsNullOrWhiteSpace(fee.GovtFeeDescription)) existingRow.Description = fee.GovtFeeDescription;
                if (!string.IsNullOrWhiteSpace(fee.TransactionNo)) existingRow.PaymentReference = fee.TransactionNo;
                if (path != null) existingRow.PaymentProof = path;
                else if (!string.IsNullOrWhiteSpace(fee.ExistingPaymentProof)) existingRow.PaymentProof = fee.ExistingPaymentProof;
                existingRow.UpdatedAt = DateTime.UtcNow;
                updatedRowKeys.Add(existingRow.RowKey);
                fee.RowKey = existingRow.RowKey;
            }
            else
            {
                var newRowKey = string.IsNullOrWhiteSpace(fee.RowKey) ? Guid.NewGuid().ToString("D") : fee.RowKey;
                var newFee = new SalesVisaGovtFee
                {
                    RowKey = newRowKey,
                    SalesVisaApplicantId = firstApplicantId,
                    SupplierId = fee.SupplierId,
                    SupplierName = fee.SupplierName,
                    Amount = fee.Amount,
                    Description = fee.GovtFeeDescription,
                    PaymentReference = fee.TransactionNo,
                    Status = "recorded",
                    PaymentProof = path ?? fee.ExistingPaymentProof,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.SalesVisaGovtFees.Add(newFee);
                updatedRowKeys.Add(newRowKey);
                fee.RowKey = newRowKey;
            }
        }

        // Delete unlinked recorded rows explicitly removed by user
        var unlinkedRemoved = existingVisaFees.Where(e => !updatedRowKeys.Contains(e.RowKey) && e.AccountsPaymentId == null && e.Status == "recorded").ToList();
        if (unlinkedRemoved.Any())
        {
            _db.SalesVisaGovtFees.RemoveRange(unlinkedRemoved);
        }

        await _db.SaveChangesAsync(ct);

        // Calculate authoritative total cost
        var currentFees = await _db.SalesVisaGovtFees
            .Where(g => g.SalesVisaApplicantId == firstApplicantId)
            .ToListAsync(ct);

        var totalGovtCost = currentFees.Sum(f => f.Amount);
        if (totalGovtCost > 0)
        {
            booking.ExpectedSupplierCost = totalGovtCost;
            booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
            foreach (var app in allApplicants)
            {
                app.ExpectedSupplierCost = totalGovtCost / Math.Max(1, allApplicants.Count);
                app.Profit = app.PayableAmount - app.ExpectedSupplierCost;
            }
        }

        if (agentsApiClient != null)
        {
            var syncFees = currentFees.Select(f => new TexasPortal.Application.Sales.Queries.GovtFeeSyncItem(f.Amount, f.SupplierId, f.SupplierName, f.PaymentReference, f.Description, f.RowKey)).ToList();
            await agentsApiClient.SyncGovtSupplierPaymentsAsync(booking.Id.ToString(), "Visa", request.ProcessedBy, syncFees, ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("sales-visa/applicants/{firstApplicantId:long}/edit-visa-id")]
    public async Task<IActionResult> EditVisaId(long firstApplicantId, [FromBody] EditGovRefRequest request, CancellationToken ct)
    {
        var applicant = await _db.SalesVisaApplicants.FirstOrDefaultAsync(x => x.Id == firstApplicantId, ct);
        if (applicant == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.VisaAppId))
        {
            var trimmedRef = request.VisaAppId.Trim();
            var exists = await _db.SalesVisaApplicants
                .AnyAsync(x => x.SalesVisaApplicationId != applicant.SalesVisaApplicationId && x.GovtFeeReference == trimmedRef, ct);
            if (exists)
            {
                return BadRequest($"Government Application / Ref No '{trimmedRef}' is already used by another visa application.");
            }
        }

        var allApplicants = await _db.SalesVisaApplicants
            .Where(x => x.SalesVisaApplicationId == applicant.SalesVisaApplicationId)
            .ToListAsync(ct);

        foreach (var app in allApplicants)
        {
            app.VisaAppId = request.VisaAppId;
            app.GovtFeeReference = request.VisaAppId;
            app.ProcessedBy = request.ProcessedBy;
            app.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("sales-visa/applications/{id:long}/approve")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ApproveVisa(long id, [FromForm] ApproveVisaRequest request, CancellationToken ct)
    {
        var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        var path = await SaveFileAsync(request.VisaDocument, "visa_documents", ct);

        booking.ProcessingStatus = "approved";
        booking.VisaDocument = path;
        booking.ApprovalNote = request.DescNote;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.UserId, "visa_application", id, "approved", booking.PayableAmount,
            $"{booking.VisaPackName}", ct, booking.DocumentNumber);

        return Ok();
    }

    [HttpPost("sales-visa/applications/{id:long}/reject")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RejectVisa(long id, [FromForm] RejectVisaRequest request, CancellationToken ct)
    {
        ApplicationFile? rejectionDoc = null;
        if (request.RejectionDocument != null)
        {
            var stream = request.RejectionDocument.OpenReadStream();
            rejectionDoc = new ApplicationFile
            {
                FileName = request.RejectionDocument.FileName,
                ContentType = request.RejectionDocument.ContentType,
                Stream = stream
            };
        }

        var result = await _writeService.RejectVisaAsync(
            id, rejectionDoc, request.DescNote, request.RefundNote, request.RejectionFee, request.PayableAmount, request.ProcessedBy, ct);

        if (!result.Success)
        {
            return StatusCode(500, result.ErrorMessage ?? "Failed to reject visa.");
        }

        return Ok(result);
    }

    [HttpPost("sales-visa/applications/{id:long}/request-document-update")]
    public async Task<IActionResult> RequestVisaDocumentUpdate(long id, [FromBody] RequestDocUpdateRequest request, CancellationToken ct)
    {
        var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound("Visa application not found.");

        var note = !string.IsNullOrWhiteSpace(request?.RequestNote) ? request.RequestNote : request?.Request_Note;
        if (string.IsNullOrWhiteSpace(note)) return BadRequest("Request note is required.");

        booking.ProcessingStatus = "document_update_requested";
        booking.DocumentRequestNote = note;
        booking.DocumentRequestDate = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.UserId, "visa_application", id, "document_update_requested", booking.PayableAmount,
            note, ct, booking.DocumentNumber);

        return Ok(new { Message = "Document update requested successfully." });
    }

    public class UpdatePayableAmountRequestDto
    {
        public decimal? PayableAmount { get; set; }
        public decimal? Payable_Amount { get; set; }
    }

    [HttpPost("sales-visa/applications/{id:long}/payable-amount")]
    [HttpPut("sales-visa/applications/{id:long}/payable-amount")]
    public async Task<IActionResult> UpdateVisaPayableAmount([FromRoute] long id, [FromBody] UpdatePayableAmountRequestDto request, CancellationToken ct)
    {
        var amount = request.PayableAmount ?? request.Payable_Amount;
        if (!amount.HasValue || amount.Value < 0) return BadRequest("Payable amount cannot be negative.");

        var booking = await _db.SalesVisaApplications
            .Include(v => v.Applicants)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound("Visa application not found.");

        var oldPayable = booking.PayableAmount;
        var newPayable = amount.Value;
        var diff = oldPayable - newPayable;

        bool isAgent = string.Equals(booking.ClientType, "agent", StringComparison.OrdinalIgnoreCase) ||
                       (!booking.HeadId.HasValue && booking.AgentId.HasValue);

        Head? head = null;
        if (booking.HeadId.HasValue)
        {
            head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == booking.HeadId.Value, ct);
        }

        var customerAccountCommandService = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService>();
        var ledgerNotificationService = HttpContext.RequestServices.GetService<TexasPortal.Application.Common.Interfaces.ILedgerNotificationService>();
        var walletNotificationService = HttpContext.RequestServices.GetService<TexasPortal.Application.Common.Interfaces.IWalletNotificationService>();
        var outstandingNotificationService = HttpContext.RequestServices.GetService<TexasPortal.Application.Common.Interfaces.IOutstandingNotificationService>();
        var agentsApiClient = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Queries.IAgentsApiClient>();

        var isPaid = !string.IsNullOrWhiteSpace(booking.PaymentStatus) &&
                     (booking.PaymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
                      booking.PaymentStatus.Equals("success", StringComparison.OrdinalIgnoreCase) ||
                      booking.PaymentStatus.Equals("verified", StringComparison.OrdinalIgnoreCase) ||
                      booking.PaymentStatus.Equals("completed", StringComparison.OrdinalIgnoreCase));

        // If reduced on a paid/verified booking, refund difference to Head / Agent
        if (diff > 0m && isPaid)
        {
            if (head != null && customerAccountCommandService != null)
            {
                await customerAccountCommandService.ProcessRefundAsync(
                    head.Id,
                    diff,
                    booking.DocumentNumber,
                    1,
                    $"Refund for visa application {booking.DocumentNumber} payable amount adjustment from {oldPayable:N3} BHD to {newPayable:N3} BHD. Credit: +{diff:N3} BHD.",
                    ct: ct);

                var ledger = new HeadLedger
                {
                    HeadId = head.Id,
                    ServiceType = "visa_application_payable_adjustment",
                    ServiceId = booking.Id,
                    Debit = 0m,
                    Credit = diff,
                    Description = $"Payable amount adjusted for visa application {booking.DocumentNumber}. Reduced from {oldPayable:N3} BHD to {newPayable:N3} BHD. Credit: +{diff:N3} BHD.",
                    CreatedAt = DateTime.UtcNow
                };
                _db.HeadLedgers.Add(ledger);

                if (ledgerNotificationService != null)
                {
                    await ledgerNotificationService.PublishLedgerChangedAsync(
                        referenceType: TexasPortal.Contracts.Notifications.ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        headId: head.Id,
                        cancellationToken: ct);
                }

                if (walletNotificationService != null)
                {
                    await walletNotificationService.PublishWalletChangedAsync(
                        referenceType: TexasPortal.Contracts.Notifications.ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: head.Id,
                        ownerType: head.Type,
                        cancellationToken: ct);
                }

                if (outstandingNotificationService != null)
                {
                    await outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: TexasPortal.Contracts.Notifications.ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: head.Id,
                        ownerType: "customer",
                        cancellationToken: ct);
                }
            }
            else if (isAgent && booking.AgentId.HasValue && agentsApiClient != null)
            {
                try
                {
                    await agentsApiClient.CreditAgentWalletAsync(
                        booking.AgentId.Value,
                        diff,
                        $"Refund for visa application {booking.DocumentNumber} payable amount adjustment from {oldPayable:N3} BHD to {newPayable:N3} BHD. Credit: +{diff:N3} BHD.",
                        ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AGENT WALLET ERROR] Failed to credit agent wallet: {ex.Message}");
                }
            }
        }

        booking.PayableAmount = newPayable;
        booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;

        if (booking.Applicants != null && booking.Applicants.Count > 0)
        {
            var perApplicant = newPayable / booking.Applicants.Count;
            foreach (var app in booking.Applicants)
            {
                app.PayableAmount = perApplicant;
                app.Profit = app.PayableAmount - app.ExpectedSupplierCost;
            }
        }

        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { 
            booking.Id, 
            booking.PayableAmount, 
            booking.Profit, 
            OldPayableAmount = oldPayable, 
            BalanceAdjustment = diff > 0m && isPaid ? diff : 0m 
        });
    }

    // â”€â”€â”€ VFS Processing Actions â”€â”€â”€

    [HttpPost("vfs-appointments/{id:long}/process")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ProcessVfs(long id, [FromForm] ProcessVfsRequest request, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        if (!string.Equals(booking.ProcessingStatus, "approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            booking.ProcessingStatus = "under review";
        }
        booking.UpdatedAt = DateTime.UtcNow;

        var agentsApiClient = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Queries.IAgentsApiClient>();
        var govtFees = GetEffectiveGovtFees(request.GovtFees);

        var existingVfsFees = await _db.VfsAppointmentGovtFees
            .Where(g => g.VfsAppointmentId == id)
            .ToListAsync(ct);

        var updatedVfsRowKeys = new HashSet<string>();

        foreach (var fee in govtFees)
        {
            var path = await SaveFileAsync(fee.PaymentProof, "govt_fee_proofs", ct);
            VfsAppointmentGovtFee? existingRow = null;

            if (!string.IsNullOrWhiteSpace(fee.RowKey))
            {
                existingRow = existingVfsFees.FirstOrDefault(e => e.RowKey == fee.RowKey);
            }
            if (existingRow == null && !string.IsNullOrWhiteSpace(fee.TransactionNo))
            {
                existingRow = existingVfsFees.FirstOrDefault(e => !string.IsNullOrEmpty(e.PaymentReference) && string.Equals(e.PaymentReference, fee.TransactionNo, StringComparison.OrdinalIgnoreCase));
            }

            if (existingRow != null)
            {
                existingRow.Amount = fee.Amount;
                if (fee.SupplierId.HasValue && fee.SupplierId.Value > 0) existingRow.SupplierId = fee.SupplierId.Value;
                if (!string.IsNullOrWhiteSpace(fee.SupplierName)) existingRow.SupplierName = fee.SupplierName;
                if (!string.IsNullOrWhiteSpace(fee.TransactionNo)) existingRow.PaymentReference = fee.TransactionNo;
                if (path != null) existingRow.PaymentProof = path;
                else if (!string.IsNullOrWhiteSpace(fee.ExistingPaymentProof)) existingRow.PaymentProof = fee.ExistingPaymentProof;
                existingRow.UpdatedAt = DateTime.UtcNow;
                updatedVfsRowKeys.Add(existingRow.RowKey);
                fee.RowKey = existingRow.RowKey;
            }
            else
            {
                var newRowKey = string.IsNullOrWhiteSpace(fee.RowKey) ? Guid.NewGuid().ToString("D") : fee.RowKey;
                var newFee = new VfsAppointmentGovtFee
                {
                    RowKey = newRowKey,
                    VfsAppointmentId = id,
                    SupplierId = fee.SupplierId,
                    SupplierName = fee.SupplierName,
                    Amount = fee.Amount,
                    PaymentReference = fee.TransactionNo,
                    Status = "recorded",
                    PaymentProof = path ?? fee.ExistingPaymentProof,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.VfsAppointmentGovtFees.Add(newFee);
                updatedVfsRowKeys.Add(newRowKey);
                fee.RowKey = newRowKey;
            }
        }

        var unlinkedRemovedVfs = existingVfsFees.Where(e => !updatedVfsRowKeys.Contains(e.RowKey) && e.AccountsPaymentId == null && e.Status == "recorded").ToList();
        if (unlinkedRemovedVfs.Any())
        {
            _db.VfsAppointmentGovtFees.RemoveRange(unlinkedRemovedVfs);
        }

        await _db.SaveChangesAsync(ct);

        var currentVfsFees = await _db.VfsAppointmentGovtFees
            .Where(g => g.VfsAppointmentId == id)
            .ToListAsync(ct);

        if (agentsApiClient != null)
        {
            var syncFees = currentVfsFees.Select(f => new TexasPortal.Application.Sales.Queries.GovtFeeSyncItem(f.Amount, f.SupplierId, f.SupplierName, f.PaymentReference, "VFS Appointment Fee", f.RowKey)).ToList();
            await agentsApiClient.SyncGovtSupplierPaymentsAsync(booking.Id.ToString(), "VFS", booking.CreatedBy, syncFees, ct);
        }

        booking.ExpectedSupplierCost = currentVfsFees.Sum(f => f.Amount);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("vfs-appointments/{id:long}/update")]
    public async Task<IActionResult> UpdateVfsSchedule(long id, [FromBody] UpdateVfsScheduleRequest request, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        booking.AppointmentDate = request.AppointmentDate;
        if (TimeSpan.TryParse(request.AppointmentTime, out var parsedTime))
        {
            booking.AppointmentTime = parsedTime;
        }
        booking.VfsReceipt = request.Description; // Using VfsReceipt to hold schedule note/description as per legacy schema
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("vfs-appointments/{id:long}/update-vfs-ref")]
    public async Task<IActionResult> UpdateVfsRef(long id, [FromBody] UpdateVfsRefRequest request, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("vfs-appointments/{id:long}/status")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateVfsStatus(long id, [FromForm] UpdateVfsStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        var status = request.Status.ToLower();
        booking.ProcessingStatus = status;

        if (status == "completed")
        {
            var vfsDocPath = await SaveFileAsync(request.VfsDoc, "vfs_documents", ct);
            var receiptPath = await SaveFileAsync(request.VerifiedReceipt, "verified_receipts", ct);
            
            booking.PassportCopy = vfsDocPath; // Map to passport_copy / vfs_receipt
            booking.VfsReceipt = receiptPath;
        }
        else if (status == "cancelled")
        {
            var cancelReceiptPath = await SaveFileAsync(request.RejectedReceipt, "rejected_receipts", ct);
            booking.VfsReceipt = cancelReceiptPath;
        }

        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.CreatedBy, "vfs_appointment", id, status, booking.PayableAmount,
            $"{booking.Name} - {booking.Country}", ct, booking.DocumentNumber);

        return Ok();
    }

    // â”€â”€â”€ Holiday Processing Actions â”€â”€â”€

    [HttpPost("holiday-packages/{id:long}/process")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ProcessHoliday(long id, [FromForm] ProcessHolidayRequest request, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        if (!string.Equals(booking.ProcessingStatus, "approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            booking.ProcessingStatus = "under review";
        }
        booking.UpdatedAt = DateTime.UtcNow;

        var agentsApiClient = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Queries.IAgentsApiClient>();
        var govtFees = GetEffectiveGovtFees(request.GovtFees);

        // Clear existing government/supplier fees for this package so deleted rows are removed and updated rows are replaced
        var existingFees = await _db.HolidayPackageGovtFees
            .Where(g => g.HolidayPackageId == id)
            .ToListAsync(ct);

        if (existingFees.Any())
        {
            _db.HolidayPackageGovtFees.RemoveRange(existingFees);
        }

        foreach (var fee in govtFees)
        {
            var targetSupplierId = (fee.SupplierId.HasValue && fee.SupplierId.Value > 0) ? fee.SupplierId.Value : (booking.SupplierId ?? 1);
            var path = await SaveFileAsync(fee.PaymentProof, "govt_fee_proofs", ct);
            _db.HolidayPackageGovtFees.Add(new HolidayPackageGovtFee
            {
                HolidayPackageId = id,
                SupplierId = targetSupplierId,
                SupplierName = fee.SupplierName,
                Description = string.IsNullOrWhiteSpace(fee.GovtFeeDescription) ? $"Supplier Payment for Holiday {booking.DocumentNumber}" : fee.GovtFeeDescription,
                Amount = fee.Amount,
                PaymentReference = fee.TransactionNo,
                Status = "recorded",
                PaymentProof = path ?? fee.ExistingPaymentProof,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        }

        if (agentsApiClient != null)
        {
            var syncFees = govtFees.Select(f => new TexasPortal.Application.Sales.Queries.GovtFeeSyncItem(f.Amount, f.SupplierId, f.SupplierName, f.TransactionNo, f.GovtFeeDescription)).ToList();
            await agentsApiClient.SyncGovtSupplierPaymentsAsync(booking.Id.ToString(), "Holiday", booking.CreatedBy, syncFees, ct);
        }

        booking.ExpectedSupplierCost = govtFees.Sum(f => f.Amount);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("holiday-packages/{id:long}/status")]
    public async Task<IActionResult> UpdateHolidayStatus(long id, [FromBody] UpdateHolidayStatusRequest request, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        var status = request.Status.ToLower();
        booking.ProcessingStatus = status;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.CreatedBy, "holiday_package", id, status, booking.PayableAmount,
            $"{booking.PassengerName} - {booking.Country}", ct, booking.DocumentNumber);

        return Ok();
    }

    [HttpPost("holiday-packages/{id:long}/update-holiday-id")]
    public async Task<IActionResult> UpdateHolidayRef(long id, [FromBody] UpdateHolidayRefRequest request, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("holiday-packages/{id:long}/documents")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadHolidayDocs(long id, [FromForm] UploadHolidayDocsRequest request, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return NotFound();

        var effectiveDocs = GetEffectiveHolidayDocs(request.Documents);
        if (effectiveDocs.Count == 0)
        {
            return BadRequest("At least one valid document is required.");
        }

        foreach (var doc in effectiveDocs)
        {
            var path = await SaveFileAsync(doc.File, "holiday_documents", ct);
            if (path != null)
            {
                _db.HolidayPackageDocuments.Add(new HolidayPackageDocument
                {
                    HolidayPackageId = id,
                    DocumentPath = path,
                    DocumentType = string.IsNullOrWhiteSpace(doc.ServiceName) ? "Travel Document" : doc.ServiceName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private List<HolidayDocUploadItem> GetEffectiveHolidayDocs(List<HolidayDocUploadItem>? boundDocs)
    {
        var list = new List<HolidayDocUploadItem>();

        if (boundDocs != null && boundDocs.Count > 0)
        {
            foreach (var doc in boundDocs)
            {
                if (doc.File != null && doc.File.Length > 0)
                {
                    list.Add(doc);
                }
            }
        }

        if (list.Count == 0 && HttpContext?.Request?.HasFormContentType == true)
        {
            var form = HttpContext.Request.Form;
            var indices = new HashSet<string>();

            foreach (var key in form.Keys)
            {
                var match = System.Text.RegularExpressions.Regex.Match(key, @"(?:documents|Documents)\[(\d+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    indices.Add(match.Groups[1].Value);
                }
            }

            foreach (var file in form.Files)
            {
                var match = System.Text.RegularExpressions.Regex.Match(file.Name, @"(?:documents|Documents)\[(\d+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    indices.Add(match.Groups[1].Value);
                }
            }

            foreach (var idx in indices)
            {
                var serviceName = GetFormValue(form, $"Documents[{idx}].ServiceName", $"documents[{idx}][serviceName]", $"documents[{idx}][service_name]", $"Documents[{idx}][serviceName]");
                var file = form.Files.FirstOrDefault(f => 
                    string.Equals(f.Name, $"Documents[{idx}].File", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Name, $"documents[{idx}][file]", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Name, $"documents[{idx}].file", StringComparison.OrdinalIgnoreCase));

                if (file != null && file.Length > 0)
                {
                    list.Add(new HolidayDocUploadItem
                    {
                        ServiceName = string.IsNullOrWhiteSpace(serviceName) ? "Travel Document" : serviceName,
                        File = file
                    });
                }
            }
        }

        return list;
    }

    // â”€â”€â”€ File Upload Helper â”€â”€â”€

    private async Task<string?> SaveFileAsync(IFormFile? file, string folder, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return null;

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", folder);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        return $"/uploads/{folder}/{fileName}";
    }

    // â”€â”€â”€ User Sync Endpoint â”€â”€â”€

    public record UserSyncRequest(
        string Name,
        string Email,
        string? Password,
        string UserType,
        bool IsActive
    );

    [HttpPost("users/sync")]
    public async Task<IActionResult> SyncUser([FromBody] UserSyncRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "Email and Name are required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user != null)
        {
            user.Name = request.Name;
            user.UserType = string.IsNullOrWhiteSpace(request.UserType) ? user.UserType : request.UserType;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
            }
            await _db.SaveChangesAsync(ct);
            return Ok(new { Message = "User updated in Sales Portal." });
        }
        else
        {
            var newUser = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password ?? "password123", workFactor: 12),
                UserType = string.IsNullOrWhiteSpace(request.UserType) ? "Salesman" : request.UserType,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync(ct);
            return Ok(new { Message = "User created in Sales Portal." });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                UserType = string.IsNullOrWhiteSpace(u.UserType) ? "Salesman" : u.UserType,
                u.IsActive,
                u.LastLoginAt,
                u.LastLoginIp,
                AvatarUrl = u.Avatar,
                u.MonthlyTarget,
                u.ProfitTarget,
                u.CreatedAt,
                u.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpGet("users/{id:long}")]
    public async Task<IActionResult> GetUserById([FromRoute] long id, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound(new { Message = "User not found in Sales Portal." });
        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.MonthlyTarget,
            user.ProfitTarget,
            UserType = string.IsNullOrWhiteSpace(user.UserType) ? "Salesman" : user.UserType,
            user.IsActive,
            AvatarUrl = user.Avatar,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    [HttpPut("users/{id:long}/status")]
    public async Task<IActionResult> ToggleUserStatus([FromRoute] long id, [FromBody] Dictionary<string, bool> body, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound(new { Message = "User not found in Sales Portal." });

        if (body.TryGetValue("isActive", out var isActive))
        {
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { Message = $"Sales Portal user status updated to {(user.IsActive ? "active" : "inactive")}." });
    }

    public record UpdateUserTargetsRequest(decimal MonthlyTarget, decimal ProfitTarget);

    [HttpPut("users/{id:long}/targets")]
    public async Task<IActionResult> UpdateUserTargets([FromRoute] long id, [FromBody] UpdateUserTargetsRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound(new { Message = "User not found in Sales Portal." });

        user.MonthlyTarget = Math.Max(0m, request.MonthlyTarget);
        user.ProfitTarget = Math.Max(0m, request.ProfitTarget);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Sales Portal user targets updated successfully.", user.MonthlyTarget, user.ProfitTarget });
    }

    [HttpPut("users/{id:long}/password")]
    public async Task<IActionResult> ResetUserPassword([FromRoute] long id, [FromBody] Dictionary<string, string> body, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound(new { Message = "User not found in Sales Portal." });

        if (body.TryGetValue("newPassword", out var newPassword) && !string.IsNullOrWhiteSpace(newPassword))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { Message = "Sales Portal user password reset successfully." });
    }

    [HttpPut("users/{id:long}/email")]
    public async Task<IActionResult> UpdateUserEmail([FromRoute] long id, [FromBody] Dictionary<string, string> body, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound(new { Message = "User not found in Sales Portal." });

        if (body.TryGetValue("newEmail", out var newEmail) && !string.IsNullOrWhiteSpace(newEmail))
        {
            var cleanEmail = newEmail.Trim().ToLowerInvariant();
            var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail && u.Id != id, ct);
            if (existing != null)
            {
                return Conflict(new { Message = $"Sales Portal user with email '{newEmail}' already exists." });
            }

            user.Email = cleanEmail;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { Message = "Sales Portal user email updated successfully." });
    }

    private List<GovtFeeInput> GetEffectiveGovtFees(List<GovtFeeInput>? boundFees)
    {
        var list = new List<GovtFeeInput>();

        // 1. Standard Web API Model Binding Path: Use boundFees if populated
        if (boundFees != null && boundFees.Count > 0)
        {
            for (int i = 0; i < boundFees.Count; i++)
            {
                var fee = boundFees[i];
                if (fee.Amount > 0 || !string.IsNullOrWhiteSpace(fee.TransactionNo) || fee.PaymentProof != null || !string.IsNullOrWhiteSpace(fee.ExistingPaymentProof))
                {
                    if ((!fee.SupplierId.HasValue || fee.SupplierId <= 0) && HttpContext?.Request?.HasFormContentType == true)
                    {
                        var form = HttpContext.Request.Form;
                        if (long.TryParse(GetFormValue(form, $"GovtFees[{i}].SupplierId", $"govt_fees[{i}][supplier_id]", $"govt_fees[{i}][supplierId]", $"GovtFees[{i}][supplier_id]"), out var suppId) && suppId > 0)
                        {
                            fee.SupplierId = suppId;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(fee.SupplierName) && HttpContext?.Request?.HasFormContentType == true)
                    {
                        var form = HttpContext.Request.Form;
                        var suppName = GetFormValue(form, $"GovtFees[{i}].SupplierName", $"govt_fees[{i}][supplier_name]", $"govt_fees[{i}][supplierName]", $"GovtFees[{i}][supplier_name]");
                        if (!string.IsNullOrWhiteSpace(suppName)) fee.SupplierName = suppName;
                    }
                    list.Add(fee);
                }
            }
        }

        // 2. Backward-Compatibility Fallback: Parse legacy PHP/Laravel keys or legacy single fields from Request.Form
        if (list.Count == 0 && HttpContext?.Request?.HasFormContentType == true)
        {
            var form = HttpContext.Request.Form;
            var indices = new HashSet<int>();

            foreach (var key in form.Keys)
            {
                var match = System.Text.RegularExpressions.Regex.Match(key, @"(?:govt_fees|GovtFees)\[(\d+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var idx))
                {
                    indices.Add(idx);
                }
            }

            foreach (var idx in indices.OrderBy(x => x))
            {
                long.TryParse(
                    GetFormValue(form, $"GovtFees[{idx}].SupplierId", $"govt_fees[{idx}][supplier_id]", $"govt_fees[{idx}][supplierId]", $"GovtFees[{idx}][supplier_id]"),
                    out var suppId
                );

                var suppName = GetFormValue(form, $"GovtFees[{idx}].SupplierName", $"govt_fees[{idx}][supplier_name]", $"govt_fees[{idx}][supplierName]", $"GovtFees[{idx}][supplier_name]");

                decimal.TryParse(
                    GetFormValue(form, $"GovtFees[{idx}].Amount", $"govt_fees[{idx}][amount]", $"GovtFees[{idx}][amount]"),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var amount
                );

                var txnNo = GetFormValue(form, $"GovtFees[{idx}].TransactionNo", $"govt_fees[{idx}][transaction_no]", $"govt_fees[{idx}][transactionNo]", $"GovtFees[{idx}][transaction_no]");
                var desc = GetFormValue(form, $"GovtFees[{idx}].GovtFeeDescription", $"govt_fees[{idx}][govt_fee_description]", $"govt_fees[{idx}][govt_fee_description]");
                var existingProof = GetFormValue(form, $"GovtFees[{idx}].ExistingPaymentProof", $"govt_fees[{idx}][existing_payment_proof]");

                IFormFile? file = GetFormFile(form, $"GovtFees[{idx}].PaymentProof", $"govt_fees[{idx}][payment_proof]", $"govtFees[{idx}][paymentProof]");

                if (amount > 0 || !string.IsNullOrWhiteSpace(txnNo) || file != null || !string.IsNullOrWhiteSpace(existingProof))
                {
                    list.Add(new GovtFeeInput
                    {
                        SupplierId = suppId > 0 ? suppId : null,
                        SupplierName = suppName,
                        Amount = amount,
                        TransactionNo = txnNo ?? string.Empty,
                        GovtFeeDescription = desc,
                        ExistingPaymentProof = existingProof,
                        PaymentProof = file
                    });
                }
            }

            // Legacy single field fallback
            if (list.Count == 0 && form.ContainsKey("govtFeeAmount"))
            {
                if (decimal.TryParse(form["govtFeeAmount"].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var legacyAmount) && legacyAmount > 0)
                {
                    list.Add(new GovtFeeInput
                    {
                        Amount = legacyAmount,
                        TransactionNo = form["govtFeeTransactionNo"].ToString() ?? string.Empty,
                        GovtFeeDescription = "Government Fee",
                        PaymentProof = form.Files["govtFeePaymentProof"]
                    });
                }
            }
        }

        return list;
    }

    private static string? GetFormValue(IFormCollection form, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (form.ContainsKey(key) && !string.IsNullOrWhiteSpace(form[key]))
            {
                return form[key].ToString();
            }
        }
        return null;
    }

    private static IFormFile? GetFormFile(IFormCollection form, params string[] keys)
    {
        foreach (var key in keys)
        {
            var file = form.Files[key];
            if (file != null) return file;
        }
        return null;
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("govt-fees/update")]
    public async Task<IActionResult> UpdateGovtFee([FromBody] SyncGovtFeeUpdateRequest request, CancellationToken ct)
    {
        var serviceType = request.ServiceType?.ToLowerInvariant() ?? "";

        await SyncSupplierCostInternalAsync(new SyncSupplierCostRequest(
            request.ServiceType,
            request.EntityId,
            request.Amount,
            request.SupplierId,
            request.SupplierName,
            request.Description,
            request.ExternalReference
        ), ct);

        if (serviceType.Contains("holiday"))
        {
            var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == request.EntityId, ct);
            if (booking != null)
            {
                var fees = await _db.HolidayPackageGovtFees
                    .Where(g => g.HolidayPackageId == request.EntityId)
                    .OrderBy(g => g.CreatedAt)
                    .ToListAsync(ct);

                HolidayPackageGovtFee? target = null;
                if (!string.IsNullOrWhiteSpace(request.RowKey))
                {
                    target = fees.FirstOrDefault(f => f.RowKey == request.RowKey);
                }
                if (target == null)
                {
                    target = fees.FirstOrDefault(f => f.AccountsPaymentId == request.PaymentId);
                }
                if (target == null && string.IsNullOrWhiteSpace(request.RowKey))
                {
                    target = fees.FirstOrDefault(f => f.AccountsPaymentId == null && f.Status == "recorded");
                }

                if (target == null)
                {
                    return BadRequest($"No fee row found for RowKey {request.RowKey} / PaymentId {request.PaymentId}");
                }

                target.AccountsPaymentId = request.PaymentId;
                target.Amount = request.Amount;
                if (request.SupplierId.HasValue) target.SupplierId = request.SupplierId.Value;
                if (!string.IsNullOrWhiteSpace(request.SupplierName)) target.SupplierName = request.SupplierName;
                if (!string.IsNullOrWhiteSpace(request.Status)) target.Status = request.Status;
                if (!string.IsNullOrWhiteSpace(request.Description)) target.Description = request.Description;
                target.UpdatedAt = DateTime.UtcNow;

                var previousLink = await _db.GovtFeePaymentLinks
                    .FirstOrDefaultAsync(l => l.FeeTable == "holiday_package_govt_fees" && l.RowKey == target.RowKey && l.SupersededAt == null, ct);

                if (previousLink != null && previousLink.AccountsPaymentId != request.PaymentId)
                {
                    previousLink.SupersededAt = DateTime.UtcNow;
                    previousLink.SupersededReason = $"Replaced by payment {request.PaymentId}";
                }

                if (previousLink == null || previousLink.AccountsPaymentId != request.PaymentId)
                {
                    _db.GovtFeePaymentLinks.Add(new GovtFeePaymentLink
                    {
                        FeeTable = "holiday_package_govt_fees",
                        RowKey = target.RowKey,
                        AccountsPaymentId = request.PaymentId,
                        AmountAtLinkTime = request.Amount,
                        LinkedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(ct);

                var totalCost = await _db.HolidayPackageGovtFees
                    .Where(g => g.HolidayPackageId == request.EntityId)
                    .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;

                booking.ExpectedSupplierCost = totalCost;
                booking.Profit = booking.PayableAmount - totalCost;
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0) booking.SupplierId = request.SupplierId.Value;
                if (!string.IsNullOrWhiteSpace(request.SupplierName)) booking.SupplierName = request.SupplierName;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("visa"))
        {
            var visaApp = await _db.SalesVisaApplications
                .Include(v => v.Applicants)
                .FirstOrDefaultAsync(x => x.Id == request.EntityId, ct);

            if (visaApp != null)
            {
                var firstApplicantId = visaApp.Applicants.FirstOrDefault()?.Id ?? 0;
                var fees = await _db.SalesVisaGovtFees
                    .Where(g => g.SalesVisaApplicantId == firstApplicantId)
                    .OrderBy(g => g.CreatedAt)
                    .ToListAsync(ct);

                SalesVisaGovtFee? target = null;
                if (!string.IsNullOrWhiteSpace(request.RowKey))
                {
                    target = fees.FirstOrDefault(f => f.RowKey == request.RowKey);
                }
                if (target == null)
                {
                    target = fees.FirstOrDefault(f => f.AccountsPaymentId == request.PaymentId);
                }
                if (target == null && string.IsNullOrWhiteSpace(request.RowKey))
                {
                    target = fees.FirstOrDefault(f => f.AccountsPaymentId == null && f.Status == "recorded");
                }

                if (target == null)
                {
                    return BadRequest($"No fee row found for RowKey {request.RowKey} / PaymentId {request.PaymentId}");
                }

                target.AccountsPaymentId = request.PaymentId;
                target.Amount = request.Amount;
                if (!string.IsNullOrWhiteSpace(request.Status)) target.Status = request.Status;
                if (!string.IsNullOrWhiteSpace(request.Description)) target.Description = request.Description;
                target.UpdatedAt = DateTime.UtcNow;

                var previousLink = await _db.GovtFeePaymentLinks
                    .FirstOrDefaultAsync(l => l.FeeTable == "sales_visa_govt_fees" && l.RowKey == target.RowKey && l.SupersededAt == null, ct);

                if (previousLink != null && previousLink.AccountsPaymentId != request.PaymentId)
                {
                    previousLink.SupersededAt = DateTime.UtcNow;
                    previousLink.SupersededReason = $"Replaced by payment {request.PaymentId}";
                }

                if (previousLink == null || previousLink.AccountsPaymentId != request.PaymentId)
                {
                    _db.GovtFeePaymentLinks.Add(new GovtFeePaymentLink
                    {
                        FeeTable = "sales_visa_govt_fees",
                        RowKey = target.RowKey,
                        AccountsPaymentId = request.PaymentId,
                        AmountAtLinkTime = request.Amount,
                        LinkedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(ct);

                var totalCost = await _db.SalesVisaGovtFees
                    .Where(g => g.SalesVisaApplicantId == firstApplicantId)
                    .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;

                visaApp.ExpectedSupplierCost = totalCost;
                visaApp.Profit = visaApp.PayableAmount - totalCost;
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0) visaApp.SupplierId = request.SupplierId.Value;
                if (!string.IsNullOrWhiteSpace(request.SupplierName)) visaApp.SupplierName = request.SupplierName;
                foreach (var app in visaApp.Applicants)
                {
                    app.ExpectedSupplierCost = totalCost / Math.Max(1, visaApp.Applicants.Count);
                    app.Profit = app.PayableAmount - app.ExpectedSupplierCost;
                }
                visaApp.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("vfs"))
        {
            var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == request.EntityId, ct);
            if (booking != null)
            {
                var fees = await _db.VfsAppointmentGovtFees
                    .Where(g => g.VfsAppointmentId == request.EntityId)
                    .OrderBy(g => g.CreatedAt)
                    .ToListAsync(ct);

                VfsAppointmentGovtFee? target = null;
                if (!string.IsNullOrWhiteSpace(request.RowKey))
                {
                    target = fees.FirstOrDefault(f => f.RowKey == request.RowKey);
                }
                if (target == null)
                {
                    target = fees.FirstOrDefault(f => f.AccountsPaymentId == request.PaymentId);
                }
                if (target == null && string.IsNullOrWhiteSpace(request.RowKey))
                {
                    target = fees.FirstOrDefault(f => f.AccountsPaymentId == null && f.Status == "recorded");
                }

                if (target == null)
                {
                    return BadRequest($"No fee row found for RowKey {request.RowKey} / PaymentId {request.PaymentId}");
                }

                target.AccountsPaymentId = request.PaymentId;
                target.Amount = request.Amount;
                if (!string.IsNullOrWhiteSpace(request.Status)) target.Status = request.Status;
                target.UpdatedAt = DateTime.UtcNow;

                var previousLink = await _db.GovtFeePaymentLinks
                    .FirstOrDefaultAsync(l => l.FeeTable == "vfs_appointment_govt_fees" && l.RowKey == target.RowKey && l.SupersededAt == null, ct);

                if (previousLink != null && previousLink.AccountsPaymentId != request.PaymentId)
                {
                    previousLink.SupersededAt = DateTime.UtcNow;
                    previousLink.SupersededReason = $"Replaced by payment {request.PaymentId}";
                }

                if (previousLink == null || previousLink.AccountsPaymentId != request.PaymentId)
                {
                    _db.GovtFeePaymentLinks.Add(new GovtFeePaymentLink
                    {
                        FeeTable = "vfs_appointment_govt_fees",
                        RowKey = target.RowKey,
                        AccountsPaymentId = request.PaymentId,
                        AmountAtLinkTime = request.Amount,
                        LinkedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(ct);

                var totalCost = await _db.VfsAppointmentGovtFees
                    .Where(g => g.VfsAppointmentId == request.EntityId)
                    .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;

                booking.ExpectedSupplierCost = totalCost;
                booking.Profit = booking.PayableAmount - totalCost;
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0) booking.SupplierId = request.SupplierId.Value;
                if (!string.IsNullOrWhiteSpace(request.SupplierName)) booking.SupplierName = request.SupplierName;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("sync-supplier-cost")]
    public async Task<IActionResult> SyncSupplierCost([FromBody] SyncSupplierCostRequest request, CancellationToken ct)
    {
        await SyncSupplierCostInternalAsync(request, ct);
        return Ok();
    }

    private async Task SyncSupplierCostInternalAsync(SyncSupplierCostRequest request, CancellationToken ct)
    {
        var serviceType = request.ServiceType?.ToLowerInvariant() ?? "";
        var idOrDoc = !string.IsNullOrWhiteSpace(request.ExternalReference) ? request.ExternalReference : (request.EntityId > 0 ? request.EntityId.ToString() : "");
        long.TryParse(idOrDoc, out var parsedId);
        var docStr = idOrDoc?.Trim();

        if (serviceType.Contains("trans"))
        {
            var booking = await _db.Transportations.FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("flight"))
        {
            var booking = await _db.FlightBookings.Include(f => f.Passengers).FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;

                foreach (var p in booking.Passengers)
                {
                    if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                    {
                        p.SupplierId = request.SupplierId.Value;
                        p.IsInhouse = false;
                    }
                    if (!string.IsNullOrWhiteSpace(request.SupplierName))
                    {
                        p.SupplierName = request.SupplierName;
                    }
                    if (request.Amount >= 0 && booking.Passengers.Count > 0)
                    {
                        p.ExpectedSupplierCost = request.Amount / booking.Passengers.Count;
                        p.Profit = p.PayableAmount - p.ExpectedSupplierCost;
                    }
                }

                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("hotel"))
        {
            var booking = await _db.HotelBookings.FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("visa"))
        {
            var booking = await _db.SalesVisaApplications.Include(v => v.Applicants).FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }

                var firstApplicantId = booking.Applicants.FirstOrDefault()?.Id ?? 0;
                var govtFeesSum = await _db.SalesVisaGovtFees
                    .Where(g => g.SalesVisaApplicantId == firstApplicantId)
                    .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;

                var effectiveCost = govtFeesSum > 0 ? govtFeesSum : request.Amount;

                if (effectiveCost >= 0)
                {
                    booking.ExpectedSupplierCost = effectiveCost;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;

                foreach (var app in booking.Applicants)
                {
                    if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                    {
                        app.IsInhouse = false;
                    }
                    if (effectiveCost >= 0 && booking.Applicants.Count > 0)
                    {
                        app.ExpectedSupplierCost = effectiveCost / booking.Applicants.Count;
                        app.Profit = app.PayableAmount - app.ExpectedSupplierCost;
                    }
                }

                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("vfs"))
        {
            var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("holiday"))
        {
            var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("other"))
        {
            var booking = await _db.OtherServices.FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (serviceType.Contains("transport"))
        {
            var booking = await _db.Transportations.FirstOrDefaultAsync(x => (parsedId > 0 && x.Id == parsedId) || x.DocumentNumber == docStr || (docStr != null && docStr.Length >= 5 && x.DocumentNumber != null && x.DocumentNumber.EndsWith(docStr)), ct);
            if (booking != null)
            {
                if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
                {
                    booking.SupplierId = request.SupplierId.Value;
                    booking.IsInhouse = false;
                }
                if (!string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    booking.SupplierName = request.SupplierName;
                }
                if (request.Amount >= 0)
                {
                    booking.ExpectedSupplierCost = request.Amount;
                }
                booking.Profit = booking.PayableAmount - booking.ExpectedSupplierCost;
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        await FinalizeProfitHelperAsync(request.ServiceType, idOrDoc, ct);
        return;
    }

    [HttpPost("batch-document-numbers")]
    public async Task<IActionResult> GetBatchDocumentNumbers([FromBody] List<BatchDocNumberLookupItem> items, CancellationToken ct)
    {
        if (items == null || items.Count == 0)
            return Ok(new List<BatchDocNumberResponseItem>());

        var response = new List<BatchDocNumberResponseItem>();

        var grouped = items
            .Where(i => !string.IsNullOrWhiteSpace(i.ExternalReference))
            .GroupBy(i => (i.ServiceType ?? "").ToLowerInvariant().Replace("_", "").Trim());

        foreach (var g in grouped)
        {
            var ids = g.Select(x => long.TryParse(x.ExternalReference, out var id) ? id : 0)
                       .Where(id => id > 0)
                       .Distinct()
                       .ToList();

            if (ids.Count == 0) continue;

            var serviceKey = g.Key;

            if (serviceKey.Contains("visa") && !serviceKey.Contains("vfs"))
            {
                var dict = await _db.SalesVisaApplications
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
            else if (serviceKey.Contains("vfs"))
            {
                var dict = await _db.VfsAppointments
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
            else if (serviceKey.Contains("flight"))
            {
                var dict = await _db.FlightBookings
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
            else if (serviceKey.Contains("hotel"))
            {
                var dict = await _db.HotelBookings
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
            else if (serviceKey.Contains("holiday") || serviceKey.Contains("package"))
            {
                var dict = await _db.HolidayPackages
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
            else if (serviceKey.Contains("transport"))
            {
                var dict = await _db.Transportations
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
            else if (serviceKey.Contains("other"))
            {
                var dict = await _db.OtherServices
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DocumentNumber, ct);

                foreach (var item in g)
                {
                    if (long.TryParse(item.ExternalReference, out var id) && dict.TryGetValue(id, out var docNo) && !string.IsNullOrWhiteSpace(docNo))
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, docNo));
                    else
                        response.Add(new BatchDocNumberResponseItem(item.ServiceType, item.ExternalReference, item.ExternalReference));
                }
            }
        }

        return Ok(response);
    }

    [HttpPost("bookings/{serviceType}/{id:long}/processing-status")]
    public async Task<IActionResult> UpdateBookingProcessingStatus(string serviceType, long id, [FromBody] UpdateBookingProcessingStatusInternalRequest request, CancellationToken ct)
    {
        var normType = (serviceType ?? string.Empty).ToLowerInvariant().Replace("-", "_");
        var status = request.Status?.Trim().ToLowerInvariant() ?? string.Empty;
        
        bool targetClosed = request.IsDealClosed ?? (status == "completed" || status == "closed" || status == "deal_closed" || status == "locked");
        if (status == "under review" || status == "open" || status == "unlocked")
        {
            targetClosed = false;
        }

        // Deal lock / unlock requests (deal_closed, unlocked, locked) MUST NOT overwrite the operational processing status!
        bool shouldUpdateProcessingStatus = !string.IsNullOrWhiteSpace(status)
            && status != "deal_closed"
            && status != "unlocked"
            && status != "locked";

        bool oldDealClosed = false;
        string oldStatus = string.Empty;

        switch (normType)
        {
            case "flights" or "flight" or "flight_booking":
                var flight = await _db.FlightBookings.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (flight == null) return NotFound();
                oldDealClosed = flight.IsDealClosed;
                oldStatus = flight.ProcessingStatus ?? string.Empty;
                flight.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    flight.ProcessingStatus = status;
                }
                flight.UpdatedAt = DateTime.UtcNow;
                break;

            case "hotels" or "hotel" or "hotel_booking":
                var hotel = await _db.HotelBookings.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (hotel == null) return NotFound();
                oldDealClosed = hotel.IsDealClosed;
                oldStatus = hotel.ProcessingStatus ?? string.Empty;
                hotel.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    hotel.ProcessingStatus = status;
                }
                hotel.UpdatedAt = DateTime.UtcNow;
                break;

            case "visa" or "visas" or "visa_application":
                var visa = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (visa == null) return NotFound();
                oldDealClosed = visa.IsDealClosed;
                oldStatus = visa.ProcessingStatus ?? string.Empty;
                visa.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    visa.ProcessingStatus = status;
                }
                visa.UpdatedAt = DateTime.UtcNow;
                break;

            case "vfs" or "vfs_appointment" or "vfs_appointments":
                var vfs = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (vfs == null) return NotFound();
                oldDealClosed = vfs.IsDealClosed;
                oldStatus = vfs.ProcessingStatus ?? string.Empty;
                vfs.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    vfs.ProcessingStatus = status;
                }
                vfs.UpdatedAt = DateTime.UtcNow;
                break;

            case "holiday" or "holiday_package" or "holiday_packages":
                var holiday = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (holiday == null) return NotFound();
                oldDealClosed = holiday.IsDealClosed;
                oldStatus = holiday.ProcessingStatus ?? string.Empty;
                holiday.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    holiday.ProcessingStatus = status;
                }
                holiday.UpdatedAt = DateTime.UtcNow;
                break;

            case "transport" or "transportation" or "transportations":
                var transport = await _db.Transportations.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (transport == null) return NotFound();
                oldDealClosed = transport.IsDealClosed;
                oldStatus = transport.ProcessingStatus ?? string.Empty;
                transport.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    transport.ProcessingStatus = status;
                }
                transport.UpdatedAt = DateTime.UtcNow;
                break;

            case "other" or "other_service" or "other_services":
                var other = await _db.OtherServices.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (other == null) return NotFound();
                oldDealClosed = other.IsDealClosed;
                oldStatus = other.ProcessingStatus ?? string.Empty;
                other.IsDealClosed = targetClosed;
                if (shouldUpdateProcessingStatus)
                {
                    other.ProcessingStatus = status;
                }
                other.UpdatedAt = DateTime.UtcNow;
                break;

            default:
                return BadRequest($"Unsupported service type: {serviceType}");
        }

        await _db.SaveChangesAsync(ct);

        // Authoritative Profit Recognition on Deal Closed / Unlock
        var profitService = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Profit.ISalesmanProfitService>();
        if (profitService != null)
        {
            if (targetClosed && !oldDealClosed)
            {
                // Transition 1: Deal Closed -> Authoritative Final Profit Recognition
                await profitService.FinalizeOrUpdateProfitAsync(serviceType, id.ToString(), ct);
            }
            else if (!targetClosed && oldDealClosed)
            {
                // Transition 2: Unlocking Deal -> Unfinalize and Reconcile
                await profitService.UnfinalizeProfitAsync(serviceType, id.ToString(), ct);
            }
        }

        return Ok(new { Success = true, IsDealClosed = targetClosed, Status = targetClosed ? "closed" : "open" });
    }

    // â”€â”€â”€ Sales Quotations â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [HttpGet("quotations")]
    [ProducesResponseType(typeof(PagedResponse<TexasPortal.Contracts.Sales.Quotations.QuotationSummaryResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetQuotations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? clientType = null,
        [FromQuery] long? salesmanId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 1000) return BadRequest("Invalid paging parameters.");

        var result = await _readService.GetQuotationsAsync(page, pageSize, search, clientType, salesmanId, startDate, endDate, status, ct);
        return Ok(result);
    }

    [HttpGet("quotations/{idOrDoc}")]
    [ProducesResponseType(typeof(TexasPortal.Contracts.Sales.Quotations.QuotationDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetQuotationDetails(string idOrDoc, CancellationToken ct)
    {
        var result = await _readService.GetQuotationByIdAsync(idOrDoc, ct);
        return result is null ? NotFound("Quotation not found.") : Ok(result);
    }

    [HttpPost("quotations/{idOrDoc}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400), ProducesResponseType(404)]
    public async Task<IActionResult> UpdateQuotationStatus(string idOrDoc, [FromBody] TexasPortal.Contracts.Sales.Quotations.UpdateQuotationStatusRequest request, CancellationToken ct)
    {
        var targetStatus = (request.Status ?? "").Trim().ToLowerInvariant();
        if (targetStatus != "pending" && targetStatus != "approved" && targetStatus != "rejected" && targetStatus != "success")
        {
            return BadRequest("Status must be 'pending', 'approved', 'rejected', or 'success'.");
        }

        var q = _db.Quotations.AsQueryable();
        if (long.TryParse(idOrDoc, out var id))
        {
            q = q.Where(x => x.Id == id || x.DocumentNumber == idOrDoc);
        }
        else
        {
            q = q.Where(x => x.DocumentNumber == idOrDoc);
        }

        var quotation = await q.FirstOrDefaultAsync(ct);
        if (quotation == null) return NotFound("Quotation not found.");

        var oldStatus = quotation.Status;
        quotation.Status = targetStatus;
        quotation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, targetStatus, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                    _db, quotation.UserId, "quotation", quotation.Id, quotation.Status, quotation.TotalAmount,
                    $"Quotation {quotation.DocumentNumber} status updated to {quotation.Status}.", ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InternalOperationsController] Failed to create status notification for quotation {quotation.Id}: {ex.Message}");
            }
        }

        return Ok(new { Success = true, Status = quotation.Status, DocumentNumber = quotation.DocumentNumber });
    }

    [HttpGet("quotations/{idOrDoc}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetQuotationPdf(string idOrDoc, CancellationToken ct)
    {
        var detail = await _readService.GetQuotationByIdAsync(idOrDoc, ct);
        if (detail == null) return NotFound("Quotation not found.");

        var pdfRequest = new TexasPortal.Contracts.Documents.QuotationPdfRequest(
            QuotationNo: detail.DocumentNumber,
            QuotationDate: detail.CreatedAt,
            ValidUntil: null,
            ClientType: detail.ClientType,
            HeadId: detail.HeadId,
            CustomerName: detail.CustomerName,
            CustomerContact: detail.CustomerContact,
            CustomerEmail: detail.CustomerEmail,
            SalesmanName: detail.SalesmanName,
            RemarksNote: detail.Remarks,
            Items: detail.Items.Select(i => new TexasPortal.Contracts.Documents.QuotationPdfItemRequest(
                ServiceType: i.ServiceType,
                Description: i.Description,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                TotalAmount: i.TotalAmount
            )).ToList()
        );

        var document = new TexasPortal.Infrastructure.Pdf.QuotationPdfDocument(pdfRequest);
        byte[] pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"{detail.DocumentNumber}.pdf");
    }

    private async Task FinalizeProfitHelperAsync(string? serviceType, string? bookingIdOrRef, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(serviceType) || string.IsNullOrWhiteSpace(bookingIdOrRef)) return;
        try
        {
            var profitService = HttpContext.RequestServices.GetService<TexasPortal.Application.Sales.Profit.ISalesmanProfitService>();
            if (profitService != null)
            {
                await profitService.FinalizeOrUpdateProfitAsync(serviceType, bookingIdOrRef, ct);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal exception handling for background sync
            System.Console.WriteLine($"[InternalOperationsController] Non-fatal profit update error for {serviceType}/{bookingIdOrRef}: {ex.Message}");
        }
    }

    [HttpPost("bookings/{serviceType}/{id:long}/operation-closed")]
    public async Task<IActionResult> UpdateBookingOperationClosed(string serviceType, long id, [FromBody] UpdateBookingOperationClosedRequest request, CancellationToken ct)
    {
        var normType = (serviceType ?? string.Empty).ToLowerInvariant().Replace("-", "_");
        var targetClosed = request.OperationClosed ?? request.IsOperationClosed ?? true;

        switch (normType)
        {
            case "visa" or "visas" or "visa_application":
                var visa = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (visa == null) return NotFound();
                visa.OperationClosed = targetClosed;
                visa.UpdatedAt = DateTime.UtcNow;
                break;

            case "flights" or "flight" or "flight_booking":
                var flight = await _db.FlightBookings.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (flight == null) return NotFound();
                flight.OperationClosed = targetClosed;
                flight.UpdatedAt = DateTime.UtcNow;
                break;

            case "hotels" or "hotel" or "hotel_booking":
                var hotel = await _db.HotelBookings.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (hotel == null) return NotFound();
                hotel.OperationClosed = targetClosed;
                hotel.UpdatedAt = DateTime.UtcNow;
                break;

            case "vfs" or "vfs_appointment" or "vfs_appointments":
                var vfs = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (vfs == null) return NotFound();
                vfs.OperationClosed = targetClosed;
                vfs.UpdatedAt = DateTime.UtcNow;
                break;

            case "holiday" or "holiday_package" or "holiday_packages":
                var holiday = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (holiday == null) return NotFound();
                holiday.OperationClosed = targetClosed;
                holiday.UpdatedAt = DateTime.UtcNow;
                break;

            case "transport" or "transportation" or "transportations":
                var trans = await _db.Transportations.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (trans == null) return NotFound();
                trans.OperationClosed = targetClosed;
                trans.UpdatedAt = DateTime.UtcNow;
                break;

            case "other" or "other_service" or "other_services":
                var other = await _db.OtherServices.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (other == null) return NotFound();
                other.OperationClosed = targetClosed;
                other.UpdatedAt = DateTime.UtcNow;
                break;

            default:
                return BadRequest($"Unsupported service type: {serviceType}");
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, serviceType, id, operationClosed = targetClosed });
    }

    /// <summary>Updates applicant personal information (name, nationality, passport) from operational portal.</summary>
    [HttpPut("sales-visa/applicants/{applicantId:long}")]
    public async Task<IActionResult> UpdateApplicantDetails([FromRoute] long applicantId, [FromBody] TexasPortal.Contracts.Sales.Bookings.UpdateVisaApplicantDetailsRequest request, CancellationToken ct)
    {
        var applicant = await _db.SalesVisaApplicants.FirstOrDefaultAsync(x => x.Id == applicantId, ct);
        if (applicant == null) return NotFound("Applicant not found.");

        if (request.Prefix != null) applicant.Prefix = request.Prefix.Trim();
        if (request.FName != null) applicant.FName = request.FName.Trim();
        if (request.LName != null) applicant.LName = request.LName.Trim();
        if (request.Dob.HasValue) applicant.Dob = request.Dob.Value;
        if (request.Nationality != null) applicant.Nationality = request.Nationality.Trim();
        if (request.PassportNo != null) applicant.PassportNo = request.PassportNo.Trim();
        if (request.ContactNo != null) applicant.ContactNo = request.ContactNo.Trim();
        if (request.EmailId != null) applicant.EmailId = request.EmailId.Trim();
        applicant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(applicant);
    }

}
