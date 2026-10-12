using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Receipts;

namespace TexasPortal.Application.Sales.Queries;

public record GovtFeeSyncItem(
    decimal Amount,
    long? SupplierId = null,
    string? SupplierName = null,
    string? TransactionNo = null,
    string? GovtFeeDescription = null,
    string? RowKey = null
);

public interface IAgentsApiClient
{
    Task<List<HeadLookupResponse>> GetAgentsAsync(long salesmanId, string? search, int limit, CancellationToken ct);
    Task<HeadLookupResponse?> GetAgentByIdAsync(long agentId, CancellationToken ct);
    Task<List<BankResponse>> GetBanksAsync(CancellationToken ct);
    Task<List<SupplierResponse>> GetSuppliersAsync(CancellationToken ct);
    Task<List<VisaCountryMasterResponse>> GetVisaCountriesAsync(CancellationToken ct = default);
    Task<List<VisaPackageMasterResponse>> GetVisaPackagesAsync(long? countryId = null, CancellationToken ct = default);
    Task CreditAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct);
    Task DebitAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct);
    Task<PagedResponse<HeadLedgerEntryResponse>> GetAgentLedgerAsync(long agentId, int page, int pageSize, CancellationToken ct);
    Task CreateGovtSupplierPaymentAsync(long supplierId, decimal amount, string? referenceNumber, string? externalReference, string? serviceType, string? description, long? createdByUserId = null, CancellationToken ct = default);
    Task SyncGovtSupplierPaymentsAsync(string externalReference, string serviceType, long? createdByUserId, List<GovtFeeSyncItem> fees, CancellationToken ct = default);
    Task<(bool HasReturn, decimal? ReturnAmount, bool HasPurchaseReturn, decimal? PurchaseReturnAmount)> CheckSalesReturnAsync(string serviceType, string serviceId, CancellationToken ct = default);
    Task<BookingSupplierPaymentSummary?> GetBookingSupplierPaymentsAsync(string serviceType, string externalReference, CancellationToken ct = default);
    Task<string?> GetBookingApproverAsync(string serviceType, string bookingId, string? docNumber = null, CancellationToken ct = default);
}

public record BookingSupplierPaymentSummary(
    string ServiceType,
    string ExternalReference,
    decimal ApprovedTotal,
    List<ApprovedSupplierPaymentItem> Payments
);

public record ApprovedSupplierPaymentItem(
    long Id,
    decimal Amount,
    string? Reference,
    string? Description,
    string Status,
    System.DateTime? ApprovedAt
);
