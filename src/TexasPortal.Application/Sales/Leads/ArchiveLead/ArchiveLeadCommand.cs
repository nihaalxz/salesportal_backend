namespace TexasPortal.Application.Sales.Leads.ArchiveLead;

public sealed record ArchiveLeadCommand(
    long CurrentUserId,
    long LeadId
);
