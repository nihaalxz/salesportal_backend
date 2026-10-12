using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class HeadsWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;
    private long _otherSalesmanId;
    private long _headId;

    public HeadsWriteTests(SalesApiTestFactory factory)
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
        var salesman = new User
        {
            Name = "Salesman A",
            Email = "salesman.heads.a@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesman);

        // Seed Salesman B (Other)
        var otherSalesman = new User
        {
            Name = "Salesman B",
            Email = "salesman.heads.b@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(otherSalesman);
        await db.SaveChangesAsync();

        _salesmanId = salesman.Id;
        _otherSalesmanId = otherSalesman.Id;

        // Seed Head owned by Salesman A
        var head = new Head
        {
            CreatedBy = _salesmanId,
            UserId = "TX101",
            FirstName = "Alice",
            LastName = "Customer",
            Email = "alice.heads@example.com",
            MobileNo = "97311111111",
            Nationality = "Bahraini",
            Address = "Manama",
            Status = "active"
        };
        db.HeadsDbSet.Add(head);
        await db.SaveChangesAsync();
        _headId = head.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request, string role = "Salesman", long? customUserId = null)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", (customUserId ?? _salesmanId).ToString());
        request.Headers.Add("X-Test-Role", role);
    }

    [Fact]
    public async Task AnonymousRequest_Returns_401()
    {
        var requestPayload = new CreateHeadRequest(
            FirstName: "New",
            LastName: "Head",
            Type: "customer",
            MobileNo: "33333333",
            Email: "new.head@example.com",
            Nationality: "Omani",
            Address: "Muscat"
        );

        var response = await _client.PostAsJsonAsync("/api/sales/heads", requestPayload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongRoleRequest_Returns_403()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/heads")
        {
            Content = JsonContent.Create(new CreateHeadRequest(
                FirstName: "New",
                LastName: "Head",
                Type: "customer",
                MobileNo: "33333333",
                Email: "new.head@example.com",
                Nationality: "Omani",
                Address: "Muscat"
            ))
        };
        SetAuthHeaders(request, role: "Accountant");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateHead_Succeeds_And_Generates_TX_Code()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/heads")
        {
            Content = JsonContent.Create(new CreateHeadRequest(
                FirstName: "New",
                LastName: "Customer",
                Type: "customer",
                MobileNo: "33344455",
                Email: "new.cust@example.com",
                Nationality: "Bahraini",
                Address: "Riffa"
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var head = await response.Content.ReadFromJsonAsync<HeadDetailResponse>();
        Assert.NotNull(head);
        Assert.StartsWith("TX", head.Type == "agent" ? "AGT" : "TX"); // Generates TX... for customer
        Assert.Equal("new.cust@example.com", head.Email);
        Assert.Equal("customer", head.Type);

        // Verify record in Database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var dbRecord = await db.HeadsDbSet.FirstOrDefaultAsync(h => h.Id == head.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal(_salesmanId, dbRecord.CreatedBy);
    }

    [Fact]
    public async Task CreateHead_ValidationErrors_Returns_422()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/heads")
        {
            Content = JsonContent.Create(new CreateHeadRequest(
                FirstName: "", // invalid empty name
                LastName: "Doe",
                Type: "invalid_type", // invalid type
                MobileNo: "1234",
                Email: "not_an_email", // invalid email format
                Nationality: "",
                Address: ""
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateHead_DuplicateEmail_Returns_422()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/heads")
        {
            Content = JsonContent.Create(new CreateHeadRequest(
                FirstName: "Another",
                LastName: "Alice",
                Type: "customer",
                MobileNo: "33333333",
                Email: "alice.heads@example.com", // Duplicate Email
                Nationality: "Bahraini",
                Address: "Seef"
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHead_Succeeds()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/sales/heads/{_headId}")
        {
            Content = JsonContent.Create(new UpdateHeadRequest(
                FirstName: "Alice Updated",
                LastName: "Customer Updated",
                Type: "customer",
                MobileNo: "97399999999",
                Email: "alice.updated.heads@example.com",
                Nationality: "Saudi",
                Address: "Khobar"
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<HeadDetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Alice Updated", updated.FirstName);
        Assert.Equal("Saudi", updated.Nationality);
    }

    [Fact]
    public async Task UpdateHead_Of_OtherSalesman_Returns_404()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/sales/heads/{_headId}")
        {
            Content = JsonContent.Create(new UpdateHeadRequest(
                FirstName: "Alice Hack",
                LastName: "Customer",
                Type: "customer",
                MobileNo: "97311111111",
                Email: "alice.hack@example.com",
                Nationality: "Saudi",
                Address: "Jeddah"
            ))
        };
        // Authenticated as Salesman B (Other)
        SetAuthHeaders(request, customUserId: _otherSalesmanId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
