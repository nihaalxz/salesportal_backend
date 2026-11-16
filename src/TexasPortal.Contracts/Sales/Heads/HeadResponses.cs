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
    public string? Address { get; init; }
    public long? SalesmanId { get; init; }
    public string? SalesmanName { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class HeadDetailResponse : HeadSummaryResponse
{
    public new string Address { get; init; } = null!;
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

public sealed class CountryCodeOptionResponse
{
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string Flag { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class HeadSummaryStatsResponse
{
    public int TotalCount { get; init; }
    public int CustomerCount { get; init; }
    public int AgentCount { get; init; }
    public decimal TotalWallet { get; init; }
}
