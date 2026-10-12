using System;

namespace TexasPortal.Contracts.Sales.Performance;

public sealed class PerformanceSummaryResponse
{
    public long UserId { get; init; }
    public string Month { get; init; } = null!;
    public int Year { get; init; }
    public decimal ProfitGenerated { get; init; }
    public decimal ProfitTarget { get; init; }
    public decimal MonthlyTarget { get; init; }
    public decimal Incentive { get; init; }
    public bool TargetAchieved { get; init; }
    public decimal BasicSalary { get; init; }
    public decimal HousingAllowance { get; init; }
    public decimal TransportAllowance { get; init; }
    public decimal TotalCompensation => BasicSalary + HousingAllowance + TransportAllowance + Incentive;
}
