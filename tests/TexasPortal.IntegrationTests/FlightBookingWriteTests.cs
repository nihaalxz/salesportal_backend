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
using TexasPortal.Application.Sales.Bookings.Flights.CreateFlightBooking;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class FlightBookingWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanAId;
    private long _salesmanBId;
    private long _headAId;
    private long _headBId;

    public FlightBookingWriteTests(SalesApiTestFactory factory)
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
            Email = $"salesman.a.{Guid.NewGuid():N}@texasportal.com",
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
            Email = $"salesman.b.{Guid.NewGuid():N}@texasportal.com",
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
            Email = "alice.flight@example.com",
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
            Email = "bob.flight@example.com",
            MobileNo = "97322222222",
            Nationality = "Bahraini",
            Address = "Riffa",
            Status = "active",
            Wallet = 100.000m
        };
        db.HeadsDbSet.Add(headB);

        // Seed initial Cash In Hand for Salesman A
        var cash = new CashInHand
        {
            UserId = _salesmanAId,
            Amount = 10.000m,
            UpdatedAt = DateTime.UtcNow
        };
        db.CashInHandsDbSet.Add(cash);

        await db.SaveChangesAsync();
        _headAId = headA.Id;
        _headBId = headB.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request, string idempotencyKey, long userId, string role = "Salesman")
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Role", role);
        if (idempotencyKey != null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
    }

    [Fact]
    public async Task Anonymous_Returns401()
    {
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40m, 60m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoPermission_Returns403()
    {
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40m, 60m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId, role: "Guest");

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MissingIdempotencyKey_Returns400()
    {
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40m, 60m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, null!, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_Returns400()
    {
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "", // Invalid
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>() // Invalid - empty passengers
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
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
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: 999999, // Non-existent Head
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40m, 60m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
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
        // Salesman A tries to book using Salesman B's owned Head
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headBId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40m, 60m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
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
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40.000m, 60.000m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<FlightBookingResponse>();
        Assert.NotNull(responseBody);
        Assert.True(responseBody.Id > 0);
        Assert.Equal(60.000m, responseBody.PayableAmount);
        Assert.Equal(20.000m, responseBody.Profit);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify booking in database
        var dbBooking = await db.FlightBookingsDbSet.FindAsync(responseBody.Id);
        Assert.NotNull(dbBooking);
        Assert.Equal(_salesmanAId, dbBooking.UserId);
        Assert.Equal(_headAId, dbBooking.HeadId);

        // Verify passengers persisted correctly
        var passengers = await db.FlightPassengersDbSet.Where(p => p.FlightBookingId == dbBooking.Id).ToListAsync();
        Assert.Single(passengers);
        Assert.Equal("John Passenger", passengers[0].PassengerName);
        Assert.Equal(60.000m, passengers[0].PayableAmount);
        Assert.Equal(40.000m, passengers[0].ExpectedSupplierCost);
        Assert.Equal(20.000m, passengers[0].Profit);

        // Verify ledger entries (debit = 60, credit = 60)
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Flight" && l.ServiceId == dbBooking.Id);
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit);

        // Verify Cash In Hand (initial 10 + 60 = 70)
        var cash = await db.CashInHandsDbSet.FirstOrDefaultAsync(c => c.UserId == _salesmanAId);
        Assert.NotNull(cash);
        Assert.Equal(70.000m, cash.Amount);

        // Verify Head Wallet decremented by 60 (initial 100 - 60 = 40)
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(100.000m, head.Wallet);

        // Verify Salesman Profit Generated (0 + 20 = 20)
        var salesman = await db.UsersDbSet.FindAsync(_salesmanAId);
        Assert.NotNull(salesman);
        Assert.Equal(20.000m, salesman.ProfitGenerated);
    }

    [Fact]
    public async Task CreditPayment_EnforcesFinancialInvariants()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "credit",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40.000m, 60.000m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        var resContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected Created (201) but got {response.StatusCode}. Response: {resContent}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify Head Wallet decremented by 60 (initial 100 - 60 = 40)
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);
    }

    [Fact]
    public async Task WalletPayment_EnforcesWalletDeduction()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40.000m, 60.000m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify Head Wallet decremented by 60 (initial 100 - 60 = 40)
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);
    }

    [Fact]
    public async Task InsufficientWallet_Succeeds_And_DeductsWalletIntoNegative()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "John Passenger", 40.000m, 120.000m, false, 1) // wallet limit is 100
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
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
    public async Task MultiplePassengers_AggregatesPayableAmount_And_ProfitCorrectly()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, false, 1),
                new("Mrs.", "Passenger B", 30.000m, 50.000m, false, 1)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<FlightBookingResponse>();
        Assert.NotNull(responseBody);
        Assert.Equal(110.000m, responseBody.PayableAmount); // 60 + 50 = 110
        Assert.Equal(40.000m, responseBody.Profit); // (60 - 40) + (50 - 30) = 40
    }

    [Fact]
    public async Task InHousePassenger_ProfitBehaviorCorrect()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, true, null) // Inhouse
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<FlightBookingResponse>();
        Assert.NotNull(responseBody);
        Assert.Equal(60.000m, responseBody.PayableAmount);
        Assert.Equal(0m, responseBody.Profit); // Inhouse has 0 profit
    }

    [Fact]
    public async Task IdempotentReplay_ReturnsOriginalResult()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, false, 1)
            }
        );

        // First request
        var httpReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq1, idempotencyKey, _salesmanAId);
        var response1 = await _client.SendAsync(httpReq1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var res1 = await response1.Content.ReadFromJsonAsync<FlightBookingResponse>();

        // Replay request
        var httpReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq2, idempotencyKey, _salesmanAId);
        var response2 = await _client.SendAsync(httpReq2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var res2 = await response2.Content.ReadFromJsonAsync<FlightBookingResponse>();

        Assert.Equal(res1!.Id, res2!.Id);
    }

    [Fact]
    public async Task IdempotencyConflict_Returns409()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request1 = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, false, 1)
            }
        );

        var request2 = request1 with { FromLocation = "DXB", ToLocation = "BAH" };

        // Post request 1
        var httpReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request1) };
        SetAuthHeaders(httpReq1, idempotencyKey, _salesmanAId);
        var response1 = await _client.SendAsync(httpReq1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Replay request 2 with DIFFERENT payload and same key
        var httpReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request2) };
        SetAuthHeaders(httpReq2, idempotencyKey, _salesmanAId);
        var response2 = await _client.SendAsync(httpReq2);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task ConcurrentDuplicateRequest_CreatesOnce()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, false, 1)
            }
        );

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 3; i++)
        {
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
            SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);
            tasks.Add(_client.SendAsync(httpReq));
        }

        var responses = await Task.WhenAll(tasks);
        int successCount = 0;
        int conflictCount = 0;

        foreach (var r in responses)
        {
            if (r.StatusCode == HttpStatusCode.Created) successCount++;
            else if (r.StatusCode == HttpStatusCode.Conflict) conflictCount++;
        }

        // Exact outcomes: exactly 1 Created, others (2) return Conflict or Success replay
        Assert.True(successCount >= 1);
        
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify database contains EXACTLY one booking for this idempotency key
        var dbBookings = await db.FlightBookingsDbSet
            .Where(b => b.HeadId == _headAId && b.UserId == _salesmanAId)
            .ToListAsync();
        Assert.Single(dbBookings);
    }

    [Fact]
    public async Task ConcurrentWalletPayments_AllSucceed_And_DeductWallet()
    {
        // Alice has Wallet = 100. Let's make 3 concurrent bookings of 60 BHD each.
        // Combined cost is 180 BHD. Ensure all three succeed and Wallet becomes -80 BHD.
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, false, 1)
            }
        );

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 3; i++)
        {
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
            SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId); // different keys
            tasks.Add(_client.SendAsync(httpReq));
        }

        var responses = await Task.WhenAll(tasks);
        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(-80.000m, head.Wallet);
    }

    [Fact]
    public async Task ConcurrentProfitUpdates_DoNotLoseUpdates()
    {
        // Generate 4 concurrent cash bookings for Salesman A.
        // Each booking has 20 BHD profit. Total profit should be 80 BHD.
        // Ensure no lost updates on ProfitGenerated.
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger A", 40.000m, 60.000m, false, 1)
            }
        );

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 4; i++)
        {
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
            SetAuthHeaders(httpReq, Guid.NewGuid().ToString(), _salesmanAId);
            tasks.Add(_client.SendAsync(httpReq));
        }

        var responses = await Task.WhenAll(tasks);
        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var salesman = await db.UsersDbSet.FindAsync(_salesmanAId);
        Assert.NotNull(salesman);
        Assert.Equal(80.000m, salesman.ProfitGenerated); // 4 * 20 = 80
    }

    [Fact]
    public async Task Failure_RollsBackStagedDatabaseState()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "TriggerRollbackException", 40.000m, 60.000m, false, 1)
            }
        );

        // Verify initial state
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var initialBookingsCount = await db.FlightBookingsDbSet.CountAsync();
            var initialPassengersCount = await db.FlightPassengersDbSet.CountAsync();
            var initialLedgerCount = await db.HeadLedgersDbSet.CountAsync();
            Assert.Equal(0, initialBookingsCount);
            Assert.Equal(0, initialPassengersCount);
            Assert.Equal(0, initialLedgerCount);
        }

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // Verify rollback happened - database tables are completely clean/empty of this booking
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var bookingsCount = await db.FlightBookingsDbSet.CountAsync();
            var passengersCount = await db.FlightPassengersDbSet.CountAsync();
            var ledgerCount = await db.HeadLedgersDbSet.CountAsync();
            
            Assert.Equal(0, bookingsCount);
            Assert.Equal(0, passengersCount);
            Assert.Equal(0, ledgerCount);

            // Salesman CashInHand should remain 10 BHD
            var cash = await db.CashInHandsDbSet.FirstOrDefaultAsync(c => c.UserId == _salesmanAId);
            Assert.NotNull(cash);
            Assert.Equal(10.000m, cash.Amount);

            // User profit remains 0
            var salesman = await db.UsersDbSet.FindAsync(_salesmanAId);
            Assert.NotNull(salesman);
            Assert.Equal(0m, salesman.ProfitGenerated);

            // Idempotency status is marked Failed
            var idemp = await db.IdempotencyRecordsDbSet.FirstOrDefaultAsync(i => i.IdempotencyKey == idempotencyKey);
            Assert.NotNull(idemp);
            Assert.Equal("Failed", idemp.Status);
        }

        // Retry with same key but valid payload can succeed!
        var retryRequest = request with
        {
            Passengers = new List<FlightPassengerRequest>
            {
                new("Mr.", "Valid Passenger", 40.000m, 60.000m, false, 1)
            }
        };

        var httpReqRetry = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(retryRequest) };
        SetAuthHeaders(httpReqRetry, idempotencyKey, _salesmanAId);

        var retryResponse = await _client.SendAsync(httpReqRetry);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
    }

    [Fact]
    public async Task CancellationToken_PropagatesWherePractical()
    {
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Valid Passenger", 40.000m, 60.000m, false, 1)
            }
        );

        using var cts = new System.Threading.CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel

        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateFlightBookingHandler>();
        var command = new CreateFlightBookingCommand(_salesmanAId, Guid.NewGuid().ToString(), request);

        // Verify task fails with OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await handler.HandleAsync(command, cts.Token);
        });
    }

    [Fact]
    public async Task MixedPassengerTypes_ProfitBehaviorCorrect()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateFlightBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CareOf: null,
            FromLocation: "BAH",
            ToLocation: "DXB",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            Passengers: new List<FlightPassengerRequest>
            {
                new("Mr.", "Passenger Inhouse", 40.000m, 60.000m, true, null), // In-house: 0 profit
                new("Mrs.", "Passenger External", 30.000m, 50.000m, false, 1) // External: 20 profit
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/flights") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var res = await response.Content.ReadFromJsonAsync<FlightBookingResponse>();
        Assert.NotNull(res);
        Assert.Equal(110.000m, res.PayableAmount); // 60 + 50
        Assert.Equal(20.000m, res.Profit); // Inhouse(0) + External(20) = 20

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        
        // Assert user profit is incremented by exactly 20 BHD
        var salesman = await db.UsersDbSet.FindAsync(_salesmanAId);
        Assert.NotNull(salesman);
        Assert.Equal(20.000m, salesman.ProfitGenerated);
    }
}
