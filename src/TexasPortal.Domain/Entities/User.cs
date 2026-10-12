using System;
using System.Collections.Generic;

namespace TexasPortal.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Avatar { get; set; }
    public string UserType { get; set; } = "Salesman";
    public bool IsActive { get; set; } = true;
    public string Password { get; set; } = null!;
    public string? RememberToken { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public decimal ProfitTarget { get; set; }
    public decimal MonthlyTarget { get; set; }
    public decimal ProfitGenerated { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<StaffMonthlyRecord> MonthlyRecords { get; set; } = new List<StaffMonthlyRecord>();
}
