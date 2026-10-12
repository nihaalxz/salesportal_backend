using System;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class StaffMonthlyRecord : IUserOwned
{
    public long Id { get; set; }
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public string Month { get; set; } = null!;
    public int Year { get; set; }
    public decimal ProfitGenerated { get; set; }
    public decimal Incentive { get; set; }
    public decimal ProfitTarget { get; set; }
    public decimal MonthlyTarget { get; set; }
    public bool TargetAchieved { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HousingAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
