using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TexasPortal.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace TexasPortal.IntegrationTests;

public class DatabaseConnectivityTests
{
    private readonly ITestOutputHelper _output;
    private readonly SalesPortalDbContext _dbContext;

    public DatabaseConnectivityTests(ITestOutputHelper output)
    {
        _output = output;

        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_Validation_Conn;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "TexasPortal_Validation_Conn";
            connString = builder.ConnectionString;
        }

        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseSqlServer(connString)
            .Options;

        _dbContext = new SalesPortalDbContext(options);
    }

    [Fact]
    [Trait("Category", "TargetSchema")]
    public async Task VerifyTargetSchema_OnDisposableDatabase()
    {
        var scratchPath = Path.Combine(Directory.GetCurrentDirectory(), "scratch_tables.txt");
        try
        {
            try
            {
                var connStr = _dbContext.Database.GetDbConnection().ConnectionString;
                var isLocal = connStr.Contains("localhost") || connStr.Contains("(localdb)") || connStr.Contains("127.0.0.1") || connStr.Contains("SQLEXPRESS");
                if (isLocal)
                {
                    await _dbContext.Database.EnsureDeletedAsync();
                }
                await _dbContext.Database.MigrateAsync();
            }
            catch (Exception ex) when (ex.Message.Contains("permission") || ex.Message.Contains("CREATE DATABASE") || ex.Message.Contains("requested by the login"))
            {
                _output.WriteLine("Direct MigrateAsync failed due to permission limits. Executing script fallback...");
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "schema_deploy.sql");
                if (!File.Exists(scriptPath))
                {
                    scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "schema_deploy.sql");
                }

                if (File.Exists(scriptPath))
                {
                    var scriptContent = await File.ReadAllTextAsync(scriptPath);
                    var batches = scriptContent.Split(new[] { "\r\nGO", "\nGO", "\r\ngo", "\ngo" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var batch in batches)
                    {
                        var trimmed = batch.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;
                        await _dbContext.Database.ExecuteSqlRawAsync(trimmed);
                    }
                    _output.WriteLine("Script fallback execution succeeded!");
                }
                else
                {
                    throw new FileNotFoundException("Deployment script schema_deploy.sql not found.", scriptPath);
                }
            }

            _output.WriteLine("Querying INFORMATION_SCHEMA to verify schema deployment...");
            using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;";
            await _dbContext.Database.OpenConnectionAsync();
            
            using var reader = await command.ExecuteReaderAsync();
            using var writer = new StreamWriter(scratchPath, false);
            writer.WriteLine("Deployed Tables:");
            
            var tables = new System.Collections.Generic.List<string>();
            while (await reader.ReadAsync())
            {
                var tableName = reader.GetString(0);
                writer.WriteLine($"- {tableName}");
                tables.Add(tableName);
            }

            _output.WriteLine($"Successfully verified {tables.Count} tables are deployed.");

            // Assert major business tables are present
            Assert.Contains("users", tables);
            Assert.Contains("heads", tables);
            Assert.Contains("head_ledgers", tables);
            Assert.Contains("receipts", tables);
            Assert.Contains("flight_bookings", tables);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Database schema validation failed: {ex}");
            File.WriteAllText(scratchPath, $"Error: {ex.ToString()}");
            throw;
        }
        finally
        {
            await _dbContext.Database.CloseConnectionAsync();
        }
    }

    [Fact]
    [Trait("Category", "TargetSchemaCompatibility")]
    public async Task VerifyEFModelCompatibility_QueryAllMappedTables()
    {
        // Execute read-only smoke checks against every table to verify translation
        var ct = default(System.Threading.CancellationToken);

        await _dbContext.UsersDbSet.AnyAsync(ct);
        await _dbContext.HeadsDbSet.AnyAsync(ct);
        await _dbContext.HeadLedgersDbSet.AnyAsync(ct);
        await _dbContext.ReceiptsDbSet.AnyAsync(ct);
        await _dbContext.FlightBookingsDbSet.AnyAsync(ct);
        await _dbContext.FlightPassengersDbSet.AnyAsync(ct);
        await _dbContext.HotelBookingsDbSet.AnyAsync(ct);
        await _dbContext.SalesVisaApplicationsDbSet.AnyAsync(ct);
        await _dbContext.SalesVisaApplicantsDbSet.AnyAsync(ct);
        await _dbContext.VfsAppointmentsDbSet.AnyAsync(ct);
        await _dbContext.HolidayPackagesDbSet.AnyAsync(ct);
        await _dbContext.TransportationsDbSet.AnyAsync(ct);
        await _dbContext.OtherServicesDbSet.AnyAsync(ct);
        await _dbContext.CashInHandsDbSet.AnyAsync(ct);
        await _dbContext.DepositsDbSet.AnyAsync(ct);
        await _dbContext.StaffMonthlyRecordsDbSet.AnyAsync(ct);
        await _dbContext.LeadsDbSet.AnyAsync(ct);
        await _dbContext.DisputesDbSet.AnyAsync(ct);
        await _dbContext.SalesVisaGovtFeesDbSet.AnyAsync(ct);
        await _dbContext.VfsAppointmentGovtFeesDbSet.AnyAsync(ct);
        await _dbContext.HolidayPackageGovtFeesDbSet.AnyAsync(ct);
        await _dbContext.HolidayPackageDocumentsDbSet.AnyAsync(ct);
        await _dbContext.NotificationsDbSet.AnyAsync(ct);

        _output.WriteLine("All 23 entities successfully verified query translation against target DB!");
    }
}
