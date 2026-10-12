using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using TexasPortal.Infrastructure.Persistence;

using System.Linq;

namespace TexasPortal.IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Authenticated", out var authVal) || authVal != "true")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = "0";
        if (Request.Headers.TryGetValue("X-Test-UserId", out var userIdVal))
        {
            userId = userIdVal.ToString();
        }

        // Allow test cases to supply a non-Salesman role to validate [Authorize(Roles = "Salesman")]
        var role = "Salesman";
        if (Request.Headers.TryGetValue("X-Test-Role", out var roleVal) && !string.IsNullOrWhiteSpace(roleVal))
        {
            role = roleVal.ToString();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class SalesApiTestFactory : WebApplicationFactory<Program>
{
    private static readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

    public string ConnectionString { get; private set; }

    public SalesApiTestFactory()
    {
        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_HTTP_Tests;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "TexasPortal_HTTP_Tests";
            connString = builder.ConnectionString;
        }
        ConnectionString = connString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the production DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SalesPortalDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Register database context to point to the HTTP-isolated catalog
            services.AddDbContext<SalesPortalDbContext>(options =>
            {
                options.UseSqlServer(ConnectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: new[] { 1205 });
                });
            });

            // Register Test Authentication Scheme and set it as default
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, options => { });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
            
            var isLocal = ConnectionString.Contains("localhost") || ConnectionString.Contains("(localdb)") || ConnectionString.Contains("127.0.0.1") || ConnectionString.Contains("SQLEXPRESS");
            if (isLocal)
            {
                Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
                try
                {
                    await db.Database.EnsureDeletedAsync();
                }
                catch
                {
                    try
                    {
                        await db.Database.ExecuteSqlRawAsync(@"
                            IF EXISTS (SELECT name FROM sys.databases WHERE name = 'TexasPortal_HTTP_Tests')
                            BEGIN
                                ALTER DATABASE [TexasPortal_HTTP_Tests] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                DROP DATABASE [TexasPortal_HTTP_Tests];
                            END;");
                    }
                    catch
                    {
                        // Ignore if already deleted
                    }
                }
            }
            await db.Database.MigrateAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }
}
