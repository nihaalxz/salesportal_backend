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
using TexasPortal.Application.Sales.Bookings.Visas.CreateVisaBooking;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class VisaBookingWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanAId;
    private long _salesmanBId;
    private long _headAId;
    private long _headBId;

    public VisaBookingWriteTests(SalesApiTestFactory factory)
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
            Email = "salesman.a.visa@texasportal.com",
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
            Email = "salesman.b.visa@texasportal.com",
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
            Email = "alice.visa@example.com",
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
            Email = "bob.visa@example.com",
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
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 1,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoPermission_Returns403()
    {
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 1,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
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
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 1,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
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
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: null, // should fail validation when client type is customer
            Agent: null,
            AgentId: null,
            CountryId: 0, // invalid
            VisaPackId: 0, // invalid
            VisaPackName: null!, // invalid
            PaymentMethod: "invalidMethod", // invalid
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: null,
            Applicants: new List<VisaApplicantRequest>() // empty applicants list
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
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
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: 999999, // invalid HeadId
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 1,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
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
        // Salesman A attempts to book using Head B (which is owned/created by Salesman B)
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headBId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 1,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
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
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var res = await response.Content.ReadFromJsonAsync<VisaBookingResponse>();
        Assert.NotNull(res);
        Assert.True(res.Id > 0);
        Assert.Equal(60.000m, res.PayableAmount);
        Assert.Equal(40.000m, res.ExpectedSupplierCost);
        Assert.Equal(20.000m, res.Profit);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Verify booking in database
        var dbBooking = await db.SalesVisaApplicationsDbSet.FindAsync(res.Id);
        Assert.NotNull(dbBooking);
        Assert.Equal(_salesmanAId, dbBooking.UserId);
        Assert.Equal(_headAId, dbBooking.HeadId);

        // Verify applicants in database
        var dbApps = await db.SalesVisaApplicantsDbSet.Where(a => a.SalesVisaApplicationId == res.Id).ToListAsync();
        Assert.Single(dbApps);
        var dbApp = dbApps.First();
        Assert.Equal("FName", dbApp.FName);
        Assert.Equal(60.000m, dbApp.PayableAmount);
        Assert.Equal(40.000m, dbApp.ExpectedSupplierCost);
        Assert.Equal(20.000m, dbApp.Profit);
        Assert.False(dbApp.IsInhouse);

        // Verify ledger records (debit = 60, credit = 60)
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Visa" && l.ServiceId == res.Id);
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
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "credit",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var bookingResponse = await response.Content.ReadFromJsonAsync<VisaBookingResponse>();
        Assert.NotNull(bookingResponse);

        // Verify Head Wallet balance
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);
        

        // Verify ledger credit is 60
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Visa" && l.ServiceId == bookingResponse.Id);
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit);

        // Verify profit generated
        var salesman = await db.UsersDbSet.FindAsync(_salesmanAId);
        Assert.NotNull(salesman);
        Assert.Equal(20.000m, salesman.ProfitGenerated);
    }

    [Fact]
    public async Task WalletPayment_EnforcesWalletDeduction()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
        {
            Content = JsonContent.Create(request)
        };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var bookingResponse = await response.Content.ReadFromJsonAsync<VisaBookingResponse>();
        Assert.NotNull(bookingResponse);

        // Verify head wallet (initial 100 - 60 = 40)
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);

        // Verify ledger entries (debit = 60, credit = 60)
        var ledger = await db.HeadLedgersDbSet.FirstOrDefaultAsync(l => l.ServiceType == "Visa" && l.ServiceId == bookingResponse.Id);
        Assert.NotNull(ledger);
        Assert.Equal(60.000m, ledger.Debit);
        Assert.Equal(60.000m, ledger.Credit);
    }
    [Fact]
    public async Task InsufficientWallet_Succeeds_And_DeductsWallet()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 120.000m, 40.000m, false) // 120 is > 100 wallet
            }
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas")
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
    public async Task IdempotentReplay_ReturnsOriginalResult()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        // First request
        var httpReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq1, idempotencyKey, _salesmanAId);
        var response1 = await _client.SendAsync(httpReq1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var res1 = await response1.Content.ReadFromJsonAsync<VisaBookingResponse>();

        // Replay request
        var httpReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq2, idempotencyKey, _salesmanAId);
        var response2 = await _client.SendAsync(httpReq2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var res2 = await response2.Content.ReadFromJsonAsync<VisaBookingResponse>();

        Assert.Equal(res1!.Id, res2!.Id);
    }

    [Fact]
    public async Task IdempotencyConflict_Returns409()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request1 = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var request2 = request1 with { VisaPackName = "Premium Visa" };

        // Post request 1
        var httpReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request1) };
        SetAuthHeaders(httpReq1, idempotencyKey, _salesmanAId);
        var response1 = await _client.SendAsync(httpReq1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Replay request 2 with DIFFERENT payload and same key
        var httpReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request2) };
        SetAuthHeaders(httpReq2, idempotencyKey, _salesmanAId);
        var response2 = await _client.SendAsync(httpReq2);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task ConcurrentDuplicateRequest_CreatesOnce()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 3; i++)
        {
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request) };
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

        Assert.True(successCount >= 1);
        
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var dbBookings = await db.SalesVisaApplicationsDbSet
            .Where(b => b.HeadId == _headAId && b.UserId == _salesmanAId)
            .ToListAsync();
        Assert.Single(dbBookings);
    }

    [Fact]
    public async Task ConcurrentWalletPayments_AllSucceed_And_DeductWallet()
    {
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "wallet",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 3; i++)
        {
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request) };
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
        var head = await db.HeadsDbSet.FindAsync(_headAId);
        Assert.NotNull(head);
        Assert.Equal(40.000m, head.Wallet);
    }

    [Fact]
    public async Task ConcurrentProfitUpdates_DoNotLoseUpdates()
    {
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "FName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 4; i++)
        {
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request) };
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
    }[Fact]
    public async Task Failure_RollsBackStagedDatabaseState()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "TriggerRollbackException", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        // Verify initial state
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var initialBookingsCount = await db.SalesVisaApplicationsDbSet.CountAsync();
            var initialApplicantsCount = await db.SalesVisaApplicantsDbSet.CountAsync();
            var initialLedgerCount = await db.HeadLedgersDbSet.CountAsync();
            Assert.Equal(0, initialBookingsCount);
            Assert.Equal(0, initialApplicantsCount);
            Assert.Equal(0, initialLedgerCount);
        }

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(request) };
        SetAuthHeaders(httpReq, idempotencyKey, _salesmanAId);

        var response = await _client.SendAsync(httpReq);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // Verify rollback happened
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            var bookingsCount = await db.SalesVisaApplicationsDbSet.CountAsync();
            var applicantsCount = await db.SalesVisaApplicantsDbSet.CountAsync();
            var ledgerCount = await db.HeadLedgersDbSet.CountAsync();
            
            Assert.Equal(0, bookingsCount);
            Assert.Equal(0, applicantsCount);
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
            Applicants = new List<VisaApplicantRequest>
            {
                new("Mr.", "ValidFName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        };

        var httpReqRetry = new HttpRequestMessage(HttpMethod.Post, "/api/sales/bookings/visas") { Content = JsonContent.Create(retryRequest) };
        SetAuthHeaders(httpReqRetry, idempotencyKey, _salesmanAId);

        var retryResponse = await _client.SendAsync(httpReqRetry);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
    }

    [Fact]
    public async Task CancellationToken_PropagatesWherePractical()
    {
        var request = new CreateVisaBookingRequest(
            ClientType: "customer",
            HeadId: _headAId,
            Agent: null,
            AgentId: null,
            CountryId: 1,
            VisaPackId: 10,
            VisaPackName: "Tourist Visa",
            PaymentMethod: "cash",
            BankId: null,
            BankName: null,
            PaymentProof: null,
            PaymentReference: null,
            SupplierId: 2,
            Applicants: new List<VisaApplicantRequest>
            {
                new("Mr.", "ValidFName", "LName", DateTime.UtcNow.AddYears(-30), "Bahraini", null, "97312345", "test@test.com", "Passport123", null, 60.000m, 40.000m, false)
            }
        );

        using var cts = new System.Threading.CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel

        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateVisaBookingHandler>();
        var command = new CreateVisaBookingCommand(_salesmanAId, Guid.NewGuid().ToString(), request);

        // Verify task fails with OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await handler.HandleAsync(command, cts.Token);
        });
    }
}
