using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace TexasPortal.IntegrationTests;

public class TransactionExecutionTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SalesPortalDbContext _dbContext;
    private readonly TransactionalExecutor _executor;

    public TransactionExecutionTests(ITestOutputHelper output)
    {
        _output = output;

        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_Validation_Tx;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "TexasPortal_Validation_Tx";
            connString = builder.ConnectionString;
        }

        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseSqlServer(connString)
            .Options;

        _dbContext = new SalesPortalDbContext(options);
        _executor = new TransactionalExecutor(_dbContext);
    }

    [Fact]
    [Trait("Category", "TransactionExecution")]
    public async Task TransactionalExecutor_ShouldCommitChangesOnSuccess()
    {
        var ct = default(CancellationToken);

        var connStr = _dbContext.Database.GetDbConnection().ConnectionString;
        var isLocal = connStr.Contains("localhost") || connStr.Contains("(localdb)") || connStr.Contains("127.0.0.1") || connStr.Contains("SQLEXPRESS");
        if (isLocal)
        {
            await _dbContext.Database.EnsureDeletedAsync(ct);
        }
        await _dbContext.Database.MigrateAsync(ct);

        // Execute successful insert
        await _executor.ExecuteAsync(async (cancellationToken) =>
        {
            var user = new User
            {
                Name = "Transactional Salesman",
                Email = "transactional@texasportal.com",
                UserType = "Salesman",
                IsActive = true,
                Password = "hashedpassword",
                ProfitTarget = 0,
                MonthlyTarget = 0,
                ProfitGenerated = 0
            };
            _dbContext.UsersDbSet.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, ct);

        // Assert user was committed
        var exists = await _dbContext.UsersDbSet.AnyAsync(u => u.Email == "transactional@texasportal.com", ct);
        Assert.True(exists);
    }

    [Fact]
    [Trait("Category", "TransactionExecution")]
    public async Task TransactionalExecutor_ShouldRollbackChangesOnError()
    {
        var ct = default(CancellationToken);

        var connStr = _dbContext.Database.GetDbConnection().ConnectionString;
        var isLocal = connStr.Contains("localhost") || connStr.Contains("(localdb)") || connStr.Contains("127.0.0.1") || connStr.Contains("SQLEXPRESS");
        if (isLocal)
        {
            await _dbContext.Database.EnsureDeletedAsync(ct);
        }
        await _dbContext.Database.MigrateAsync(ct);

        // Execute operation that throws
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _executor.ExecuteAsync(async (cancellationToken) =>
            {
                var user = new User
                {
                    Name = "Failed Salesman",
                    Email = "failed@texasportal.com",
                    UserType = "Salesman",
                    IsActive = true,
                    Password = "hashedpassword",
                    ProfitTarget = 0,
                    MonthlyTarget = 0,
                    ProfitGenerated = 0
                };
                _dbContext.UsersDbSet.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException("Simulated transaction error");
            }, ct);
        });

        // Assert user was NOT committed
        var exists = await _dbContext.UsersDbSet.AnyAsync(u => u.Email == "failed@texasportal.com", ct);
        Assert.False(exists);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}
