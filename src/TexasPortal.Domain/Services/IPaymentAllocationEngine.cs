using TexasPortal.Domain.Enums;
using TexasPortal.Domain.ValueObjects;

namespace TexasPortal.Domain.Services;

public record PaymentAllocationInput(
    Money AvailableBalance,
    Money OutstandingReceivables,
    Money CreditLimit,
    Money BookingAmount,
    bool AutoApplyBalance = true);

public record PaymentAllocationOutput(
    Money BalanceApplied,
    Money ReceivableAmount,
    AllocationType AllocationType);

public interface IPaymentAllocationEngine
{
    PaymentAllocationOutput CalculateAllocation(PaymentAllocationInput input);
}
