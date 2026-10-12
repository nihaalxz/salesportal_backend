using System;

namespace TexasPortal.Domain.Entities;

public class QuotationItem
{
    public long Id { get; set; }
    public long QuotationId { get; set; }
    public string ServiceType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }

    // Navigation property
    public Quotation Quotation { get; set; } = null!;
}
