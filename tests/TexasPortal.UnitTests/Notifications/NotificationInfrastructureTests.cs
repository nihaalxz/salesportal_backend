using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Common.Options;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Infrastructure.BackgroundServices;
using TexasPortal.Infrastructure.Notifications;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.UnitTests.Notifications;

public class NotificationInfrastructureTests
{
    private static SalesPortalDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SalesPortalDbContext(options);
    }

    [Fact]
    public void NotificationEnvelope_ShouldSetPropertiesCorrectly()
    {
        var envelope = new NotificationEnvelope
        {
            EventId = "evt_123",
            CorrelationId = "corr_456",
            EventName = BusinessEventNames.SalesEntriesChanged,
            ReferenceType = ReferenceTypes.Flight,
            ReferenceId = "999",
            HeadId = 42,
            Producer = "texas_sales_portal_backend",
            TargetGroup = SignalRGroups.Accounts
        };

        Assert.Equal("evt_123", envelope.EventId);
        Assert.Equal("corr_456", envelope.CorrelationId);
        Assert.Equal(BusinessEventNames.SalesEntriesChanged, envelope.EventName);
        Assert.Equal(ReferenceTypes.Flight, envelope.ReferenceType);
        Assert.Equal("999", envelope.ReferenceId);
        Assert.Equal(42, envelope.HeadId);
        Assert.Equal("texas_sales_portal_backend", envelope.Producer);
        Assert.Equal(SignalRGroups.Accounts, envelope.TargetGroup);
        Assert.Equal(1, envelope.PayloadVersion);
        Assert.Equal("Normal", envelope.Priority);
    }

    [Fact]
    public void ReferenceTypes_And_SignalRGroups_ShouldContainExpectedConstants()
    {
        Assert.Equal("Flight", ReferenceTypes.Flight);
        Assert.Equal("Hotel", ReferenceTypes.Hotel);
        Assert.Equal("Visa", ReferenceTypes.Visa);
        Assert.Equal("Vfs", ReferenceTypes.Vfs);
        Assert.Equal("Holiday", ReferenceTypes.Holiday);

        Assert.Equal("dashboard:sales", SignalRGroups.DashboardSales);
        Assert.Equal("dashboard:finance", SignalRGroups.DashboardFinance);
        Assert.Equal("dashboard:wallet", SignalRGroups.DashboardWallet);
        Assert.Equal("dashboard:summary", SignalRGroups.DashboardSummary);
        Assert.Equal("dashboard:receipts", SignalRGroups.DashboardReceipts);
        Assert.Equal("dashboard:operations", SignalRGroups.DashboardOperations);
        Assert.Equal("dashboard:user:88", SignalRGroups.DashboardUser(88));
    }

    [Fact]
    public async Task SingleEvent_DashboardTrigger_ShouldEnqueueSalesDashboardChanged()
    {
        using var db = CreateInMemoryDbContext();
        var options = Options.Create(new NotificationOptions { Enabled = true, ProducerName = "test" });
        var publisher = new NotificationPublisher(db, options, NullLogger<NotificationPublisher>.Instance);
        var coordinator = new DashboardRefreshCoordinator(publisher, NullLogger<DashboardRefreshCoordinator>.Instance);
        var salesService = new SalesEntriesNotificationService(publisher, coordinator);

        await salesService.PublishBookingCreatedAsync(ReferenceTypes.Flight, bookingId: 500);
        await db.SaveChangesAsync();

        var dispatcher = new TestDispatcher();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ISalesPortalDbContext>(db);
        serviceCollection.AddSingleton<INotificationDispatcher>(dispatcher);
        var provider = serviceCollection.BuildServiceProvider();

        var worker = new OutboxNotificationWorker(
            Options.Create(new WorkerOptions { Enabled = true }),
            provider,
            NullLogger<OutboxNotificationWorker>.Instance);

        await worker.ProcessPendingOutboxBatchAsync(CancellationToken.None);

        Assert.Equal(2, dispatcher.DispatchedEnvelopes.Count);
        Assert.Contains(dispatcher.DispatchedEnvelopes, e => e.EventName == BusinessEventNames.SalesEntriesChanged);
        Assert.Contains(dispatcher.DispatchedEnvelopes, e => e.EventName == BusinessEventNames.DashboardChanged && e.TargetGroup == "dashboard:sales");
    }

    [Fact]
    public async Task MultiWidget_MixedTransaction_ShouldTriggerFinanceAndWalletDashboards()
    {
        using var db = CreateInMemoryDbContext();
        var options = Options.Create(new NotificationOptions { Enabled = true, ProducerName = "test" });
        var publisher = new NotificationPublisher(db, options, NullLogger<NotificationPublisher>.Instance);
        var coordinator = new DashboardRefreshCoordinator(publisher, NullLogger<DashboardRefreshCoordinator>.Instance);
        var ledgerService = new LedgerNotificationService(publisher, coordinator);
        var walletService = new WalletNotificationService(publisher, coordinator, NullLogger<WalletNotificationService>.Instance);

        await ledgerService.PublishLedgerChangedAsync(ReferenceTypes.JournalEntry, "JRN-1", headId: 10);
        await walletService.PublishWalletChangedAsync(ReferenceTypes.Receipt, "RCT-1", ownerId: 10, ownerType: "customer");

        await db.SaveChangesAsync();

        var dispatcher = new TestDispatcher();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ISalesPortalDbContext>(db);
        serviceCollection.AddSingleton<INotificationDispatcher>(dispatcher);
        var provider = serviceCollection.BuildServiceProvider();

        var worker = new OutboxNotificationWorker(
            Options.Create(new WorkerOptions { Enabled = true }),
            provider,
            NullLogger<OutboxNotificationWorker>.Instance);

        await worker.ProcessPendingOutboxBatchAsync(CancellationToken.None);

        Assert.Contains(dispatcher.DispatchedEnvelopes, e => e.EventName == BusinessEventNames.DashboardChanged && e.TargetGroup == "dashboard:finance");
        Assert.Contains(dispatcher.DispatchedEnvelopes, e => e.EventName == BusinessEventNames.DashboardChanged && e.TargetGroup == "dashboard:wallet");
    }

    [Fact]
    public async Task DashboardTargetDeduplication_ShouldDeduplicateUniqueGroups()
    {
        using var db = CreateInMemoryDbContext();
        var options = Options.Create(new NotificationOptions { Enabled = true, ProducerName = "test" });
        var publisher = new NotificationPublisher(db, options, NullLogger<NotificationPublisher>.Instance);
        var coordinator = new DashboardRefreshCoordinator(publisher, NullLogger<DashboardRefreshCoordinator>.Instance);

        await coordinator.TriggerDashboardRefreshAsync(BusinessEventNames.SalesEntriesChanged, ReferenceTypes.Flight, "FLT-DEDUP");
        await coordinator.TriggerDashboardRefreshAsync(BusinessEventNames.SalesEntriesChanged, ReferenceTypes.Flight, "FLT-DEDUP");

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task DashboardRollback_ShouldNotPersistOutboxRows()
    {
        using var db = CreateInMemoryDbContext();
        var options = Options.Create(new NotificationOptions { Enabled = true, ProducerName = "test" });
        var publisher = new NotificationPublisher(db, options, NullLogger<NotificationPublisher>.Instance);
        var coordinator = new DashboardRefreshCoordinator(publisher, NullLogger<DashboardRefreshCoordinator>.Instance);

        await coordinator.TriggerDashboardRefreshAsync(BusinessEventNames.SalesEntriesChanged, ReferenceTypes.Flight, "FLT-FAIL");
        Assert.True(db.ChangeTracker.Entries<Domain.Entities.OutboxMessage>().Any());

        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.OutboxMessages.CountAsync());

        var dispatcher = new TestDispatcher();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ISalesPortalDbContext>(db);
        serviceCollection.AddSingleton<INotificationDispatcher>(dispatcher);
        var provider = serviceCollection.BuildServiceProvider();

        var worker = new OutboxNotificationWorker(
            Options.Create(new WorkerOptions { Enabled = true }),
            provider,
            NullLogger<OutboxNotificationWorker>.Instance);

        await worker.ProcessPendingOutboxBatchAsync(CancellationToken.None);
        Assert.Empty(dispatcher.DispatchedEnvelopes);
    }

    [Fact]
    public async Task DashboardConcurrency_100Items_ShouldProcessAtomically()
    {
        using var db = CreateInMemoryDbContext();
        var dispatcher = new TestDispatcher();
        var options = Options.Create(new NotificationOptions { Enabled = true, ProducerName = "test" });
        var publisher = new NotificationPublisher(db, options, NullLogger<NotificationPublisher>.Instance);
        var coordinator = new DashboardRefreshCoordinator(publisher, NullLogger<DashboardRefreshCoordinator>.Instance);

        var stopwatch = Stopwatch.StartNew();

        for (int i = 1; i <= 100; i++)
        {
            var eventName = (i % 3 == 0) ? BusinessEventNames.LedgerChanged : (i % 2 == 0) ? BusinessEventNames.WalletChanged : BusinessEventNames.SalesEntriesChanged;
            await coordinator.TriggerDashboardRefreshAsync(
                sourceEventName: eventName,
                referenceType: ReferenceTypes.Flight,
                referenceId: $"FLT-BATCH-{i}");
        }

        stopwatch.Stop();
        var enqueueMs = stopwatch.ElapsedMilliseconds;

        await db.SaveChangesAsync();

        Assert.Equal(100, await db.OutboxMessages.CountAsync());
        Assert.True(await db.OutboxMessages.AllAsync(m => m.Status == "pending"));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ISalesPortalDbContext>(db);
        serviceCollection.AddSingleton<INotificationDispatcher>(dispatcher);
        var provider = serviceCollection.BuildServiceProvider();

        var worker = new OutboxNotificationWorker(
            Options.Create(new WorkerOptions { Enabled = true, BatchSize = 200 }),
            provider,
            NullLogger<OutboxNotificationWorker>.Instance);

        stopwatch.Restart();
        await worker.ProcessPendingOutboxBatchAsync(CancellationToken.None);
        stopwatch.Stop();
        var dispatchMs = stopwatch.ElapsedMilliseconds;

        Assert.Equal(100, dispatcher.DispatchedEnvelopes.Count);
        Assert.Equal(100, dispatcher.DispatchedEnvelopes.Select(e => e.EventId).Distinct().Count());
        Assert.True(await db.OutboxMessages.AllAsync(m => m.Status == "delivered"));
        Assert.Equal(0, await db.OutboxMessages.CountAsync(m => m.Status == "pending"));
        Assert.Equal(0, await db.OutboxMessages.CountAsync(m => m.Status == "failed"));

        Console.WriteLine($"[DASHBOARD CONCURRENCY PERF TEST] Enqueue 100 DashboardChanged messages: {enqueueMs} ms");
        Console.WriteLine($"[DASHBOARD CONCURRENCY PERF TEST] Worker process & dispatch batch 100 DashboardChanged messages: {dispatchMs} ms");
    }

    private class TestDispatcher : INotificationDispatcher
    {
        public List<NotificationEnvelope> DispatchedEnvelopes { get; } = new();

        public Task DispatchAsync(NotificationEnvelope envelope, CancellationToken ct = default)
        {
            DispatchedEnvelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }
}
