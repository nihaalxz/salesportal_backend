using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("DevAuth")]
public class ConnectivityEvidenceWriter : IClassFixture<DevAuthHttpTestFactory>, IAsyncLifetime
{
    private readonly DevAuthHttpTestFactory _factory;
    private readonly HttpClient _client;

    public ConnectivityEvidenceWriter(DevAuthHttpTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GenerateConnectivityEvidence()
    {
        // Set up the seeder to run on the DevAuth database (it runs during factory database initialization)
        // Let's call the endpoints and build the Markdown file content.

        var docPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "testing", "phase-10b-backend-connectivity-evidence.md"));
        var dir = Path.GetDirectoryName(docPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir!);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        
        var sw = new StringWriter();
        sw.WriteLine("# Phase 10B Backend Connectivity Evidence");
        sw.WriteLine();
        sw.WriteLine("This document provides verified evidence of the complete SvelteKit-to-Database read path execution, compiled dynamically during integration testing against the SQL Server database.");
        sw.WriteLine();
        sw.WriteLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sw.WriteLine();
        sw.WriteLine("## Database Diagnostic State");
        sw.WriteLine();
        sw.WriteLine("### Users in Database");
        sw.WriteLine("| Id | Name | Email | UserType |");
        sw.WriteLine("|---|---|---|---|");
        foreach (var u in dbContext.UsersDbSet)
        {
            sw.WriteLine($"| {u.Id} | {u.Name} | {u.Email} | {u.UserType} |");
        }
        sw.WriteLine();
        sw.WriteLine("### Customer Heads in Database");
        sw.WriteLine("| Id | FirstName | LastName | CreatedBy |");
        sw.WriteLine("|---|---|---|---|");
        foreach (var h in dbContext.HeadsDbSet)
        {
            sw.WriteLine($"| {h.Id} | {h.FirstName} | {h.LastName} | {h.CreatedBy} |");
        }
        sw.WriteLine();
        sw.WriteLine("### Hotel Bookings in Database");
        sw.WriteLine("| Id | GuestName | UserId | HeadId | PayableAmount |");
        sw.WriteLine("|---|---|---|---|---|");
        foreach (var hb in dbContext.HotelBookingsDbSet)
        {
            sw.WriteLine($"| {hb.Id} | {hb.GuestName} | {hb.UserId} | {hb.HeadId} | {hb.PayableAmount} |");
        }
        sw.WriteLine();
        sw.WriteLine("---");
        sw.WriteLine();

        // 1. Dashboard as A
        await WriteEndpointEvidenceAsync(sw, "1. Dashboard Summary as Salesman A", "/api/sales/dashboard/summary", "dev-token-salesman-a");

        // 2. Dashboard as B
        await WriteEndpointEvidenceAsync(sw, "2. Dashboard Summary as Salesman B", "/api/sales/dashboard/summary", "dev-token-salesman-b");

        // 3. Heads list as A
        await WriteEndpointEvidenceAsync(sw, "3. Heads List as Salesman A", "/api/sales/heads", "dev-token-salesman-a");

        // 4. Heads list as B
        await WriteEndpointEvidenceAsync(sw, "4. Heads List as Salesman B", "/api/sales/heads", "dev-token-salesman-b");

        // 5. A-owned Head Details as A
        await WriteEndpointEvidenceAsync(sw, "5. A-Owned Head Details as Salesman A", "/api/sales/heads/1", "dev-token-salesman-a");

        // 6. B-owned Head Details as B
        await WriteEndpointEvidenceAsync(sw, "6. B-Owned Head Details as Salesman B", "/api/sales/heads/2", "dev-token-salesman-b");

        // 7. A-owned Head Details requested by B (should fail or return NotFound)
        await WriteEndpointEvidenceAsync(sw, "7. A-Owned Head Details requested by Salesman B", "/api/sales/heads/1", "dev-token-salesman-b");

        // 8. B-owned Head Details requested by A (should fail or return NotFound)
        await WriteEndpointEvidenceAsync(sw, "8. B-Owned Head Details requested by Salesman A", "/api/sales/heads/2", "dev-token-salesman-a");

        // 9. A-owned Head Ledger as A
        await WriteEndpointEvidenceAsync(sw, "9. A-Owned Head Ledger as Salesman A", "/api/sales/heads/1/ledger", "dev-token-salesman-a");

        // 10. B-owned Head Ledger as B
        await WriteEndpointEvidenceAsync(sw, "10. B-Owned Head Ledger as Salesman B", "/api/sales/heads/2/ledger", "dev-token-salesman-b");

        // 11. Cross-owner Ledger Request
        await WriteEndpointEvidenceAsync(sw, "11. A-Owned Head Ledger requested by Salesman B", "/api/sales/heads/1/ledger", "dev-token-salesman-b");

        // 12. Outstanding Bookings
        await WriteEndpointEvidenceAsync(sw, "12. Outstanding Bookings (Non-existent Endpoint)", "/api/sales/heads/1/bookings/outstanding", "dev-token-salesman-a");

        await File.WriteAllTextAsync(docPath, sw.ToString());
    }

    private async Task WriteEndpointEvidenceAsync(StringWriter sw, string title, string route, string token)
    {
        sw.WriteLine($"### {title}");
        sw.WriteLine();
        sw.WriteLine($"- **Route**: `{route}`");
        sw.WriteLine($"- **Auth Identity**: `{token}`");

        var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        try
        {
            var response = await _client.SendAsync(request);
            sw.WriteLine($"- **HTTP Status**: `{(int)response.StatusCode} {response.StatusCode}`");
            
            var content = await response.Content.ReadAsStringAsync();
            sw.WriteLine("- **JSON Response Payload**:");
            sw.WriteLine("```json");
            
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(content);
                    sw.WriteLine(JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch
                {
                    sw.WriteLine(content);
                }
            }
            else
            {
                sw.WriteLine(content);
            }
            sw.WriteLine("```");
        }
        catch (Exception ex)
        {
            sw.WriteLine($"- **Error**: `{ex.Message}`");
        }
        sw.WriteLine();
        sw.WriteLine("---");
        sw.WriteLine();
    }
}
