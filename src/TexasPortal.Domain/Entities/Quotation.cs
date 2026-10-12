using System;
using System.Collections.Generic;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class Quotation : IUserOwned
{
    public long Id { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public long UserId { get; set; }
    long? IUserOwned.UserId => UserId;
    public long? HeadId { get; set; }
    public string ClientType { get; set; } = "customer";
    public string CustomerName { get; set; } = null!;
    public string? CustomerContact { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "pending";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Head? Head { get; set; }
    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
}
