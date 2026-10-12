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
public class HotelBookingWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;
    private long _headId;

    public HotelBookingWriteTests(SalesApiTestFactory factory)
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
            Email = "write.salesman.hotel@texasportal.com",
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
            FirstName = "Bob",
            LastName = "Customer",
            Email = "bob.write.hotel@example.com",
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
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingIdempotencyKey_Returns_400()
    {
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
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
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40m,
            Amount: 60m,
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), role: "Accountant");

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_CashMethod_EnforcesFinancialInvariants()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<HotelBookingResponse>();
        Assert.NotNull(responseBody);
        Assert.True(responseBody.Id > 0);
        Assert.Equal(60.000m, responseBody.PayableAmount);
        Assert.Equal(40.000m, responseBody.ExpectedSupplierCost);
        Assert.Equal(20.000m, responseBody.Profit);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify booking in database
        var dbBooking = await db.HotelBookingsDbSet.FindAsync(responseBody.Id);
        Assert.NotNull(dbBooking);
        Assert.Equal(_salesmanId, dbBooking.UserId);
        Assert.Equal(_headId, dbBooking.HeadId);

        // Verify ledger entries (debit = 60, credit = 60)
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Hotel" && l.ServiceId == dbBooking.Id);
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit);

        // Verify Cash In Hand (initial 10 + 60 = 70)
        var cash = await db.CashInHandsDbSet.FirstOrDefaultAsync(c => c.UserId == _salesmanId);
        Assert.NotNull(cash);
        Assert.Equal(70.000m, cash.Amount);

        // Verify Salesman Profit Generated (0 + 20 = 20)
        var salesman = await db.UsersDbSet.FindAsync(_salesmanId);
        Assert.NotNull(salesman);
        Assert.Equal(20.000m, salesman.ProfitGenerated);
    }

    [Fact]
    public async Task CreateBooking_CreditMethod_DeductsHeadWallet()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "credit",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify Head Wallet balance (initial 100 - 60 applied balance = 40)
        var head = await db.HeadsDbSet.FindAsync(_headId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet); // Unaffected for credit
    }

    [Fact]
    public async Task CreateBooking_WalletMethod_DeductsHeadWallet()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40.000m,
            Amount: 60.000m,
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
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
        Assert.Equal(40.000m, head.Wallet); // Deducted 60 from 100
    }

    [Fact]
    public async Task CreateBooking_InsufficientWallet_Succeeds_And_DeductsWallet()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateHotelBookingRequest(
            ClientType: "customer",
            HeadId: _headId,
            Agent: null,
            AgentId: null,
            IsInhouse: false,
            SupplierId: 10,
            ExpectedSupplierCost: 40.000m,
            Amount: 120.000m, // Higher than wallet limit of 100
            Prefix: "Mr.",
            GuestName: "Bob Hotel",
            Country: "Bahrain",
            Days: 3,
            CheckInDate: DateTime.UtcNow.AddDays(5),
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            Status: "pending"
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/hotels")
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
        Assert.Equal(-20.000m, head.Wallet);
    }
}
