using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
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
public class HolidayBookingWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanAId;
    private long _salesmanBId;
    private long _headAId;
    private long _headBId;

    public HolidayBookingWriteTests(SalesApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Seed Salesman A
        var salesmanA = new User
        {
            Name = "Salesman A",
            Email = "salesman.a.holiday@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword",
            ProfitGenerated = 0m
        };
        db.UsersDbSet.Add(salesmanA);

        // Seed Salesman B
        var salesmanB = new User
        {
            Name = "Salesman B",
            Email = "salesman.b.holiday@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword",
            ProfitGenerated = 0m
        };
        db.UsersDbSet.Add(salesmanB);
        await db.SaveChangesAsync();

        _salesmanAId = salesmanA.Id;
        _salesmanBId = salesmanB.Id;

        // Seed Head for Salesman A
        var headA = new Head
        {
            CreatedBy = _salesmanAId,
            FirstName = "Alice",
            LastName = "Customer",
            Email = "alice.holiday@example.com",
            MobileNo = "97311111111",
            Nationality = "Bahraini",
            Address = "Manama",
            Status = "active",
            Wallet = 100.000m

        };
        db.HeadsDbSet.Add(headA);

        // Seed Head for Salesman B
        var headB = new Head
        {
            CreatedBy = _salesmanBId,
            FirstName = "Bob",
            LastName = "Customer",
            Email = "bob.holiday@example.com",
            MobileNo = "97322222222",
            Nationality = "Bahraini",
            Address = "Riffa",
            Status = "active",
            Wallet = 100.000m

        };
        db.HeadsDbSet.Add(headB);

        // Seed initial Cash In Hand for Salesman A
        var cashA = new CashInHand
        {
            UserId = _salesmanAId,
            Amount = 10.000m,
            UpdatedAt = DateTime.UtcNow
        };
        db.CashInHandsDbSet.Add(cashA);

        await db.SaveChangesAsync();
        _headAId = headA.Id;
        _headBId = headB.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request, string? idempotencyKey, long salesmanId, string role = "Salesman")
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", salesmanId.ToString());
        request.Headers.Add("X-Test-Role", role);
        if (idempotencyKey != null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
    }

    [Fact]
    public async Task Anonymous_Returns401()
    {
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 1,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoPermission_Returns403()
    {
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 1,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId, role: "Accountant");

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MissingIdempotencyKey_Returns400()
    {
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 1,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey: null, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_Returns400()
    {
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: null, // invalid
            Agent: null,
            AgentId: null,
            Prefix: "",
            PassengerName: "",
            SupplierId: null,
            Country: "",
            Days: -5, // invalid
            ExpectedSupplierCost: -10m, // invalid
            Amount: -20m, // invalid
            PaymentMethod: "invalidMethod", // invalid
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HeadNotFound_ReturnsExpectedError()
    {
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: 999999, // invalid HeadId
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 1,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CrossSalesmanHead_ReturnsBlocked()
    {
        // Salesman A attempts to book using Head B
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headBId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 1,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CashPayment_CreatesBooking_And_EnforcesFinancialInvariants()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 2,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var res = await response.Content.ReadFromJsonAsync<HolidayPackageResponse>();
        Assert.NotNull(res);
        Assert.True(res.Id > 0);
        Assert.Equal(60.000m, res.PayableAmount);
        Assert.Equal(40.000m, res.ExpectedSupplierCost);
        Assert.Equal(20.000m, res.Profit);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify booking in database
        var dbBooking = await db.HolidayPackagesDbSet.FindAsync(res.Id);
        Assert.NotNull(dbBooking);
        Assert.Equal(_salesmanAId, dbBooking.CreatedBy);
        Assert.Equal(_headAId, dbBooking.HeadId);

        // Verify ledger records (debit = 60, credit = 60)
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Holiday" && l.ServiceId == res.Id);
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit);

        // Verify Cash In Hand (initial 10 + 60 = 70)
        var cash = await db.CashInHandsDbSet.FirstOrDefaultAsync(c => c.UserId == _salesmanAId);
        Assert.NotNull(cash);
        Assert.Equal(70.000m, cash.Amount);

        // Verify Salesman Profit Generated (0 + 20 = 20)
        var salesman = await db.UsersDbSet.FindAsync(_salesmanAId);
        Assert.NotNull(salesman);
        Assert.Equal(20.000m, salesman.ProfitGenerated);
    }

    [Fact]
    public async Task CreditPayment_EnforcesFinancialInvariants()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 2,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "credit",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var bookingResponse = await response.Content.ReadFromJsonAsync<HolidayPackageResponse>();
        Assert.NotNull(bookingResponse);

        // Verify Head Wallet balance
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);
        

        // Verify ledger credit is 60
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Holiday" && l.ServiceId == bookingResponse.Id);
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit);
    }

    [Fact]
    public async Task WalletPayment_EnforcesWalletDeduction()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 2,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "wallet",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var bookingResponse = await response.Content.ReadFromJsonAsync<HolidayPackageResponse>();
        Assert.NotNull(bookingResponse);

        // Verify head wallet (initial 100 - 60 = 40)
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);
    }
    [Fact]
    public async Task InsufficientWallet_Succeeds_And_DeductsWallet()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "John Doe",
            SupplierId: 2,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 120.000m, // 120 is > 100 wallet
            PaymentMethod: "wallet",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(-20.000m, head.Wallet);
    }


    [Fact]
    public async Task Failure_RollsBackStagedDatabaseState()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHolidayPackageRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            Prefix: "Mr.",
            PassengerName: "TriggerRollbackException",
            SupplierId: 2,
            Country: "Georgia",
            Days: 5,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            PaymentMethod: "cash",
            PaymentProof: null);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/holidays")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var booking = await db.HolidayPackagesDbSet.FirstOrDefaultAsync(b => b.PassengerName == "TriggerRollbackException");
        Assert.Null(booking);
    }
}
