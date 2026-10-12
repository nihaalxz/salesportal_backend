using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Dashboard;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Contracts.Sales.Receipts;
using TexasPortal.Contracts.Sales.Salesmen;
using System.Collections.Generic;

namespace TexasPortal.Application.Sales.Queries;

public interface IOperationsReadService
{
    Task<IEnumerable<SalesmanResponse>> GetSalesmenAsync(CancellationToken ct);
    Task<IEnumerable<SalesmanReportResponse>> GetSalesmenReportAsync(System.DateTime? startDate = null, System.DateTime? endDate = null, CancellationToken ct = default);
    Task<IEnumerable<SalesmanServiceEntryResponse>> GetSalesmanServiceEntriesAsync(long salesmanId, System.DateTime? startDate = null, System.DateTime? endDate = null, CancellationToken ct = default);

    Task<VisaApplicationDetailResponse?> GetVisaApplicationByIdAsync(
        long id, CancellationToken ct);

    Task<PagedResponse<VisaApplicationSummaryResponse>> GetVisaApplicationsAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    Task<VfsAppointmentDetailResponse?> GetVfsAppointmentByIdAsync(
        long id, CancellationToken ct);

    Task<PagedResponse<VfsAppointmentSummaryResponse>> GetVfsAppointmentsAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    Task<HolidayPackageDetailResponse?> GetHolidayPackageByIdAsync(
        long id, CancellationToken ct);

    Task<PagedResponse<HolidayPackageSummaryResponse>> GetHolidayPackagesAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    // Global queries for Flights
    Task<PagedResponse<FlightBookingSummaryResponse>> GetFlightBookingsAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    Task<FlightBookingDetailResponse?> GetFlightBookingByIdAsync(
        long id, CancellationToken ct);

    // Global queries for Hotels
    Task<PagedResponse<HotelBookingSummaryResponse>> GetHotelBookingsAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    Task<HotelBookingDetailResponse?> GetHotelBookingByIdAsync(
        long id, CancellationToken ct);

    // Global queries for Transportation
    Task<PagedResponse<TransportationSummaryResponse>> GetTransportationsAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    Task<TransportationDetailResponse?> GetTransportationByIdAsync(
        long id, CancellationToken ct);

    // Global queries for Other Services
    Task<PagedResponse<OtherServiceSummaryResponse>> GetOtherServicesAsync(
        int page, int pageSize, string? status, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, bool? isDealClosed = null, CancellationToken ct = default);

    Task<OtherServiceDetailResponse?> GetOtherServiceByIdAsync(
        long id, CancellationToken ct);

    // Global queries for Customer Heads
    Task<PagedResponse<HeadSummaryResponse>> GetHeadsAsync(
        int page, int pageSize, string? search, string? type = null, CancellationToken ct = default);

    Task<HeadDetailResponse?> GetHeadByIdAsync(
        long id, string? type = null, CancellationToken ct = default);

    Task<PagedResponse<HeadLedgerEntryResponse>> GetHeadLedgerAsync(
        long headId, int page, int pageSize, string? type = null, System.DateTime? startDate = null, System.DateTime? endDate = null, string? serviceType = null, string? search = null, string? sort = null, CancellationToken ct = default);

    // Global queries for Leads
    Task<PagedResponse<LeadSummaryResponse>> GetLeadsAsync(
        int page, int pageSize, string? status, string? search, CancellationToken ct);

    Task<LeadDetailResponse?> GetLeadByIdAsync(
        long id, CancellationToken ct);

    // Global queries for Receipts
    Task<PagedResponse<ReceiptSummaryResponse>> GetReceiptsAsync(
        int page, int pageSize, string? search, System.DateTime? startDate = null, System.DateTime? endDate = null, CancellationToken ct = default);

    Task<ReceiptDetailResponse?> GetReceiptByIdAsync(
        long id, CancellationToken ct);

    // Global Dashboard Summary
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(CancellationToken ct);

    // Operational Portal â€” status counts for Visas, VFS, and Holidays
    Task<TexasPortal.Contracts.Sales.Dashboard.OperationalStatusCountsResponse> GetOperationalStatusCountsAsync(CancellationToken ct);

    // Global queries for Disputes
    Task<PagedResponse<TexasPortal.Contracts.Sales.Disputes.DisputeResponse>> GetDisputesAsync(
        int page, int pageSize, string? status, string? search, string? sort, CancellationToken ct);

    Task<TexasPortal.Contracts.Sales.Disputes.DisputeResponse?> GetDisputeByIdAsync(
        long id, CancellationToken ct);

    // Global queries for Quotations
    Task<PagedResponse<TexasPortal.Contracts.Sales.Quotations.QuotationSummaryResponse>> GetQuotationsAsync(
        int page, int pageSize, string? searchTerm = null, string? clientType = null, long? salesmanId = null, System.DateTime? startDate = null, System.DateTime? endDate = null, string? status = null, CancellationToken ct = default);

    Task<TexasPortal.Contracts.Sales.Quotations.QuotationDetailResponse?> GetQuotationByIdAsync(
        string idOrDoc, CancellationToken ct = default);

    Task<TexasPortal.Contracts.Sales.Disputes.DisputesDashboardResponse> GetDisputesDashboardAsync(
        CancellationToken ct);
}

