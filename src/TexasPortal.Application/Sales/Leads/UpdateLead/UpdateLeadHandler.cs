using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Leads;

namespace TexasPortal.Application.Sales.Leads.UpdateLead;

public sealed class UpdateLeadHandler
{
    private readonly ISalesPortalDbContext _db;

    public UpdateLeadHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateLeadResult> HandleAsync(
        UpdateLeadCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string[]>();

        // ── 1. Validation ─────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Name))
            errors["name"] = ["Name is required."];
        else if (request.Name.Length > 255)
            errors["name"] = ["Name must not exceed 255 characters."];

        if (string.IsNullOrWhiteSpace(request.CountryCode))
            errors["countryCode"] = ["Country code is required."];
        else if (request.CountryCode.Length > 10)
            errors["countryCode"] = ["Country code must not exceed 10 characters."];

        if (string.IsNullOrWhiteSpace(request.MobileNo))
            errors["mobileNo"] = ["Mobile number is required."];
        else if (request.MobileNo.Length > 20)
            errors["mobileNo"] = ["Mobile number must not exceed 20 characters."];

        if (request.Services == null || request.Services.Length == 0)
            errors["services"] = ["At least one service must be selected."];

        if (string.IsNullOrWhiteSpace(request.LeadSource))
            errors["leadSource"] = ["Lead source is required."];

        // Allowed statuses: Laravel rules + seeded dev defaults
        var allowedStatuses = new[] { "Pending", "Completed", "Followed Up", "new", "contacted", "qualified", "converted", "lost" };
        if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            errors["status"] = [$"Status must be one of: {string.Join(", ", allowedStatuses)}."];

        if (errors.Count > 0)
            return UpdateLeadResult.ValidationError(errors);

        // Find Lead owned by the salesman
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == command.LeadId && l.CreatedBy == command.CurrentUserId && l.DeletedAt == null, ct);

        if (lead == null)
            return UpdateLeadResult.NotFound();

        // ── 2. Update Details ─────────────────────────────────────────────────
        lead.Name = request.Name.Trim();
        lead.Status = request.Status.Trim();
        lead.CountryCode = request.CountryCode.Trim();
        lead.MobileNo = request.MobileNo.Trim();
        lead.Services = JsonSerializer.Serialize(request.Services);
        lead.LeadSource = request.LeadSource.Trim();
        lead.Description = request.Description?.Trim();
        lead.ReminderDate = request.ReminderDate;
        lead.ReminderActive = request.ReminderDate.HasValue;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // ── 3. Build Response ─────────────────────────────────────────────────
        var response = new LeadDetailResponse
        {
            Id = lead.Id,
            Name = lead.Name,
            CountryCode = lead.CountryCode,
            MobileNo = lead.MobileNo,
            Services = lead.Services,
            LeadSource = lead.LeadSource,
            Status = lead.Status,
            ReminderActive = lead.ReminderActive,
            ReminderDate = lead.ReminderDate,
            CreatedAt = lead.CreatedAt,
            Description = lead.Description
        };

        return UpdateLeadResult.Success(response);
    }
}
