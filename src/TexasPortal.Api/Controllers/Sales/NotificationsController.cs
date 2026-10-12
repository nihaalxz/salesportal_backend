using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Notifications;

using Microsoft.Extensions.DependencyInjection;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/notifications")]
[Produces("application/json")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    private ISalesPortalDbContext _db => HttpContext.RequestServices.GetRequiredService<ISalesPortalDbContext>();

    public NotificationsController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    private bool TryGetUserId(out long userId)
    {
        userId = 0;
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null) return false;
        userId = _currentUser.UserId.Value;
        return true;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var list = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.NotifiableType == "User" && n.NotifiableId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse(
                n.Id,
                n.Type,
                n.NotifiableType,
                n.NotifiableId,
                n.Data,
                n.ReadAt,
                n.CreatedAt
            ))
            .ToListAsync(ct);

        // Patch legacy notifications: resolve document numbers for any entry
        // that still stores a raw numeric ID (e.g. "#27") in the message.
        var patched = new List<NotificationResponse>(list.Count);
        foreach (var notif in list)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(notif.Data ?? "{}");
                var root = doc.RootElement;

                var hasDocNum = root.TryGetProperty("documentNumber", out var docNumProp)
                    && docNumProp.ValueKind == System.Text.Json.JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(docNumProp.GetString());

                if (!hasDocNum
                    && root.TryGetProperty("serviceType", out var stProp)
                    && root.TryGetProperty("serviceId", out var sidProp)
                    && sidProp.TryGetInt64(out var serviceId))
                {
                    var serviceType = stProp.GetString() ?? "";
                    var resolvedDocNo = await TexasPortal.Application.Sales.Notifications.NotificationHelper
                        .GetDocumentNumberAsync(_db, serviceType, serviceId, ct);

                    // Re-serialize the payload with the resolved document number,
                    // and update the message text to replace "#<id>" with "#<docNo>".
                    var rawId = $"#{serviceId}";
                    var docDisplay = resolvedDocNo.StartsWith("#") ? resolvedDocNo : $"#{resolvedDocNo}";

                    var patchedData = new Dictionary<string, object?>();
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name == "message" && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                            patchedData[prop.Name] = prop.Value.GetString()!.Replace(rawId, docDisplay);
                        else
                            patchedData[prop.Name] = prop.Value.Clone();
                    }
                    patchedData["documentNumber"] = resolvedDocNo;

                    var newData = System.Text.Json.JsonSerializer.Serialize(patchedData);
                    patched.Add(new NotificationResponse(
                        notif.Id, notif.Type, notif.NotifiableType, notif.NotifiableId,
                        newData, notif.ReadAt, notif.CreatedAt));
                    continue;
                }
            }
            catch { /* malformed JSON – return as-is */ }

            patched.Add(notif);
        }

        return Ok(patched);
    }

    [HttpPost("{id}/read")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> MarkAsRead([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notif = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.NotifiableType == "User" && n.NotifiableId == userId, ct);

        if (notif == null) return NotFound();

        notif.ReadAt = DateTime.UtcNow;
        notif.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var unread = await _db.Notifications
            .Where(n => n.NotifiableType == "User" && n.NotifiableId == userId && n.ReadAt == null)
            .ToListAsync(ct);

        foreach (var notif in unread)
        {
            notif.ReadAt = DateTime.UtcNow;
            notif.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> DeleteNotification([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notif = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.NotifiableType == "User" && n.NotifiableId == userId, ct);

        if (notif == null) return NotFound();

        _db.Notifications.Remove(notif);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("clear-all")]
    [HttpDelete]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ClearAllNotifications(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notifs = await _db.Notifications
            .Where(n => n.NotifiableType == "User" && n.NotifiableId == userId)
            .ToListAsync(ct);

        if (notifs.Count > 0)
        {
            _db.Notifications.RemoveRange(notifs);
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

}
