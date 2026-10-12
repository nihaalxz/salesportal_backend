using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace TexasPortal.IntegrationTests;

public class TargetReadSmokeTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SalesPortalDbContext _dbContext;

    public TargetReadSmokeTests(ITestOutputHelper output)
    {
        _output = output;

        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_Validation_Smoke;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            if (!connString.Contains("db59312"))
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
                builder.InitialCatalog = "TexasPortal_Validation_Smoke";
                connString = builder.ConnectionString;
            }
        }

        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseSqlServer(connString)
            .Options;

        _dbContext = new SalesPortalDbContext(options);
    }

    private async Task SeedSmokeDataAsync(CancellationToken ct)
    {
        var connStr = _dbContext.Database.GetDbConnection().ConnectionString;
        var isLocal = connStr.Contains("localhost") || connStr.Contains("(localdb)") || connStr.Contains("127.0.0.1") || connStr.Contains("SQLEXPRESS");
        if (isLocal)
        {
            await _dbContext.Database.EnsureDeletedAsync(ct);
        }
        await _dbContext.Database.MigrateAsync(ct);

        var user = new User
        {
            Name = "Test Salesman",
            Email = "salesman@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword",
            ProfitTarget = 1000.000m,
            MonthlyTarget = 2000.000m,
            ProfitGenerated = 150.000m
        };
        _dbContext.UsersDbSet.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        var head = new Head
        {
            CreatedBy = user.Id,
            UserId = "agent_123",
            Type = "agent",
            FirstName = "John",
            LastName = "Doe",
            MobileNo = "97312345678",
            Email = "john.doe@gmail.com",
            Nationality = "Bahraini",
            Wallet = 100.000m,
            Address = "Manama, Bahrain",
            Status = "active"
        };
        _dbContext.HeadsDbSet.Add(head);
        await _dbContext.SaveChangesAsync(ct);

        var booking = new FlightBooking
        {
            UserId = user.Id,
            HeadId = head.Id,
            ClientType = "agent",
            Agent = "John Agent",
            FromLocation = "BAH",
            ToLocation = "DXB",
            PaymentMethod = "cash",
            PaymentStatus = "success",
            ProcessingStatus = "completed",
            PayableAmount = 75.000m
        };
        _dbContext.FlightBookingsDbSet.Add(booking);

        var ledger = new HeadLedger
        {
            HeadId = head.Id,
            ServiceType = "FlightBooking",
            ServiceId = 1,
            Debit = 75.000m,
            Credit = 0.000m,
            Description = "Flight Booking Payment"
        };
        _dbContext.HeadLedgersDbSet.Add(ledger);

        var cash = new CashInHand
        {
            UserId = user.Id,
            Amount = 25.000m
        };
        _dbContext.CashInHandsDbSet.Add(cash);

        var monthly = new StaffMonthlyRecord
        {
            UserId = user.Id,
            Month = "July",
            Year = 2026,
            ProfitGenerated = 150.000m,
            Incentive = 50.000m,
            ProfitTarget = 1000.000m,
            MonthlyTarget = 2000.000m,
            TargetAchieved = false,
            BasicSalary = 500.000m,
            HousingAllowance = 100.000m,
            TransportAllowance = 50.000m
        };
        _dbContext.StaffMonthlyRecordsDbSet.Add(monthly);

        var lead = new Lead
        {
            CreatedBy = user.Id,
            Name = "Prospective Customer",
            CountryCode = "+973",
            MobileNo = "98765432",
            Status = "new",
            ReminderActive = false
        };
        _dbContext.LeadsDbSet.Add(lead);

        await _dbContext.SaveChangesAsync(ct);
    }

    [Fact]
    [Trait("Category", "TargetReadSmoke")]
    public async Task VerifyTargetReadSmokeProjections()
    {
        var ct = default(CancellationToken);
        await SeedSmokeDataAsync(ct);

        _output.WriteLine("Executing bounded projections with AsNoTracking...");

        // 1. User
        var userProj = await _dbContext.UsersDbSet.AsNoTracking()
            .Select(u => new { u.Id, u.Name, u.Email, u.ProfitTarget })
            .FirstOrDefaultAsync(u => u.Email == "salesman@texasportal.com", ct);
        Assert.NotNull(userProj);
        Assert.Equal(1000.000m, userProj.ProfitTarget);

        // 2. Head
        var headProj = await _dbContext.HeadsDbSet.AsNoTracking()
            .Select(h => new { h.Id, h.Email, h.Wallet })
            .Take(1)
            .ToListAsync(ct);
        Assert.Single(headProj);
        Assert.Equal("john.doe@gmail.com", headProj[0].Email);
        Assert.Equal(100.000m, headProj[0].Wallet);

        // 3. FlightBooking
        var bookingProj = await _dbContext.FlightBookingsDbSet.AsNoTracking()
            .Select(b => new { b.Id, b.FromLocation, b.ToLocation, b.PayableAmount })
            .FirstOrDefaultAsync(ct);
        Assert.NotNull(bookingProj);
        Assert.Equal("BAH", bookingProj.FromLocation);

        // 4. HeadLedger
        var ledgerProj = await _dbContext.HeadLedgersDbSet.AsNoTracking()
            .Select(l => new { l.Id, l.Debit, l.Credit })
            .ToListAsync(ct);
        Assert.Single(ledgerProj);
        Assert.Equal(75.000m, ledgerProj[0].Debit);

        // 5. CashInHand
        var cashProj = await _dbContext.CashInHandsDbSet.AsNoTracking()
            .Select(c => new { c.Id, c.Amount })
            .FirstOrDefaultAsync(ct);
        Assert.NotNull(cashProj);
        Assert.Equal(25.000m, cashProj.Amount);

        // 6. Lead
        var leadProj = await _dbContext.LeadsDbSet.AsNoTracking()
            .Select(l => new { l.Id, l.Name, l.Status })
            .Take(1)
            .ToListAsync(ct);
        Assert.Single(leadProj);

        // Output queries to scratch for SQL audit review
        var sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "scratch_target_queries.txt");
        using var writer = new StreamWriter(sqlPath, false);
        writer.WriteLine("=== User Projection ===");
        writer.WriteLine(_dbContext.UsersDbSet.AsNoTracking().Select(u => new { u.Id, u.Name, u.Email, u.ProfitTarget }).ToQueryString());
        writer.WriteLine();
        writer.WriteLine("=== Head Projection ===");
        writer.WriteLine(_dbContext.HeadsDbSet.AsNoTracking().Select(h => new { h.Id, h.Email, h.Wallet }).ToQueryString());
    }

    [Fact]
    public async Task DumpHeads()
    {
        var heads = await _dbContext.HeadsDbSet.AsNoTracking().ToListAsync();
        foreach (var h in heads)
        {
            _output.WriteLine($"DB_HEAD_DUMP: Id={h.Id}, Name={h.FirstName} {h.LastName}, UserId={h.UserId}, Type={h.Type}, CreatedBy={h.CreatedBy}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}
