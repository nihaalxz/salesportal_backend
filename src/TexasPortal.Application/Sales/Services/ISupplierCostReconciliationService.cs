using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Application.Sales.Services;

public record ReconciledFeeItem(
    long LocalId,
    decimal Amount,
    string? Description,
    string? PaymentReference,
    string Status
);

public record SupplierCostReconciliationResult(
    string ServiceType,
    string ExternalReference,
    decimal ApprovedSupplierCost,
    decimal EffectiveSupplierCost,
    List<ReconciledFeeItem> ReconciledFees
);

public interface ISupplierCostReconciliationService
{
    Task<SupplierCostReconciliationResult> ReconcileVisaFeesAsync(
        long visaApplicationId,
        List<ReconciledFeeItem> localFees,
        CancellationToken ct = default);

    Task<SupplierCostReconciliationResult> ReconcileVfsFeesAsync(
        long vfsAppointmentId,
        List<ReconciledFeeItem> localFees,
        CancellationToken ct = default);

    Task<SupplierCostReconciliationResult> ReconcileHolidayFeesAsync(
        long holidayPackageId,
        List<ReconciledFeeItem> localFees,
        CancellationToken ct = default);
}
