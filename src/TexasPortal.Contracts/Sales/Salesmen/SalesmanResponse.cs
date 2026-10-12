using System;

namespace TexasPortal.Contracts.Sales.Salesmen;

public sealed class SalesmanResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public string Email { get; init; } = null!;
}
