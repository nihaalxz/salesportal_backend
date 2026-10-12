using System;
using System.Collections.Generic;
using System.Linq;
using TexasPortal.Application.Features.Invoices.Builders;

namespace TexasPortal.Application.Features.Invoices.Factory;

public interface IInvoiceBuilderFactory
{
    IInvoiceBuilder? GetBuilder(string entityType);
}

public class InvoiceBuilderFactory : IInvoiceBuilderFactory
{
    private readonly IDictionary<string, IInvoiceBuilder> _builders;

    public InvoiceBuilderFactory(IEnumerable<IInvoiceBuilder> builders)
    {
        _builders = builders.ToDictionary(
            b => b.SupportedEntityType.ToLowerInvariant(),
            b => b,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public IInvoiceBuilder? GetBuilder(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType)) return null;

        var key = entityType.Trim().ToLowerInvariant().Replace("-", "").Replace("_", "");

        // Standardize common entity type alias variations
        if (key is "visa" or "visas" or "visaapplication" or "visaapplications" or "salesvisa") key = "visa";
        if (key is "flight" or "flights" or "flightbooking" or "flightbookings") key = "flight";
        if (key is "hotel" or "hotels" or "hotelbooking" or "hotelbookings") key = "hotel";
        if (key is "holiday" or "holidays" or "holidaypackage" or "holidaypackages") key = "holiday";
        if (key is "transport" or "transportation" or "transportations" or "transportbooking" or "transports") key = "transport";
        if (key is "vfs" or "vfsappointment" or "vfsappointments") key = "vfs";
        if (key is "other" or "others" or "otherservice" or "otherservices") key = "other";
        if (key is "receipt" or "receipts" or "paymentreceipt") key = "receipt";

        _builders.TryGetValue(key, out var builder);
        return builder;
    }
}
