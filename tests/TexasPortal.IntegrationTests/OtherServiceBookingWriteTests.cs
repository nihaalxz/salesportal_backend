using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class OtherServiceBookingWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;
    private long _headId;

    public OtherServiceBookingWriteTests(SalesApiTestFactory factory)
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
            Name = "Salesman A",
            Email = "write.salesman@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword",
            ProfitGenerated = 0m
        };
        db.UsersDbSet.Add(salesman);
        await db.SaveChangesAsync();
        _salesmanId = salesman.Id;

        // Seed Head
        var head = new Head
        {
            CreatedBy = _salesmanId,
            FirstName = "Alice",
            LastName = "Customer",
            Email = "alice.write@example.com",
            MobileNo = "97312345678",
            Nationality = "Bahraini",
            Address = "Manama",
            Status = "active",
            Wallet = 100.000m

        };
        db.HeadsDbSet.Add(head);

        // Seed initial Cash In Hand
        var cash = new CashInHand
        {
            UserId = _salesmanId,
            Amount = 10.000m,
            UpdatedAt = DateTime.UtcNow
        };
        db.CashInHandsDbSet.Add(cash);

        await db.SaveChangesAsync();
        _headId = head.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request, string idempotencyKey, string role = "Salesman")
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", _salesmanId.ToString());
        request.Headers.Add("X-Test-Role", role);
        if (idempotencyKey != null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
    }

    [Fact]
    public async Task AnonymousRequest_Returns_401()
    {
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            ServiceName: "Attestation",
            Narration: "Degree certificate attestation",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingIdempotencyKey_Returns_400()
    {
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            ServiceName: "Attestation",
            Narration: "Degree certificate attestation",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey: null!);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonSalesmanRole_Returns_403()
    {
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            ServiceName: "Attestation",
            Narration: "Degree certificate attestation",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), role: "Accountant");

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NegativeAmount_Returns_400_ValidationDetails()
    {
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: -60m, // Invalid negative money
            ServiceName: "Attestation",
            Narration: "Degree certificate attestation",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_CashMethod_EnforcesFinancialInvariants()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            ServiceName: "Attestation",
            Narration: "Degree certificate attestation",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var booking = await response.Content.ReadFromJsonAsync<OtherServiceBookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal(60.000m, booking.PayableAmount);
        Assert.Equal(20.000m, booking.Profit); // profit = amount - cost

        // Query DB directly to assert side-effects
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Assert Ledger Entry debit & credit
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceId == booking.Id && l.ServiceType == "Other");
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit); // Cash payment gives immediate credit

        // Assert Cash-In-Hand increased by 60.000 (starts at 10.000 -> 70.000)
        var cashRecord = await db.CashInHandsDbSet.FirstOrDefaultAsync(c => c.UserId == _salesmanId);
        Assert.NotNull(cashRecord);
        Assert.Equal(70.000m, cashRecord.Amount);

        // Assert Salesman profit incremented
        var userRecord = await db.UsersDbSet.FirstOrDefaultAsync(u => u.Id == _salesmanId);
        Assert.NotNull(userRecord);
        Assert.Equal(20.000m, userRecord.ProfitGenerated);
    }

    [Fact]
    public async Task CreateBooking_CreditMethod_UpdatesCustomerBalance()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 30.000m,
            Amount: 100.000m,
            ServiceName: "Translation",
            Narration: null,
            PaymentMethod: "credit",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify Head Wallet balance (initial 100 - 100 applied balance = 0)
        var headRecord = await db.HeadsDbSet.FirstOrDefaultAsync(h => h.Id == _headId);
        Assert.NotNull(headRecord);
        Assert.Equal(0.000m, headRecord.Wallet);

        // Assert Ledger debit=100, credit=100
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.HeadId == _headId && l.ServiceType == "Other");
        Assert.NotNull(ledger);
        Assert.Equal(100.000m, ledger.Debit);
        Assert.Equal(100.000m, ledger.Credit);
    }

    [Fact]
    public async Task CreateBooking_WalletMethod_DeductsWalletCorrectly()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 10.000m,
            Amount: 35.000m,
            ServiceName: "Visa Processing",
            Narration: null,
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Assert Wallet balance decrements from 100 to 65
        var headRecord = await db.HeadsDbSet.FirstOrDefaultAsync(h => h.Id == _headId);
        Assert.NotNull(headRecord);
        Assert.Equal(65.000m, headRecord.Wallet);
    }

    [Fact]
    public async Task CreateBooking_InsufficientWallet_Succeeds_And_DeductsWallet()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 10.000m,
            Amount: 150.000m, // Starts at 100, so 150 is insufficient
            ServiceName: "Attestation",
            Narration: null,
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var head = await db.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(head);
        Assert.Equal(-50.000m, head.Wallet);
    }

    [Fact]
    public async Task CreateBooking_IdempotencyReplay_Returns_CachedResponse()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            ServiceName: "Attestation",
            Narration: null,
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        // First call
        var httpReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq1, idempotencyKey);
        var res1 = await _client.SendAsync(httpReq1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        var booking1 = await res1.Content.ReadFromJsonAsync<OtherServiceBookingResponse>();

        // Replay call
        var httpReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq2, idempotencyKey);
        var res2 = await _client.SendAsync(httpReq2);
        Assert.Equal(HttpStatusCode.Created, res2.StatusCode);
        var booking2 = await res2.Content.ReadFromJsonAsync<OtherServiceBookingResponse>();

        // Assert both responses match and contain identical booking ID
        Assert.NotNull(booking1);
        Assert.NotNull(booking2);
        Assert.Equal(booking1.Id, booking2.Id);

        // Assert only ONE other service was written to DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var bookingCount = await db.OtherServicesDbSet.CountAsync(o => o.Name == "Jane Doe");
        Assert.Equal(1, bookingCount);
    }

    [Fact]
    public async Task CreateBooking_IdempotencyKeyReuseWithDifferentPayload_Returns_409()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request1 = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            ServiceName: "Attestation",
            Narration: null,
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        var request2 = new CreateOtherServiceBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Prefix: "Mr.",
            Name: "Jane Doe",
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 900m, // Different amount!
            ServiceName: "Attestation",
            Narration: null,
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            Status: "pending",
            PaymentProof: null
        );

        // First call
        var httpReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request1)
        };
        SetAuthHeaders(httpReq1, idempotencyKey);
        var res1 = await _client.SendAsync(httpReq1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        // Reuse key with different payload
        var httpReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/other")
        {
            Content = JsonContent.Create(request2)
        };
        SetAuthHeaders(httpReq2, idempotencyKey);
        var res2 = await _client.SendAsync(httpReq2);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }
}
