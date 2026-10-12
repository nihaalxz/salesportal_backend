using System;

namespace TexasPortal.Domain.ValueObjects;

public record Money(decimal Amount, string Currency = "BHD")
{
    public static Money Zero(string currency = "BHD") => new(0m, currency);
    public static Money BHD(decimal amount) => new(amount, "BHD");

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add money of different currencies: {a.Currency} vs {b.Currency}");
        return a with { Amount = a.Amount + b.Amount };
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot subtract money of different currencies: {a.Currency} vs {b.Currency}");
        return a with { Amount = a.Amount - b.Amount };
    }
}
