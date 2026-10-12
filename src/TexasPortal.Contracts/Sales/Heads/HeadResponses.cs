using System;

namespace TexasPortal.Contracts.Sales.Heads;

public class HeadSummaryResponse
{
    public long Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string Email { get; init; } = null!;
    public string MobileNo { get; init; } = null!;
    public string Nationality { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal Wallet { get; init; }
    public string? SalesmanName { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class HeadDetailResponse : HeadSummaryResponse
{
    public string Address { get; init; } = null!;
}

public sealed class HeadLookupResponse
{
    public long Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string Email { get; init; } = null!;
    public string MobileNo { get; init; } = null!;
    public decimal Wallet { get; init; }
}
