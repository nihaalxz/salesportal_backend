using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;

namespace TexasPortal.Application.Sales.Leads.ArchiveLead;

public sealed class ArchiveLeadHandler
{
    private readonly ISalesPortalDbContext _db;

    public ArchiveLeadHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<ArchiveLeadResult> HandleAsync(
        ArchiveLeadCommand command,
        CancellationToken ct = default)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == command.LeadId && l.CreatedBy == command.CurrentUserId && l.DeletedAt == null, ct);

        if (lead == null)
            return ArchiveLeadResult.NotFound();

        lead.DeletedAt = DateTime.UtcNow;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ArchiveLeadResult.Success();
    }
}
