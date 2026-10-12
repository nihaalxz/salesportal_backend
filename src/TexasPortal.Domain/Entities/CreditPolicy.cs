using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class CreditPolicy
{
    public long Id { get; set; }
    public long HeadId { get; set; }
    public HeadType HeadType { get; set; } = HeadType.Customer;
    public decimal MaxCreditLimit { get; set; } = 5000.000m;
    public CreditTerm DefaultCreditTerm { get; set; } = CreditTerm.Days30;
    public bool AllowPartialPayments { get; set; } = true;
    public bool RequireApproval { get; set; } = true;
    public bool AutoBlockOnOverdue { get; set; } = true;
    public int GracePeriodDays { get; set; } = 7;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
