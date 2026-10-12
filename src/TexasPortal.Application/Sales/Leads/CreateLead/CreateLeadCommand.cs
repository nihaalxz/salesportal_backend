using TexasPortal.Contracts.Sales.Leads;

namespace TexasPortal.Application.Sales.Leads.CreateLead;

public sealed record CreateLeadCommand(
    long CurrentUserId,
    CreateLeadRequest Request
);
