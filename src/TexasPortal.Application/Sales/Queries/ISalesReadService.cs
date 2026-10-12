using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Cash;
using TexasPortal.Contracts.Sales.Dashboard;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Contracts.Sales.Performance;

namespace TexasPortal.Application.Sales.Queries;

public sealed record HeadsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string SortBy = "lastName",
    string SortOrder = "asc",
    string? Type = null);

public sealed record LeadsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? Status = null,
    string SortBy = "createdAt",
    string SortOrder = "desc");

public sealed record FlightBookingsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record HotelBookingsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record VisaApplicationsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record VfsAppointmentsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record HolidayPackagesListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record TransportationsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record OtherServicesListQuery(
    int Page = 1,
    int PageSize = 10,
    string? PaymentStatus = null,
    string? ProcessingStatus = null,
    string? SearchTerm = null,
    bool? IsDealClosed = null);

public sealed record DepositsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? Status = null,
    string? SearchTerm = null);

public sealed record PerformanceListQuery(
    int? Year = null,
    string? Month = null);

public sealed record ReceiptsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null);

public sealed record DisputesListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? SearchTerm = null);

public sealed record QuotationsListQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? ClientType = null,
    string? Status = null);

public interface ISalesReadService
{
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(long userId, CancellationToken ct = default);
    Task<DashboardTrendResponse> GetDashboardTrendAsync(long userId, CancellationToken ct = default);
    Task<ProfileTargetMetricsResponse> GetProfileTargetMetricsAsync(long userId, CancellationToken ct = default);

    Task<PagedResponse<HeadSummaryResponse>> GetHeadsAsync(long userId, HeadsListQuery query, CancellationToken ct = default);
    Task<HeadDetailResponse?> GetHeadByIdAsync(long userId, long headId, string? type = null, CancellationToken ct = default);
    Task<PagedResponse<HeadLedgerEntryResponse>> GetHeadLedgerAsync(long userId, long headId, int page, int pageSize, string? type = null, System.DateTime? startDate = null, System.DateTime? endDate = null, string? serviceType = null, string? search = null, string? sort = null, CancellationToken ct = default);

    Task<PagedResponse<LeadSummaryResponse>> GetLeadsAsync(long userId, LeadsListQuery query, CancellationToken ct = default);
    Task<LeadDetailResponse?> GetLeadByIdAsync(long userId, long leadId, CancellationToken ct = default);

    Task<PagedResponse<FlightBookingSummaryResponse>> GetFlightBookingsAsync(long userId, FlightBookingsListQuery query, CancellationToken ct = default);
    Task<PagedResponse<HotelBookingSummaryResponse>> GetHotelBookingsAsync(long userId, HotelBookingsListQuery query, CancellationToken ct = default);
    Task<PagedResponse<VisaApplicationSummaryResponse>> GetVisaApplicationsAsync(long userId, VisaApplicationsListQuery query, CancellationToken ct = default);
    Task<PagedResponse<VfsAppointmentSummaryResponse>> GetVfsAppointmentsAsync(long userId, VfsAppointmentsListQuery query, CancellationToken ct = default);
    Task<PagedResponse<HolidayPackageSummaryResponse>> GetHolidayPackagesAsync(long userId, HolidayPackagesListQuery query, CancellationToken ct = default);
    Task<PagedResponse<TransportationSummaryResponse>> GetTransportationsAsync(long userId, TransportationsListQuery query, CancellationToken ct = default);
    Task<PagedResponse<OtherServiceSummaryResponse>> GetOtherServicesAsync(long userId, OtherServicesListQuery query, CancellationToken ct = default);
    Task<OtherServiceDetailResponse?> GetOtherServiceByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<FlightBookingDetailResponse?> GetFlightBookingByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<HotelBookingDetailResponse?> GetHotelBookingByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<VisaApplicationDetailResponse?> GetVisaApplicationByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<VfsAppointmentDetailResponse?> GetVfsAppointmentByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<HolidayPackageDetailResponse?> GetHolidayPackageByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<TransportationDetailResponse?> GetTransportationByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);

    Task<PagedResponse<TexasPortal.Contracts.Sales.Quotations.QuotationSummaryResponse>> GetQuotationsAsync(long userId, QuotationsListQuery query, CancellationToken ct = default);
    Task<TexasPortal.Contracts.Sales.Quotations.QuotationDetailResponse?> GetQuotationByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<bool> DeleteQuotationAsync(long userId, long id, CancellationToken ct = default);

    Task<CashInHandResponse?> GetCashInHandAsync(long userId, CancellationToken ct = default);
    Task<PagedResponse<DepositSummaryResponse>> GetDepositsAsync(long userId, DepositsListQuery query, CancellationToken ct = default);

    Task<PagedResponse<PerformanceSummaryResponse>> GetPerformanceRecordsAsync(long userId, PerformanceListQuery query, CancellationToken ct = default);
    Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>> LookupHeadsAsync(long userId, string? search, int limit, string? type = null, CancellationToken ct = default);
    Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Heads.OutstandingBookingResponse>> GetOutstandingBookingsForHeadAsync(long userId, long headId, string? type = null, CancellationToken ct = default);

    Task<PagedResponse<TexasPortal.Contracts.Sales.Receipts.ReceiptSummaryResponse>> GetReceiptsAsync(long userId, ReceiptsListQuery query, CancellationToken ct = default);
    Task<TexasPortal.Contracts.Sales.Receipts.ReceiptDetailResponse?> GetReceiptByIdAsync(long userId, string idOrDoc, CancellationToken ct = default);
    Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.BookingOptionResponse>> GetBookingOptionsAsync(long userId, long headId, string service, CancellationToken ct = default);
    Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.BankResponse>> GetBanksAsync(CancellationToken ct = default);
    Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>> GetSuppliersAsync(CancellationToken ct = default);

    Task<PagedResponse<TexasPortal.Contracts.Sales.Disputes.DisputeResponse>> GetDisputesAsync(long userId, DisputesListQuery query, CancellationToken ct = default);
    Task<TexasPortal.Contracts.Sales.Disputes.DisputeResponse?> GetDisputeByIdAsync(long userId, long disputeId, CancellationToken ct = default);
    Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Disputes.DisputeBookingOptionResponse>> GetDisputeBookingOptionsAsync(long userId, string serviceType, CancellationToken ct = default);
}
