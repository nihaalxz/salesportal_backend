using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Heads.CreateHead;

public sealed class CreateHeadHandler
{
    private readonly ISalesPortalDbContext _db;

    public CreateHeadHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<CreateHeadResult> HandleAsync(
        CreateHeadCommand command,
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
            return CreateHeadResult.ValidationError(errors);

        // Check if email already exists in Heads table (unique constraint)
        var emailExists = await _db.Heads.AnyAsync(h => h.Email.ToLower() == request.Email.Trim().ToLower() && h.DeletedAt == null, ct);
        if (emailExists)
        {
            errors["email"] = ["Email address is already in use by another customer head."];
            return CreateHeadResult.ValidationError(errors);
        }

        // ── 2. Unique Customer Code Generation ─────────────────────────────────
        string userIdCode = "";
        var rand = new Random();
        bool isUnique = false;
        
        while (!isUnique)
        {
            var codeNum = rand.Next(100, 1000); // 100 to 999
            userIdCode = "TX" + codeNum;
            isUnique = !await _db.Heads.AnyAsync(h => h.UserId == userIdCode, ct);
        }

        // ── 3. Create Entity ──────────────────────────────────────────────────
        var head = new Head
        {
            CreatedBy = command.CurrentUserId,
            UserId = userIdCode,
            Type = typeLower,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MobileNo = request.MobileNo.Trim(),
            Email = request.Email.Trim(),
            Nationality = request.Nationality.Trim(),
            Address = request.Address.Trim(),
            Wallet = 0.000m,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Heads.Add(head);
        await _db.SaveChangesAsync(ct);

        // ── 4. Build Response ─────────────────────────────────────────────────
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

        return CreateHeadResult.Success(response);
    }
}
