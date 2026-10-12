using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Dashboard;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class SalesApiHttpTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanAId;
    private long _salesmanBId;
    private long _headAId;
    private long _headBId;

    public SalesApiHttpTests(SalesApiTestFactory factory)
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
            Email = "http.a@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesmanA);

        // Seed Salesman B
        var salesmanB = new User
        {
            Name = "Salesman B",
            Email = "http.b@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesmanB);
        await db.SaveChangesAsync();

        _salesmanAId = salesmanA.Id;
        _salesmanBId = salesmanB.Id;

        // Seed Customer Head owned by A
        var headA = new Head
        {
            CreatedBy = _salesmanAId,
            FirstName = "Alice",
            LastName = "A_Last",
            Email = "alice@example.com",
            MobileNo = "97311111111",
            Nationality = "Bahraini",
            Address = "Manama",
            Status = "active"
        };
        db.HeadsDbSet.Add(headA);

        // Seed Customer Head owned by B
        var headB = new Head
        {
            CreatedBy = _salesmanBId,
            FirstName = "Bob",
            LastName = "B_Last",
            Email = "bob@example.com",
            MobileNo = "97322222222",
            Nationality = "Saudi",
            Address = "Khobar",
            Status = "active"
        };
        db.HeadsDbSet.Add(headB);

        // Seed Lead owned by A
        var leadA = new Lead
        {
            CreatedBy = _salesmanAId,
            Name = "Lead A",
            CountryCode = "+973",
            MobileNo = "97333333333",
            Status = "new"
        };
        db.LeadsDbSet.Add(leadA);

        await db.SaveChangesAsync();

        _headAId = headA.Id;
        _headBId = headB.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeader(HttpRequestMessage request, long userId)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", userId.ToString());
    }

    [Fact]
    public async Task AnonymousRequest_Returns_401()
    {
        var response = await _client.GetAsync("/api/sales/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_Returns_DashboardSummary()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        SetAuthHeader(request, _salesmanAId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalHeads); // Only A's head counted
    }

    [Fact]
    public async Task DirectObjectAccess_Heads_Isolation()
    {
        // Salesman A querying Head B (should return 404 to avoid resource presence leakage)
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/sales/heads/{_headBId}");
        SetAuthHeader(request, _salesmanAId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Salesman A querying Head A (should succeed)
        var requestSuccess = new HttpRequestMessage(HttpMethod.Get, $"/api/sales/heads/{_headAId}");
        SetAuthHeader(requestSuccess, _salesmanAId);

        var responseSuccess = await _client.SendAsync(requestSuccess);
        Assert.Equal(HttpStatusCode.OK, responseSuccess.StatusCode);
    }

    [Fact]
    public async Task HeadsList_With_Pagination_And_Sorting()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/heads?page=1&pageSize=10&sortBy=lastName&sortOrder=asc");
        SetAuthHeader(request, _salesmanAId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pageData = await response.Content.ReadFromJsonAsync<PagedResponse<HeadSummaryResponse>>();
        Assert.NotNull(pageData);
        Assert.NotEmpty(pageData.Items);
        Assert.Contains(pageData.Items, x => x.FirstName == "Alice");
    }

    [Fact]
    public async Task HeadsList_InvalidSortingKey_Returns_ProblemDetails_400()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/heads?sortBy=malicious_sql_injection");
        SetAuthHeader(request, _salesmanAId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerOpenApiEndpoint_Is_Available()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var openApiContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Texas Sales Portal API", openApiContent);
    }
}
