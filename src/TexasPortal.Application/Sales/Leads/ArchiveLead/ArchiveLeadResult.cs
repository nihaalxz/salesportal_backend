namespace TexasPortal.Application.Sales.Leads.ArchiveLead;

public enum ArchiveLeadResultStatus
{
    Success,
    NotFound
}

public sealed class ArchiveLeadResult
{
    public ArchiveLeadResultStatus Status { get; }

    private ArchiveLeadResult(ArchiveLeadResultStatus status)
    {
        Status = status;
    }

    public static ArchiveLeadResult Success() => new(ArchiveLeadResultStatus.Success);
    public static ArchiveLeadResult NotFound() => new(ArchiveLeadResultStatus.NotFound);
}
