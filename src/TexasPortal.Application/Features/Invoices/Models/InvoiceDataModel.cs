using System;
using System.Collections.Generic;
using TexasPortal.Application.Common.Options;

namespace TexasPortal.Application.Features.Invoices.Models;

public class InvoiceDataModel
{
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string ServiceName { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerHeadName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string CustomerContact { get; set; } = string.Empty;

    public string SalesmanEmail { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string AccountantName { get; set; } = string.Empty;
    public string RemarksNote { get; set; } = string.Empty;

    public List<InvoiceLineItem> Items { get; set; } = new();

    public decimal SubTotal { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal NetTotal { get; set; }

    public string AmountInWords { get; set; } = string.Empty;

    public InvoiceOptions Options { get; set; } = new();
}

public class InvoiceLineItem
{
    public int ItemNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal Price { get; set; }
    public decimal NetPrice { get; set; }
    public string TaxRateText { get; set; } = "VAT 0%";
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
}
