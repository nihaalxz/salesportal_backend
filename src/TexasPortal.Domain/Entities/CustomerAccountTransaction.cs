using System;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Domain.Entities;

public class CustomerAccountTransaction
{
    public long Id { get; set; }
    public long HeadId { get; set; }
    public CustomerAccountTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BHD";
    public string? CorrelationId { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties
    public virtual Head? Head { get; set; }
}
