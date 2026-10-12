using TexasPortal.Contracts.Sales.Leads;

namespace TexasPortal.Application.Sales.Leads.UpdateLead;

public sealed record UpdateLeadCommand(
    long CurrentUserId,
    long LeadId,
    UpdateLeadRequest Request
);
