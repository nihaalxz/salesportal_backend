using System;

namespace TexasPortal.Contracts.Sales.Heads;

public sealed class HeadLedgerEntryResponse
{
    public long Id { get; init; }
    public long? ServiceId { get; init; }
    public string Type { get; init; } = null!;
    public string? Description { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal Balance { get; init; }
    public string? PaymentMethod { get; init; }
    public DateTime? CreatedAt { get; init; }
}
