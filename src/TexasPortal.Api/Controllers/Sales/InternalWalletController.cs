using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Domain.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace TexasPortal.Api.Controllers.Sales
{
    [ApiController]
    [Route("api/internal/wallet")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "OperationsServiceAccess")]
    [Produces("application/json")]
    public sealed class InternalWalletController : ControllerBase
    {
        private ISalesPortalDbContext _db => HttpContext.RequestServices.GetRequiredService<ISalesPortalDbContext>();
        private ILedgerNotificationService _ledgerNotificationService => HttpContext.RequestServices.GetRequiredService<ILedgerNotificationService>();
        private IWalletNotificationService _walletNotificationService => HttpContext.RequestServices.GetRequiredService<IWalletNotificationService>();
        private IOutstandingNotificationService _outstandingNotificationService => HttpContext.RequestServices.GetRequiredService<IOutstandingNotificationService>();

        public InternalWalletController()
        {
        }

        public record WalletTransferRequest(decimal Amount, string Description);

        [HttpPost("heads/{id:long}/credit")]
        public async Task<IActionResult> CreditHeadWallet(long id, [FromBody] WalletTransferRequest request, CancellationToken ct)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            var head = await _db.Heads.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (head == null)
                return NotFound($"Customer Head with ID {id} not found.");

            head.Wallet += request.Amount;

            var isRefundOrReturn = request.Description != null &&
                (request.Description.Contains("Refund", StringComparison.OrdinalIgnoreCase) ||
                 request.Description.Contains("Return", StringComparison.OrdinalIgnoreCase));

            var serviceType = isRefundOrReturn ? "sales_return" : "journal_entry";

            var serviceId = DateTime.UtcNow.Ticks;
            _db.HeadLedgers.Add(new HeadLedger
            {
                HeadId = id,
                ServiceType = serviceType,
                ServiceId = serviceId,
                Description = request.Description ?? "Journal Entry Wallet Credit",
                Debit = 0,
                Credit = request.Amount,
                CreatedAt = DateTime.UtcNow
            });

            var refType = (request.Description != null && request.Description.Contains("Reversal", StringComparison.OrdinalIgnoreCase))
                ? ReferenceTypes.JournalReversal
                : ReferenceTypes.JournalEntry;

            await _ledgerNotificationService.PublishLedgerChangedAsync(
                referenceType: refType,
                referenceId: serviceId.ToString(),
                headId: id,
                cancellationToken: ct);

            await _walletNotificationService.PublishWalletChangedAsync(
                referenceType: refType,
                referenceId: serviceId.ToString(),
                ownerId: id,
                ownerType: string.Equals(head.Type, "agent", StringComparison.OrdinalIgnoreCase) ? "agent" : "customer",
                cancellationToken: ct);

            await _outstandingNotificationService.PublishOutstandingChangedAsync(
                referenceType: refType,
                referenceId: serviceId.ToString(),
                ownerId: id,
                ownerType: string.Equals(head.Type, "agent", StringComparison.OrdinalIgnoreCase) ? "agent" : "customer",
                cancellationToken: ct);

            await _db.SaveChangesAsync(ct);
            return Ok(new { Success = true, WalletBalance = head.Wallet });
        }

        [HttpPost("heads/{id:long}/debit")]
        public async Task<IActionResult> DebitHeadWallet(long id, [FromBody] WalletTransferRequest request, CancellationToken ct)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            var head = await _db.Heads.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (head == null)
                return NotFound($"Customer Head with ID {id} not found.");

            head.Wallet -= request.Amount;

            var isRefundOrReturn = request.Description != null &&
                (request.Description.Contains("Refund", StringComparison.OrdinalIgnoreCase) ||
                 request.Description.Contains("Return", StringComparison.OrdinalIgnoreCase));

            var serviceType = isRefundOrReturn ? "sales_return" : "journal_entry";

            var serviceId = DateTime.UtcNow.Ticks;
            _db.HeadLedgers.Add(new HeadLedger
            {
                HeadId = id,
                ServiceType = serviceType,
                ServiceId = serviceId,
                Description = request.Description ?? "Journal Entry Wallet Debit",
                Debit = request.Amount,
                Credit = 0,
                CreatedAt = DateTime.UtcNow
            });

            var refType = (request.Description != null && request.Description.Contains("Reversal", StringComparison.OrdinalIgnoreCase))
                ? ReferenceTypes.JournalReversal
                : ReferenceTypes.JournalEntry;

            await _ledgerNotificationService.PublishLedgerChangedAsync(
                referenceType: refType,
                referenceId: serviceId.ToString(),
                headId: id,
                cancellationToken: ct);

            await _walletNotificationService.PublishWalletChangedAsync(
                referenceType: refType,
                referenceId: serviceId.ToString(),
                ownerId: id,
                ownerType: string.Equals(head.Type, "agent", StringComparison.OrdinalIgnoreCase) ? "agent" : "customer",
                cancellationToken: ct);

            await _outstandingNotificationService.PublishOutstandingChangedAsync(
                referenceType: refType,
                referenceId: serviceId.ToString(),
                ownerId: id,
                ownerType: string.Equals(head.Type, "agent", StringComparison.OrdinalIgnoreCase) ? "agent" : "customer",
                cancellationToken: ct);

            await _db.SaveChangesAsync(ct);
            return Ok(new { Success = true, WalletBalance = head.Wallet });
        }
    }
}
