using System;
using TexasPortal.Domain.Enums;
using TexasPortal.Domain.ValueObjects;

namespace TexasPortal.Domain.Services;

public class PaymentAllocationEngine : IPaymentAllocationEngine
{
    public PaymentAllocationOutput CalculateAllocation(PaymentAllocationInput input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.BookingAmount == null || input.BookingAmount.Amount < 0m)
            throw new ArgumentException("Booking amount must be non-negative.", nameof(input));

        // Note: Credit limit restrictions are removed as all 7 services operate on credit/core ledger accounts.
        Money balanceApplied;
        if (input.AutoApplyBalance && input.AvailableBalance != null && input.AvailableBalance.Amount > 0m)
        {
            var appliedAmount = Math.Min(input.AvailableBalance.Amount, input.BookingAmount.Amount);
            balanceApplied = new Money(appliedAmount, input.BookingAmount.Currency);
        }
        else
        {
            balanceApplied = Money.Zero(input.BookingAmount.Currency);
        }

        var receivableAmount = input.BookingAmount - balanceApplied;

        AllocationType allocationType;
        if (receivableAmount.Amount == 0m)
        {
            allocationType = AllocationType.FullyFromBalance;
        }
        else if (balanceApplied.Amount > 0m)
        {
            allocationType = AllocationType.SplitBalanceAndCredit;
        }
        else
        {
            allocationType = AllocationType.FullyCredit;
        }

        return new PaymentAllocationOutput(balanceApplied, receivableAmount, allocationType);
    }
}
