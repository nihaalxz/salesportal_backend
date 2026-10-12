using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Heads;

namespace TexasPortal.Application.Sales.Heads.UpdateHead;

public sealed class UpdateHeadHandler
{
    private readonly ISalesPortalDbContext _db;

    public UpdateHeadHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateHeadResult> HandleAsync(
        UpdateHeadCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string[]>();

        // ── 1. Validation ─────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["First name is required."];
        else if (request.FirstName.Length > 255)
            errors["firstName"] = ["First name must not exceed 255 characters."];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["Last name is required."];
        else if (request.LastName.Length > 255)
            errors["lastName"] = ["Last name must not exceed 255 characters."];

        var typeLower = (request.Type ?? "").ToLowerInvariant();
        if (typeLower != "customer" && typeLower != "agent")
            errors["type"] = ["Type must be 'customer' or 'agent'."];

        if (string.IsNullOrWhiteSpace(request.MobileNo))
            errors["mobileNo"] = ["Mobile number is required."];
        else if (request.MobileNo.Length > 20)
            errors["mobileNo"] = ["Mobile number must not exceed 20 characters."];

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email address is required."];
        }
        else
        {
            if (request.Email.Length > 255)
                errors["email"] = ["Email must not exceed 255 characters."];
            
            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors["email"] = ["Invalid email format."];
        }

        if (string.IsNullOrWhiteSpace(request.Nationality))
            errors["nationality"] = ["Nationality is required."];
        else if (request.Nationality.Length > 100)
            errors["nationality"] = ["Nationality must not exceed 100 characters."];

        if (string.IsNullOrWhiteSpace(request.Address))
            errors["address"] = ["Address is required."];

        if (errors.Count > 0)
            return UpdateHeadResult.ValidationError(errors);

        // Find Head owned by the salesman
        var head = await _db.Heads
            .FirstOrDefaultAsync(h => h.Id == command.HeadId && h.CreatedBy == command.CurrentUserId && h.DeletedAt == null, ct);

        if (head == null)
            return UpdateHeadResult.NotFound();

        // Check email uniqueness excluding this head ID
        var emailExists = await _db.Heads
            .AnyAsync(h => h.Id != command.HeadId && h.Email.ToLower() == request.Email.Trim().ToLower() && h.DeletedAt == null, ct);

        if (emailExists)
        {
            errors["email"] = ["Email address is already in use by another customer head."];
            return UpdateHeadResult.ValidationError(errors);
        }

        // ── 2. Update Details ─────────────────────────────────────────────────
        head.FirstName = request.FirstName.Trim();
        head.LastName = request.LastName.Trim();
        head.Type = typeLower;
        head.MobileNo = request.MobileNo.Trim();
        head.Email = request.Email.Trim();
        head.Nationality = request.Nationality.Trim();
        head.Address = request.Address.Trim();
        head.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // ── 3. Build Response ─────────────────────────────────────────────────
        var response = new HeadDetailResponse
        {
            Id = head.Id,
            FirstName = head.FirstName,
            LastName = head.LastName,
            Email = head.Email,
            MobileNo = head.MobileNo,
            Nationality = head.Nationality,
            Type = head.Type,
            Status = head.Status,
            Wallet = head.Wallet,
            Address = head.Address,
            CreatedAt = head.CreatedAt
        };

        return UpdateHeadResult.Success(response);
    }
}
