using System;

namespace TexasPortal.Contracts.Sales.Cash;

public sealed class CashInHandResponse
{
    public long Id { get; init; }
    public decimal Amount { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class DepositSummaryResponse
{
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = null!;
    public string Type { get; init; } = null!;
    public long DestinationId { get; init; }
    public string? Narration { get; init; }
    public DateTime Date { get; init; }
    public DateTime? CreatedAt { get; init; }
}
