using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TexasPortal.Infrastructure.Hubs;

[Authorize]
public class TexasSyncHub : Hub
{
    private readonly ILogger<TexasSyncHub> _logger;

    public TexasSyncHub(ILogger<TexasSyncHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? Context.User?.FindFirst("sub")?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value?.ToLowerInvariant();

        _logger.LogInformation("SignalR Client Connected. ConnectionId: {ConnectionId}, UserId: {UserId}, Role: {Role}",
            Context.ConnectionId, userId, role);

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        if (!string.IsNullOrEmpty(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "global:all");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(exception, "SignalR Client Disconnected. ConnectionId: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName.Trim());
        _logger.LogDebug("Connection {ConnectionId} subscribed to group {Group}", Context.ConnectionId, groupName);
    }

    public async Task UnsubscribeFromGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName.Trim());
        _logger.LogDebug("Connection {ConnectionId} unsubscribed from group {Group}", Context.ConnectionId, groupName);
    }

    public async Task SubscribeToCustomerLedger(long headId) =>
        await SubscribeToGroup($"ledger:customer:{headId}");

    public async Task UnsubscribeFromCustomerLedger(long headId) =>
        await UnsubscribeFromGroup($"ledger:customer:{headId}");

    public async Task SubscribeToWallet(long headId) =>
        await SubscribeToGroup($"wallet:{headId}");

    public async Task UnsubscribeFromWallet(long headId) =>
        await UnsubscribeFromGroup($"wallet:{headId}");

    public async Task SubscribeToBooking(long bookingId) =>
        await SubscribeToGroup($"booking:{bookingId}");

    public async Task UnsubscribeFromBooking(long bookingId) =>
        await UnsubscribeFromGroup($"booking:{bookingId}");

    public async Task SubscribeToDashboard(string portal) =>
        await SubscribeToGroup($"dashboard:{portal.ToLowerInvariant()}");

    public async Task UnsubscribeFromDashboard(string portal) =>
        await UnsubscribeFromGroup($"dashboard:{portal.ToLowerInvariant()}");
}
