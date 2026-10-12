namespace TexasPortal.Contracts.Sales.Dashboard;

public sealed class ProfileTargetMetricsResponse
{
    public decimal MonthlyVolume { get; init; }
    public decimal MonthlyProfit { get; init; }
    public decimal AllTimeVolume { get; init; }
    public decimal AllTimeProfit { get; init; }
    public decimal MonthlyTarget { get; init; }
    public decimal ProfitTarget { get; init; }
}
