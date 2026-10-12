using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Quotations;
using TexasPortal.Domain.Common;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Quotations.CreateQuotation;

public sealed class CreateQuotationHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly IDocumentNumberGenerator _documentNumberGenerator;

    public CreateQuotationHandler(
        ISalesPortalDbContext db,
        IDocumentNumberGenerator documentNumberGenerator)
    {
        _db = db;
        _documentNumberGenerator = documentNumberGenerator;
    }

    public async Task<CreateQuotationResult> HandleAsync(CreateQuotationCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            errors.Add("customerName", ["Client / Customer Name is required."]);
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            errors.Add("items", ["At least one service quotation item is required."]);
        }
        else
        {
            for (int i = 0; i < request.Items.Count; i++)
            {
                var it = request.Items[i];
                if (string.IsNullOrWhiteSpace(it.ServiceType))
                {
                    errors.Add($"items_{i}_service", [$"Item #{i + 1}: Service type is required."]);
                }
                if (string.IsNullOrWhiteSpace(it.Description))
                {
                    errors.Add($"items_{i}_desc", [$"Item #{i + 1}: Description is required."]);
                }
                if (it.UnitPrice < 0)
                {
                    errors.Add($"items_{i}_price", [$"Item #{i + 1}: Unit price cannot be negative."]);
                }
            }
        }

        if (errors.Count > 0)
        {
            return CreateQuotationResult.ValidationError(errors);
        }

        long? validHeadId = null;
        var isAgent = string.Equals(request.ClientType, "agent", StringComparison.OrdinalIgnoreCase);

        if (!isAgent && request.HeadId.HasValue)
        {
            var headExists = await _db.Heads.AnyAsync(h => h.Id == request.HeadId.Value, ct);
            if (headExists)
            {
                validHeadId = request.HeadId.Value;
            }
        }

        // Generate sequential document number TXSQT000001
        var documentNumber = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSQT", "QuotationSequence", ct);

        var quotation = new Quotation
        {
            DocumentNumber = documentNumber,
            UserId = command.UserId,
            HeadId = validHeadId,
            ClientType = isAgent ? "agent" : "customer",
            CustomerName = request.CustomerName.Trim(),
            CustomerContact = request.CustomerContact?.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            Remarks = request.Remarks?.Trim(),
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            TotalAmount = request.Items!.Sum(it => it.TotalAmount > 0 ? it.TotalAmount : (it.UnitPrice * Math.Max(1, it.Quantity)))
        };

        foreach (var it in request.Items!)
        {
            var qty = Math.Max(1, it.Quantity);
            var itemTotal = it.TotalAmount > 0 ? it.TotalAmount : (it.UnitPrice * qty);
            quotation.Items.Add(new QuotationItem
            {
                ServiceType = it.ServiceType.Trim().ToUpperInvariant(),
                Description = it.Description.Trim(),
                Quantity = qty,
                UnitPrice = it.UnitPrice,
                TotalAmount = itemTotal
            });
        }

        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync(ct);

        // Fetch salesman name
        var salesman = await _db.Users
            .Where(u => u.Id == command.UserId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct) ?? "Sales Representative";

        var response = new QuotationDetailResponse(
            quotation.Id,
            quotation.DocumentNumber,
            quotation.UserId,
            salesman,
            quotation.HeadId,
            quotation.ClientType,
            quotation.CustomerName,
            quotation.CustomerContact,
            quotation.CustomerEmail,
            quotation.TotalAmount,
            quotation.Status,
            quotation.Remarks,
            quotation.CreatedAt,
            quotation.Items.Select(i => new QuotationItemDto(
                i.Id,
                i.ServiceType,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.TotalAmount
            )).ToList()
        );

        return CreateQuotationResult.Success(response);
    }
}
