using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Disputes;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class DisputesWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;
    private long _headId;

    public DisputesWriteTests(SalesApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var salesman = new User
        {
            Name = "Dispute Test Salesman",
            Email = "salesman.disputes@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesman);
        await db.SaveChangesAsync();
        _salesmanId = salesman.Id;

        var head = new Head
        {
            CreatedBy = _salesmanId,
            UserId = "TXDISP01",
            FirstName = "Test",
            LastName = "Customer",
            Email = "customer.disputes@example.com",
            MobileNo = "97312345678",
            Nationality = "Bahraini",
            Address = "Manama",
            Wallet = 100.000m,
            Status = "active"
        };
        db.HeadsDbSet.Add(head);
        await db.SaveChangesAsync();
        _headId = head.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", _salesmanId.ToString());
        request.Headers.Add("X-Test-Role", "Salesman");
    }

    [Fact]
    public async Task Approved_Refund_Increases_Wallet_And_Creates_No_Ledger_Entries()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var head = await db.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(head);
        head.Wallet = -100.000m; // Starting at -100 (Customer owes 100)
        
        var dispute = new Dispute
        {
            UserId = _salesmanId,
            HeadId = _headId,
            DisputableId = 101,
            DisputableType = "FlightBooking",
            Subject = "Flight cancellation refund",
            Description = "Requested 30 BHD refund for cancelled flight",
            Status = "open",
            IsRefund = true,
            RefundAmount = 30.000m,
            RefundProcessed = false,
            CreatedAt = DateTime.UtcNow
        };
        db.DisputesDbSet.Add(dispute);
        await db.SaveChangesAsync();

        var requestPayload = new ResolveDisputeRequest(
            Action: "accepted",
            Resolution: "Refund approved per carrier policy",
            ResolvedBy: "Admin",
            ApprovedRefundAmount: 30.000m
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/disputes/{dispute.Id}/resolve")
        {
            Content = JsonContent.Create(requestPayload)
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedDispute = await response.Content.ReadFromJsonAsync<DisputeResponse>();
        Assert.NotNull(updatedDispute);
        Assert.Equal("resolved", updatedDispute.Status);
        Assert.True(updatedDispute.RefundProcessed);
        Assert.Equal(30.000m, updatedDispute.ApprovedRefundAmount);

        // Verify Wallet is increased by +30 from -100 to -70
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var updatedHead = await verifyDb.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(updatedHead);
        Assert.Equal(-70.000m, updatedHead.Wallet);

        // Verify NO HeadLedger postings were created
        var ledgerCount = await verifyDb.HeadLedgersDbSet.CountAsync(l => l.HeadId == _headId);
        Assert.Equal(0, ledgerCount);
    }

    [Fact]
    public async Task Rejected_Refund_Leaves_Wallet_Unchanged()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var head = await db.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(head);
        head.Wallet = 50.000m;

        var dispute = new Dispute
        {
            UserId = _salesmanId,
            HeadId = _headId,
            DisputableId = 102,
            DisputableType = "HotelBooking",
            Subject = "No-show refund request",
            Description = "Hotel no-show refund request",
            Status = "open",
            IsRefund = true,
            RefundAmount = 20.000m,
            RefundProcessed = false,
            CreatedAt = DateTime.UtcNow
        };
        db.DisputesDbSet.Add(dispute);
        await db.SaveChangesAsync();

        var requestPayload = new ResolveDisputeRequest(
            Action: "rejected",
            Resolution: "Non-refundable policy applies",
            ResolvedBy: "Admin"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/disputes/{dispute.Id}/resolve")
        {
            Content = JsonContent.Create(requestPayload)
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedDispute = await response.Content.ReadFromJsonAsync<DisputeResponse>();
        Assert.NotNull(updatedDispute);
        Assert.Equal("rejected", updatedDispute.Status);
        Assert.False(updatedDispute.RefundProcessed);

        // Verify Wallet remains unchanged at 50
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var updatedHead = await verifyDb.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(updatedHead);
        Assert.Equal(50.000m, updatedHead.Wallet);
    }

    [Fact]
    public async Task Non_Financial_Dispute_Leaves_Wallet_Unchanged()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var head = await db.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(head);
        head.Wallet = -20.000m;

        var dispute = new Dispute
        {
            UserId = _salesmanId,
            HeadId = _headId,
            DisputableId = 103,
            DisputableType = "OtherService",
            Subject = "Operational inquiry",
            Description = "Name correction on ticket",
            Status = "open",
            IsRefund = false, // Operational dispute
            RefundAmount = null,
            RefundProcessed = false,
            CreatedAt = DateTime.UtcNow
        };
        db.DisputesDbSet.Add(dispute);
        await db.SaveChangesAsync();

        var requestPayload = new ResolveDisputeRequest(
            Action: "accepted",
            Resolution: "Name updated in system",
            ResolvedBy: "Admin"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/disputes/{dispute.Id}/resolve")
        {
            Content = JsonContent.Create(requestPayload)
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedDispute = await response.Content.ReadFromJsonAsync<DisputeResponse>();
        Assert.NotNull(updatedDispute);
        Assert.Equal("resolved", updatedDispute.Status);
        Assert.False(updatedDispute.RefundProcessed);

        // Verify Wallet remains unchanged at -20
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var updatedHead = await verifyDb.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(updatedHead);
        Assert.Equal(-20.000m, updatedHead.Wallet);
    }
}
