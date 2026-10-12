using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace TexasPortal.IntegrationTests;

public enum ValidationSeverity { Info, Warning, Error }

public record ValidationIssue(string Message, ValidationSeverity Severity, string TableName, string ColumnName = "");

public record ValidationReport(bool IsSuccess, List<ValidationIssue> Issues);

public interface ITargetSchemaValidator
{
    Task<ValidationReport> ValidateSchemaAsync(CancellationToken ct);
}

public class TargetSchemaValidator : ITargetSchemaValidator
{
    private readonly SalesPortalDbContext _dbContext;

    public TargetSchemaValidator(SalesPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ValidationReport> ValidateSchemaAsync(CancellationToken ct)
    {
        var issues = new List<ValidationIssue>();

        // 1. Verify Expected Tables exist
        var expectedTables = new[] { "users", "heads", "head_ledgers", "receipts", "flight_bookings" };
        using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
        
        await _dbContext.Database.OpenConnectionAsync(ct);
        var deployedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                deployedTables.Add(reader.GetString(0));
            }
        }
        await _dbContext.Database.CloseConnectionAsync();

        foreach (var expected in expectedTables)
        {
            if (!deployedTables.Contains(expected))
            {
                issues.Add(new ValidationIssue($"Expected table '{expected}' is missing from target database.", ValidationSeverity.Error, expected));
            }
        }

        // 2. Verify Financial Columns Precision (15, 3)
        using var colCmd = _dbContext.Database.GetDbConnection().CreateCommand();
        colCmd.CommandText = @"
            SELECT COLUMN_NAME, TABLE_NAME, NUMERIC_PRECISION, NUMERIC_SCALE 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE DATA_TYPE = 'decimal';";

        await _dbContext.Database.OpenConnectionAsync(ct);
        using (var reader = await colCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var colName = reader.GetString(0);
                var tableName = reader.GetString(1);
                var precision = Convert.ToInt32(reader.GetValue(2));
                var scale = Convert.ToInt32(reader.GetValue(3));

                if (scale != 3)
                {
                    issues.Add(new ValidationIssue($"Financial column '{colName}' on '{tableName}' has scale {scale} instead of 3.", ValidationSeverity.Error, tableName, colName));
                }
                if (precision != 15)
                {
                    issues.Add(new ValidationIssue($"Financial column '{colName}' on '{tableName}' has precision {precision} instead of 15.", ValidationSeverity.Warning, tableName, colName));
                }
            }
        }
        await _dbContext.Database.CloseConnectionAsync();

        // 3. Verify Unexpected Shadow Columns (e.g. Columns ending with 'Id1' from EF relationships)
        var shadowColumnsList = new List<string>();
        using var shadowCmd = _dbContext.Database.GetDbConnection().CreateCommand();
        shadowCmd.CommandText = "SELECT COLUMN_NAME, TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME LIKE '%Id1';";
        
        await _dbContext.Database.OpenConnectionAsync(ct);
        using (var reader = await shadowCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var col = reader.GetString(0);
                var tab = reader.GetString(1);
                issues.Add(new ValidationIssue($"Unexpected shadow relationship column '{col}' detected on table '{tab}'.", ValidationSeverity.Warning, tab, col));
            }
        }
        await _dbContext.Database.CloseConnectionAsync();

        var isSuccess = !issues.Any(x => x.Severity == ValidationSeverity.Error);
        return new ValidationReport(isSuccess, issues);
    }
}

public class MigrationValidationTests
{
    private readonly ITestOutputHelper _output;
    private readonly SalesPortalDbContext _dbContext;
    private readonly TargetSchemaValidator _validator;

    public MigrationValidationTests(ITestOutputHelper output)
    {
        _output = output;

        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_Validation_Valid;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "TexasPortal_Validation_Valid";
            connString = builder.ConnectionString;
        }

        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseSqlServer(connString)
            .Options;

        _dbContext = new SalesPortalDbContext(options);
        _validator = new TargetSchemaValidator(_dbContext);
    }

    [Fact]
    [Trait("Category", "MigrationValidation")]
    public async Task ValidationHarness_ShouldAuditDeployedSchema()
    {
        var ct = default(CancellationToken);
        
        // Ensure migrated
        await _dbContext.Database.MigrateAsync(ct);

        _output.WriteLine("Executing schema validation report...");
        var report = await _validator.ValidateSchemaAsync(ct);

        foreach (var issue in report.Issues)
        {
            _output.WriteLine($"[{issue.Severity}] Table: {issue.TableName}, Col: {issue.ColumnName} - {issue.Message}");
        }

        Assert.True(report.IsSuccess, "Schema validation harness reported errors in DDL configuration.");
    }
}
