using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Notifications;

namespace TexasPortal.Api.Services;

public sealed class LeadReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeadReminderBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public LeadReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LeadReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Lead Reminder Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueLeadRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred while processing lead reminders.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Lead Reminder Background Service stopped.");
    }

    private async Task ProcessDueLeadRemindersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISalesPortalDbContext>();

        var now = DateTime.UtcNow;
        var dueLeads = await db.Leads
            .Where(l => l.ReminderActive && l.ReminderDate != null && l.ReminderDate <= now && l.DeletedAt == null)
            .ToListAsync(ct);

        if (dueLeads.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} due lead reminders. Triggering notifications and emails...", dueLeads.Count);

        foreach (var lead in dueLeads)
        {
            try
            {
                await NotificationHelper.SendLeadReminderNotificationAsync(db, lead, ct);
                lead.ReminderActive = false;
                lead.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process reminder for Lead ID {LeadId}", lead.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
