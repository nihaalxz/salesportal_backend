using System;

namespace TexasPortal.Domain.Entities;

public class GovtFeePaymentLink
{
    public long Id { get; set; }
    public string FeeTable { get; set; } = null!; // 'sales_visa_govt_fees', 'vfs_appointment_govt_fees', 'holiday_package_govt_fees'
    public string RowKey { get; set; } = null!;
    public long AccountsPaymentId { get; set; }
    public decimal AmountAtLinkTime { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SupersededAt { get; set; }
    public string? SupersededReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
