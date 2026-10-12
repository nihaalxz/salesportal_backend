using System;

namespace TexasPortal.Contracts.Sales.Leads;

public sealed record CreateLeadRequest(
    string Name,
    string CountryCode,
    string MobileNo,
    string[] Services,
    string LeadSource,
    string? Description,
    DateTime? ReminderDate
);

public sealed record UpdateLeadRequest(
    string Name,
    string Status, // Pending, Completed, Followed Up
    string CountryCode,
    string MobileNo,
    string[] Services,
    string LeadSource,
    string? Description,
    DateTime? ReminderDate
);
