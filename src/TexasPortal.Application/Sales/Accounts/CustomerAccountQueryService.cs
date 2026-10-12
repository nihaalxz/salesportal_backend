using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Sales.Accounts;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Accounts;

public class CustomerAccountQueryService : ICustomerAccountQueryService
{
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;

    public CustomerAccountQueryService(ISalesPortalDbContext db, IAgentsApiClient agentsApiClient)
    {
        _db = db;
        _agentsApiClient = agentsApiClient;
    }

    public async Task<CustomerAccountSummaryResponse?> GetAccountSummaryAsync(long headId, HeadType headType = HeadType.Customer, CancellationToken ct = default)
    {
        // Agents live in the accounts portal, not in the local Heads table
        if (headType == HeadType.Agent)
        {
            var agent = await _agentsApiClient.GetAgentByIdAsync(headId, ct);
            if (agent == null) return null;

            var name = $"{agent.FirstName} {agent.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name)) name = agent.Email ?? $"Agent #{headId}";

            return new CustomerAccountSummaryResponse(
                HeadId: headId,
                HeadName: name,
                HeadType: "Agent",
                Balance: agent.Wallet,
                Currency: "BHD",
                LastUpdated: DateTime.UtcNow
            );
        }

        var head = await _db.Heads
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == headId && h.DeletedAt == null, ct);

        if (head == null) return null;

        var headName = $"{head.FirstName} {head.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(headName)) headName = head.Email ?? $"Head #{head.Id}";

        return new CustomerAccountSummaryResponse(
            HeadId: head.Id,
            HeadName: headName,
            HeadType: head.Type,
            Balance: head.Balance,
            Currency: "BHD",
            LastUpdated: head.UpdatedAt ?? head.CreatedAt
        );
    }

    public async Task<CustomerApplicationReceivableSummary> GetApplicationReceivablesSummaryAsync(long headId, HeadType headType = HeadType.Customer, CancellationToken ct = default)
    {
        var openReceivables = await _db.CreditReceivables
            .AsNoTracking()
            .Where(r => r.HeadId == headId && r.HeadType == headType && r.Status != ReceivableStatus.Closed && r.Status != ReceivableStatus.Cancelled)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(ct);

        var items = openReceivables.Select(r => new ApplicationReceivableItem(
            ReceivableId: r.Id,
            ApplicationNumber: r.BookingNumber,
            Module: r.BookingModule.ToString(),
            TotalAmount: r.TotalAmount,
            OutstandingAmount: r.CachedOutstandingAmount,
            PaymentStatus: r.Status.ToString(),
            CreatedAt: r.CreatedDate
        )).ToList();

        var totalOutstanding = items.Sum(i => i.OutstandingAmount);

        return new CustomerApplicationReceivableSummary(
            OpenApplications: items.Count,
            TotalOutstandingAmount: totalOutstanding,
            Items: items
        );
    }

    public async Task<List<AccountTimelineItemResponse>> GetAccountTimelineAsync(long headId, CancellationToken ct = default)
    {
        var head = await _db.Heads.AsNoTracking().FirstOrDefaultAsync(h => h.Id == headId, ct);
        var currentAccountBalance = head?.Balance ?? 0m;

        var ledgers = await _db.HeadLedgers
            .AsNoTracking()
            .Where(l => l.HeadId == headId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var orderedLedgers = ledgers.OrderBy(x => x.CreatedAt).ToList();
        var totalNetShift = orderedLedgers.Sum(l => l.Credit - l.Debit);
        var openingBalance = currentAccountBalance - totalNetShift;

        var result = new List<AccountTimelineItemResponse>();
        decimal running = openingBalance;

        foreach (var l in orderedLedgers)
        {
            running += (l.Credit - l.Debit);
            result.Add(new AccountTimelineItemResponse(
                Id: l.Id,
                TransactionDate: l.CreatedAt ?? DateTime.UtcNow,
                EventType: l.ServiceType ?? "Ledger",
                ReferenceNumber: $"L-{l.Id}",
                ServiceType: l.ServiceType ?? "General",
                Debit: l.Debit,
                Credit: l.Credit,
                RunningBalance: running,
                Description: l.Description ?? "",
                Remarks: null
            ));
        }

        return result;
    }

    public async Task<AccountStatementResponse?> GetAccountStatementAsync(long headId, CancellationToken ct = default)
    {
        var summary = await GetAccountSummaryAsync(headId, HeadType.Customer, ct);
        if (summary == null) return null;

        var timeline = await GetAccountTimelineAsync(headId, ct);
        var openCount = await _db.CreditReceivables
            .AsNoTracking()
            .Where(r => r.HeadId == headId && r.Status != ReceivableStatus.Closed && r.Status != ReceivableStatus.Cancelled)
            .CountAsync(ct);

        return new AccountStatementResponse(
            Summary: summary,
            Timeline: timeline,
            OpenReceivablesCount: openCount
        );
    }
}
