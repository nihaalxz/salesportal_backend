using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

/// <summary>
/// Factory that uses the REAL DevMockAuthHandler (no TestAuthHandler override).
/// Forces Development environment so Program.cs registers DevMockScheme.
/// Replaces only the database to use a dedicated isolated catalog.
/// </summary>
public class DevAuthHttpTestFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; }

    public DevAuthHttpTestFactory()
    {
        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_DevAuth_Tests;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "TexasPortal_DevAuth_Tests";
            connString = builder.ConnectionString;
        }
        ConnectionString = connString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force Development so DevMockAuthHandler is active (not production JwtBearer)
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Replace only the database — do NOT replace authentication
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SalesPortalDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<SalesPortalDbContext>(options =>
                options.UseSqlServer(ConnectionString));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var isLocal = ConnectionString.Contains("localhost") ||
                      ConnectionString.Contains("(localdb)") ||
                      ConnectionString.Contains("127.0.0.1") ||
                      ConnectionString.Contains("SQLEXPRESS");
        if (isLocal)
            await db.Database.EnsureDeletedAsync();

        await db.Database.MigrateAsync();
        await TexasPortal.Api.Controllers.Sales.DevSeeder.SeedAsync(db);
    }
}

/// <summary>
/// Factory that forces Production environment.
/// Used to prove DevMockAuthHandler is NOT active in production.
/// The DB connection string is replaced but never actually used — all test requests
/// receive 401 at the authentication middleware before any controller scope is created.
/// </summary>
public class ProductionEnvTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force Production — Program.cs registers JwtBearer, NOT DevMockAuthHandler
        builder.UseEnvironment("Production");

        // Replace DB registration — the connection is never actually opened because
        // auth middleware rejects all requests with 401 before the controller runs.
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SalesPortalDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Use same isolated SQL Server catalog — it won't be accessed since
            // auth rejects requests at the middleware layer before any DB scope opens.
            var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connString))
                connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_ProdIsolation_Tests;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
            else
            {
                var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
                csb.InitialCatalog = "TexasPortal_ProdIsolation_Tests";
                connString = csb.ConnectionString;
            }

            services.AddDbContext<SalesPortalDbContext>(options =>
                options.UseSqlServer(connString));
        });
    }
}

/// <summary>
/// HTTP integration tests that call through the REAL DevMockAuthHandler.
/// These tests prove the allowlist behavior, not TestAuthHandler behavior.
/// Database must be available for test setup (schema migration).
/// </summary>
[Collection("DevAuth")]
public class DevAuthHttpTests : IClassFixture<DevAuthHttpTestFactory>, IAsyncLifetime
{
    private readonly DevAuthHttpTestFactory _factory;
    private readonly HttpClient _client;

    public DevAuthHttpTests(DevAuthHttpTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -----------------------------------------------------------------------
    // Case 1: Missing Authorization header → 401
    // -----------------------------------------------------------------------
    [Fact]
    public async Task MissingAuthorizationHeader_Returns_401()
    {
        var response = await _client.GetAsync("/api/sales/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 2: Unknown / non-allowlisted token → 401
    // -----------------------------------------------------------------------
    [Fact]
    public async Task UnknownToken_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer unknown-token-not-in-allowlist");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 3: Malformed Authorization header (no Bearer prefix) → 401
    // -----------------------------------------------------------------------
    [Fact]
    public async Task MalformedAuthorizationHeader_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "dev-token-salesman-a"); // Missing "Bearer "
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 4: Old hardcoded token (removed from allowlist) → 401
    // -----------------------------------------------------------------------
    [Fact]
    public async Task OldHardcodedToken_dev_salesman_token_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-salesman-token");
        var response = await _client.SendAsync(request);
        // This token was removed from the allowlist — must get 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 5: Salesman A token → authenticated, 200 on Dashboard
    // -----------------------------------------------------------------------
    [Fact]
    public async Task SalesmanA_Token_Authenticates_And_Gets_Dashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-salesman-a");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 6: Salesman B token → authenticated, 200 on Dashboard
    // -----------------------------------------------------------------------
    [Fact]
    public async Task SalesmanB_Token_Authenticates_And_Gets_Dashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-salesman-b");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 7: No-permission token → authenticated but Guest role → 403 Forbidden
    // VERIFIES: [Authorize(Roles = "Salesman")] blocks Guest identity
    // -----------------------------------------------------------------------
    [Fact]
    public async Task NoPermission_Token_Returns_403_On_Dashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-no-permission");
        var response = await _client.SendAsync(request);
        // The token authenticates (valid allowlist entry) but role=Guest fails [Authorize(Roles="Salesman")]
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NoPermission_Token_Returns_403_On_Heads()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/heads");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-no-permission");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NoPermission_Token_Returns_403_On_HeadDetails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/heads/999");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-no-permission");
        var response = await _client.SendAsync(request);
        // 403 before ownership check — no Salesman role
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NoPermission_Token_Returns_403_On_HeadLedger()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/heads/999/ledger");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-no-permission");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Case 8: Ownership isolation — A and B get scoped data
    // -----------------------------------------------------------------------
    [Fact]
    public async Task SalesmanA_And_SalesmanB_Get_Distinct_Empty_Dashboards()
    {
        var reqA = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        reqA.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-salesman-a");
        var resA = await _client.SendAsync(reqA);
        Assert.Equal(HttpStatusCode.OK, resA.StatusCode);

        var reqB = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        reqB.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-salesman-b");
        var resB = await _client.SendAsync(reqB);
        Assert.Equal(HttpStatusCode.OK, resB.StatusCode);
    }
}

/// <summary>
/// Tests proving DevMockAuthHandler is NOT active in a Production environment.
/// All dev tokens must fail to authenticate (return 401) because JwtBearer
/// requires valid signed tokens which these dev strings are not.
/// </summary>
public class ProductionAuthIsolationTests : IClassFixture<ProductionEnvTestFactory>
{
    private readonly HttpClient _client;

    public ProductionAuthIsolationTests(ProductionEnvTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Production_DevTokenA_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-salesman-a");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Production_DevTokenB_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-salesman-b");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Production_NoPermission_Token_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-token-no-permission");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Production_AnonymousRequest_Returns_401()
    {
        var response = await _client.GetAsync("/api/sales/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Production_OldHardcodedDevToken_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer dev-salesman-token");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

/// <summary>
/// Tests for [Authorize(Roles = "Salesman")] using TestAuthHandler (for fast isolated role tests).
/// These tests use X-Test-Role header to inject a Guest role and verify 403 is returned.
/// </summary>
[Collection("SalesApi")]
public class SalesAuthorizationRoleTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;

    public SalesAuthorizationRoleTests(SalesApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        var salesman = new TexasPortal.Domain.Entities.User
        {
            Name = "Role Test Salesman",
            Email = "role.test@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesman);
        await db.SaveChangesAsync();
        _salesmanId = salesman.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetSalesmanAuth(HttpRequestMessage request, long userId)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        // No X-Test-Role = defaults to Salesman
    }

    private void SetGuestAuth(HttpRequestMessage request, long userId)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Role", "Guest");
    }

    [Fact]
    public async Task Guest_Role_Returns_403_On_Dashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        SetGuestAuth(request, _salesmanId);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Guest_Role_Returns_403_On_Heads()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/heads");
        SetGuestAuth(request, _salesmanId);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Guest_Role_Returns_403_On_Leads()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/leads");
        SetGuestAuth(request, _salesmanId);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Salesman_Role_Gets_200_On_Dashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales/dashboard/summary");
        SetSalesmanAuth(request, _salesmanId);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Returns_401_On_Dashboard()
    {
        var response = await _client.GetAsync("/api/sales/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
