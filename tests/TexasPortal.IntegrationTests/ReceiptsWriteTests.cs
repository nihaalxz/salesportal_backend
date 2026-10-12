using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Receipts;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class ReceiptsWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;
    private long _headId;
    private long _flightId1;
    private long _flightId2;

    public ReceiptsWriteTests(SalesApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Seed Salesman
        var salesman = new User
        {
            Name = "Salesman Receipts",
            Email = "salesman.receipts@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesman);
        await db.SaveChangesAsync();
        _salesmanId = salesman.Id;

        // Seed Head with BHD 1000 Wallet limit
        var head = new Head
        {
            CreatedBy = _salesmanId,
            FirstName = "Alice",
            LastName = "Green",
            Email = "alice.green@example.com",
            MobileNo = "33344455",
            Type = "customer",
            Status = "active",
            Wallet = 350.000m,
            Address = "Road 123, Bahrain",
            Nationality = "Bahraini",
            CreatedAt = DateTime.UtcNow
        };
        db.HeadsDbSet.Add(head);
        await db.SaveChangesAsync();
        _headId = head.Id;

        // Seed 2 Flight bookings for Alice
        var flight1 = new FlightBooking
        {
            UserId = _salesmanId,
            HeadId = _headId,
            FromLocation = "Bahrain",
            ToLocation = "Dubai",
            PayableAmount = 250.000m,
            PaymentStatus = "pending",
            ProcessingStatus = "confirmed",
            PaymentMethod = "cash",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.FlightBookingsDbSet.Add(flight1);

        var flight2 = new FlightBooking
        {
            UserId = _salesmanId,
            HeadId = _headId,
            FromLocation = "Bahrain",
            ToLocation = "London",
            PayableAmount = 400.000m,
            PaymentStatus = "pending",
            ProcessingStatus = "confirmed",
            PaymentMethod = "cash",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.FlightBookingsDbSet.Add(flight2);
        await db.SaveChangesAsync();

        _flightId1 = flight1.Id;
        _flightId2 = flight2.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", _salesmanId.ToString());
        request.Headers.Add("X-Test-Role", "Salesman");
    }

    private async Task<long> CreateTestHeadAsync(decimal wallet = 350.000m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var head = new Head
        {
            CreatedBy = _salesmanId,
            FirstName = "TestUser",
            LastName = Guid.NewGuid().ToString("N")[..6],
            Email = $"user_{Guid.NewGuid():N}@example.com",
            MobileNo = "33344455",
            Type = "customer",
            Status = "active",
            Wallet = wallet,
            Balance = wallet,
            Address = "Road 123, Bahrain",
            Nationality = "Bahraini",
            CreatedAt = DateTime.UtcNow
        };
        db.HeadsDbSet.Add(head);
        await db.SaveChangesAsync();
        return head.Id;
    }

    [Fact]
    public async Task CreateCashReceipt_StartsPending_DoesNotModifyBalance_UntilApproved()
    {
        var testHeadId = await CreateTestHeadAsync(350.000m);

        // 1. Create Pending Cash Receipt
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/receipts")
        {
            Content = JsonContent.Create(new CreateReceiptRequest(
                HeadId: testHeadId,
                ClientType: "customer",
                AgentName: null,
                Narration: "Cash deposit",
                Service: "deposit",
                BookingId: null,
                Amount: 500.000m,
                PaymentMode: "cash",
                BankId: null,
                BankName: null
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content.ReadFromJsonAsync<ReceiptDetailResponse>();
        Assert.NotNull(receipt);
        Assert.Equal("pending", receipt.Status);

        // Assert balance untouched before approval
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var dbHead = await db.HeadsDbSet.FindAsync(testHeadId);
            Assert.Equal(350.000m, dbHead!.Wallet);
        }

        // 2. Approve Cash Receipt
        var approveReq = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/receipts/{receipt.Id}/status")
        {
            Content = JsonContent.Create(new UpdateReceiptStatusRequest(Status: "success"))
        };
        SetAuthHeaders(approveReq);

        var approveRes = await _client.SendAsync(approveReq);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        // Assert balance increased by 500 (350 + 500 = 850)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var dbHead = await db.HeadsDbSet.FindAsync(testHeadId);
            Assert.Equal(850.000m, dbHead!.Wallet);

            var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.HeadId == testHeadId && l.Credit == 500);
            Assert.NotNull(ledger);
        }
    }

    [Fact]
    public async Task Scenario1_BalanceDeposit_FullFIFOAllocation_ClosesAllReceivables()
    {
        var testHeadId = await CreateTestHeadAsync(0m);

        // Seed 2 CreditReceivables (Flight: 50 BHD, Visa: 40 BHD, total debt = 90 BHD)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var rec1 = new CreditReceivable
            {
                BookingId = "1001",
                BookingNumber = "TXSFLT1001",
                BookingModule = Domain.Enums.BookingModule.Flight,
                HeadId = testHeadId,
                HeadType = Domain.Enums.HeadType.Customer,
                SalesmanId = _salesmanId,
                TotalAmount = 50.000m,
                CachedOutstandingAmount = 50.000m,
                Status = Domain.Enums.ReceivableStatus.Open,
                CreatedDate = DateTime.UtcNow.AddHours(-2)
            };
            var rec2 = new CreditReceivable
            {
                BookingId = "1002",
                BookingNumber = "TXSVFS1002",
                BookingModule = Domain.Enums.BookingModule.Visa,
                HeadId = testHeadId,
                HeadType = Domain.Enums.HeadType.Customer,
                SalesmanId = _salesmanId,
                TotalAmount = 40.000m,
                CachedOutstandingAmount = 40.000m,
                Status = Domain.Enums.ReceivableStatus.Open,
                CreatedDate = DateTime.UtcNow.AddHours(-1)
            };
            db.CreditReceivables.AddRange(rec1, rec2);
            await db.SaveChangesAsync();
        }

        // Create Deposit Receipt of BHD 90
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/receipts")
        {
            Content = JsonContent.Create(new CreateReceiptRequest(
                HeadId: testHeadId,
                ClientType: "customer",
                AgentName: null,
                Narration: "Exact FIFO Settlement",
                Service: "deposit",
                BookingId: null,
                Amount: 90.000m,
                PaymentMode: "cash",
                BankId: null,
                BankName: null
            ))
        };
        SetAuthHeaders(request);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptDetailResponse>();

        // Approve Receipt
        var approveReq = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/receipts/{receipt!.Id}/status")
        {
            Content = JsonContent.Create(new UpdateReceiptStatusRequest(Status: "success"))
        };
        SetAuthHeaders(approveReq);
        await _client.SendAsync(approveReq);

        // Assert both receivables are closed
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var recs = await db.CreditReceivables.Where(r => r.HeadId == testHeadId && (r.BookingId == "1001" || r.BookingId == "1002")).ToListAsync();
            Assert.All(recs, r => Assert.Equal(Domain.Enums.ReceivableStatus.Closed, r.Status));
            Assert.All(recs, r => Assert.Equal(0m, r.CachedOutstandingAmount));
        }
    }

    [Fact]
    public async Task Scenario2_BalanceDeposit_PartialFIFOAllocation_PartiallySettlesOldestReceivable()
    {
        var testHeadId = await CreateTestHeadAsync(0m);

        // Seed 2 CreditReceivables (Flight: 50 BHD, Visa: 40 BHD)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var rec1 = new CreditReceivable
            {
                BookingId = "2001",
                BookingNumber = "TXSFLT2001",
                BookingModule = Domain.Enums.BookingModule.Flight,
                HeadId = testHeadId,
                HeadType = Domain.Enums.HeadType.Customer,
                SalesmanId = _salesmanId,
                TotalAmount = 50.000m,
                CachedOutstandingAmount = 50.000m,
                Status = Domain.Enums.ReceivableStatus.Open,
                CreatedDate = DateTime.UtcNow.AddHours(-2)
            };
            var rec2 = new CreditReceivable
            {
                BookingId = "2002",
                BookingNumber = "TXSVFS2002",
                BookingModule = Domain.Enums.BookingModule.Visa,
                HeadId = testHeadId,
                HeadType = Domain.Enums.HeadType.Customer,
                SalesmanId = _salesmanId,
                TotalAmount = 40.000m,
                CachedOutstandingAmount = 40.000m,
                Status = Domain.Enums.ReceivableStatus.Open,
                CreatedDate = DateTime.UtcNow.AddHours(-1)
            };
            db.CreditReceivables.AddRange(rec1, rec2);
            await db.SaveChangesAsync();
        }

        // Create Partial Deposit Receipt of BHD 30
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/receipts")
        {
            Content = JsonContent.Create(new CreateReceiptRequest(
                HeadId: testHeadId,
                ClientType: "customer",
                AgentName: null,
                Narration: "Partial FIFO Settlement",
                Service: "deposit",
                BookingId: null,
                Amount: 30.000m,
                PaymentMode: "cash",
                BankId: null,
                BankName: null
            ))
        };
        SetAuthHeaders(request);
        var response = await _client.SendAsync(request);
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptDetailResponse>();

        // Approve Receipt
        var approveReq = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/receipts/{receipt!.Id}/status")
        {
            Content = JsonContent.Create(new UpdateReceiptStatusRequest(Status: "success"))
        };
        SetAuthHeaders(approveReq);
        await _client.SendAsync(approveReq);

        // Assert oldest receivable has 20 BHD remaining, newest untouched
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var rec1 = await db.CreditReceivables.FirstAsync(r => r.HeadId == testHeadId && r.BookingId == "2001");
            var rec2 = await db.CreditReceivables.FirstAsync(r => r.HeadId == testHeadId && r.BookingId == "2002");
            Assert.Equal(20.000m, rec1.CachedOutstandingAmount);
            Assert.Equal(Domain.Enums.ReceivableStatus.PartiallyPaid, rec1.Status);
            Assert.Equal(40.000m, rec2.CachedOutstandingAmount);
            Assert.Equal(Domain.Enums.ReceivableStatus.Open, rec2.Status);
        }
    }
}
