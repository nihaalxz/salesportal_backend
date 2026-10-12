using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Leads.CreateLead;

public sealed class CreateLeadHandler
{
    private readonly ISalesPortalDbContext _db;

    public CreateLeadHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<CreateLeadResult> HandleAsync(
        CreateLeadCommand command,
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

        if (errors.Count > 0)
            return CreateLeadResult.ValidationError(errors);

        // ── 2. Create Entity ──────────────────────────────────────────────────
        var servicesJson = JsonSerializer.Serialize(request.Services);

        var lead = new Lead
        {
            CreatedBy = command.CurrentUserId,
            Name = request.Name.Trim(),
            CountryCode = request.CountryCode.Trim(),
            MobileNo = request.MobileNo.Trim(),
            Services = servicesJson,
            LeadSource = request.LeadSource.Trim(),
            Description = request.Description?.Trim(),
            Status = "Pending", // Force default Pending
            ReminderDate = request.ReminderDate,
            ReminderActive = request.ReminderDate.HasValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Leads.Add(lead);
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

        return CreateLeadResult.Success(response);
    }
}
