using System;
using System.Collections.Generic;

namespace TexasPortal.Application.Features.Documents.Models;

public class DocumentDataModel
{
    public string Category { get; set; } = "Invoice"; // "Invoice", "List", "Report"
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public List<KeyValuePair<string, string>> MetaData { get; set; } = new();

    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public string? ClientType { get; set; }

    public List<KeyValuePair<string, string>> ActiveFilters { get; set; } = new();

    public List<DocumentSummaryCard> SummaryCards { get; set; } = new();

    public DocumentTable Table { get; set; } = new();

    public DocumentTotals? Totals { get; set; }

    public string? RemarksNote { get; set; }
    public string? AmountInWords { get; set; }
    public string? SalesmanNameOrEmail { get; set; }
    public string? PreparedBy { get; set; }
    public string? AccountantName { get; set; }

    public DocumentOptions Options { get; set; } = new();
}

public class DocumentSummaryCard
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Subtext { get; set; }
    public string HighlightColor { get; set; } = "#1b2559";
}

public class DocumentTable
{
    public List<DocumentColumn> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public List<string>? TotalRow { get; set; }
}

public class DocumentColumn
{
    public string Header { get; set; } = string.Empty;
    public ColumnAlignment Alignment { get; set; } = ColumnAlignment.Left;
    public float RelativeWidth { get; set; } = 1.0f;
    public bool IsMonoFont { get; set; } = false;
    public bool IsBold { get; set; } = false;
    public string? CustomColor { get; set; }
}

public enum ColumnAlignment
{
    Left,
    Center,
    Right
}

public class DocumentTotals
{
    public decimal? SubTotal { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? RoundOff { get; set; }
    public decimal NetTotal { get; set; }
    public string Currency { get; set; } = "BHD";
}
