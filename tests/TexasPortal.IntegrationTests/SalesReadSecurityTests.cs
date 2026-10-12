using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using TexasPortal.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace TexasPortal.IntegrationTests;

public class SalesReadSecurityTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SalesPortalDbContext _dbContext;
    private readonly SalesReadService _readService;
    private long _salesmanAId;
    private long _salesmanBId;

    public SalesReadSecurityTests(ITestOutputHelper output)
    {
        _output = output;

        var connString = Environment.GetEnvironmentVariable("TEXAS_PORTAL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connString))
        {
            connString = "Server=localhost\\SQLEXPRESS;Database=TexasPortal_Validation_Security;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
        else
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "TexasPortal_Validation_Security";
            connString = builder.ConnectionString;
        }

        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseSqlServer(connString)
            .Options;

        _dbContext = new SalesPortalDbContext(options);
        _readService = new SalesReadService(_dbContext, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new TexasPortal.Application.Sales.Services.SupplierCostReconciliationService(_dbContext, new FakeAgentsApiClient(), Microsoft.Extensions.Logging.Abstractions.NullLogger<TexasPortal.Application.Sales.Services.SupplierCostReconciliationService>.Instance));
    }

    private class FakeOperationalAssignmentClient : TexasPortal.Application.Common.Interfaces.IOperationalAssignmentClient
    {
        public Task<IReadOnlyDictionary<long, TexasPortal.Application.Common.Interfaces.OperationalAssignmentSummary>> GetActiveAssignmentsAsync(string entityType, System.Collections.Generic.IEnumerable<long> entityIds, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyDictionary<long, TexasPortal.Application.Common.Interfaces.OperationalAssignmentSummary>>(new System.Collections.Generic.Dictionary<long, TexasPortal.Application.Common.Interfaces.OperationalAssignmentSummary>());
        }
    }

    private class FakeAgentsApiClient : IAgentsApiClient
    {
        public Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>> GetAgentsAsync(long salesmanId, string? search, int limit, CancellationToken ct)
        {
            return Task.FromResult(new System.Collections.Generic.List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>());
        }

        public Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.BankResponse>> GetBanksAsync(CancellationToken ct)
        {
            return Task.FromResult(new System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.BankResponse>());
        }

        public Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>> GetSuppliersAsync(CancellationToken ct)
        {
            return Task.FromResult(new System.Collections.Generic.List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>());
        }

        public Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>> GetVisaCountriesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new System.Collections.Generic.List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>());
        }

        public Task<System.Collections.Generic.List<TexasPortal.Contracts.Sales.Bookings.VisaPackageMasterResponse>> GetVisaPackagesAsync(long? countryId = null, CancellationToken ct = default)
        {
            return Task.FromResult(new System.Collections.Generic.List<TexasPortal.Contracts.Sales.Bookings.VisaPackageMasterResponse>());
        }

        public Task CreditAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task DebitAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<TexasPortal.Contracts.Common.PagedResponse<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>> GetAgentLedgerAsync(long agentId, int page, int pageSize, CancellationToken ct)
        {
            var empty = TexasPortal.Contracts.Common.PagedResponse<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>.Create(
                new System.Collections.Generic.List<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>(), 0, page, pageSize);
            return Task.FromResult(empty);
        }

        public Task<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse?> GetAgentByIdAsync(long agentId, CancellationToken ct)
        {
            return Task.FromResult<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse?>(null);
        }

        public Task CreateGovtSupplierPaymentAsync(long supplierId, decimal amount, string? referenceNumber, string? externalReference, string? serviceType, string? description, long? createdByUserId = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task SyncGovtSupplierPaymentsAsync(string externalReference, string serviceType, long? createdByUserId, System.Collections.Generic.List<TexasPortal.Application.Sales.Queries.GovtFeeSyncItem> items, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<(bool HasReturn, decimal? ReturnAmount, bool HasPurchaseReturn, decimal? PurchaseReturnAmount)> CheckSalesReturnAsync(string bookingId, string bookingModule, CancellationToken ct = default)
        {
            return Task.FromResult((false, (decimal?)null, false, (decimal?)null));
        }

        public Task<TexasPortal.Application.Sales.Queries.BookingSupplierPaymentSummary?> GetBookingSupplierPaymentsAsync(string externalReference, string serviceType, CancellationToken ct = default)
        {
            return Task.FromResult<TexasPortal.Application.Sales.Queries.BookingSupplierPaymentSummary?>(null);
        }
    }

    private async Task SeedDataAsync(CancellationToken ct)
    {
        var connStr = _dbContext.Database.GetDbConnection().ConnectionString;
        var isLocal = connStr.Contains("localhost") || connStr.Contains("(localdb)") || connStr.Contains("127.0.0.1") || connStr.Contains("SQLEXPRESS");
        if (isLocal)
        {
            await _dbContext.Database.EnsureDeletedAsync(ct);
        }
        await _dbContext.Database.MigrateAsync(ct);

        // Salesman A
        var salesmanA = new User
        {
            Name = "Salesman A",
            Email = "salesman.a@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword",
            ProfitTarget = 1000m,
            MonthlyTarget = 2000m,
            ProfitGenerated = 150m
        };
        _dbContext.UsersDbSet.Add(salesmanA);

        // Salesman B
        var salesmanB = new User
        {
            Name = "Salesman B",
            Email = "salesman.b@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword",
            ProfitTarget = 800m,
            MonthlyTarget = 1600m,
            ProfitGenerated = 50m
        };
        _dbContext.UsersDbSet.Add(salesmanB);
        await _dbContext.SaveChangesAsync(ct);

        _salesmanAId = salesmanA.Id;
        _salesmanBId = salesmanB.Id;

        // Seed data for Salesman A
        var headA = new Head
        {
            CreatedBy = _salesmanAId,
            FirstName = "A_First",
            LastName = "A_Last",
            Email = "head.a@gmail.com",
            MobileNo = "97311111111",
            Nationality = "Bahraini",
            Address = "Manama",
            Status = "active"
        };
        _dbContext.HeadsDbSet.Add(headA);

        var leadA = new Lead
        {
            CreatedBy = _salesmanAId,
            Name = "Lead A",
            CountryCode = "+973",
            MobileNo = "97322222222",
            Status = "new"
        };
        _dbContext.LeadsDbSet.Add(leadA);

        var flightA = new FlightBooking
        {
            UserId = _salesmanAId,
            FromLocation = "BAH",
            ToLocation = "DXB",
            PaymentMethod = "cash",
            PaymentStatus = "pending",
            ProcessingStatus = "pending",
            PayableAmount = 100m
        };
        _dbContext.FlightBookingsDbSet.Add(flightA);

        var cashA = new CashInHand
        {
            UserId = _salesmanAId,
            Amount = 120m,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.CashInHandsDbSet.Add(cashA);

        var depositA = new Deposit
        {
            UserId = _salesmanAId,
            Amount = 50m,
            Status = "pending",
            Type = "bank",
            DestinationId = 1,
            Date = DateTime.UtcNow
        };
        _dbContext.DepositsDbSet.Add(depositA);

        var perfA = new StaffMonthlyRecord
        {
            UserId = _salesmanAId,
            Month = "July",
            Year = 2026,
            ProfitGenerated = 150m,
            ProfitTarget = 1000m,
            MonthlyTarget = 2000m,
            Incentive = 50m,
            BasicSalary = 500m,
            HousingAllowance = 100m,
            TransportAllowance = 50m,
            TargetAchieved = false
        };
        _dbContext.StaffMonthlyRecordsDbSet.Add(perfA);

        // Seed data for Salesman B
        var headB = new Head
        {
            CreatedBy = _salesmanBId,
            FirstName = "B_First",
            LastName = "B_Last",
            Email = "head.b@gmail.com",
            MobileNo = "97333333333",
            Nationality = "Saudi",
            Address = "Riyadh",
            Status = "active"
        };
        _dbContext.HeadsDbSet.Add(headB);

        await _dbContext.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task Verify_SalesmanOwnershipIsolation_ForEveryResource()
    {
        var ct = CancellationToken.None;
        await SeedDataAsync(ct);

        // 1. Heads Isolation
        var headsForA = await _readService.GetHeadsAsync(_salesmanAId, new HeadsListQuery(), ct);
        Assert.Single(headsForA.Items);
        Assert.Equal("A_First", headsForA.Items[0].FirstName);

        var headsForB = await _readService.GetHeadsAsync(_salesmanBId, new HeadsListQuery(), ct);
        Assert.Single(headsForB.Items);
        Assert.Equal("B_First", headsForB.Items[0].FirstName);

        // Try querying Head B by Head ID using Salesman A credentials (should return null/404)
        var headIdB = headsForB.Items[0].Id;
        var directHeadQueryByA = await _readService.GetHeadByIdAsync(_salesmanAId, headIdB, ct: ct);
        Assert.Null(directHeadQueryByA);

        // 2. Leads Isolation
        var leadsForA = await _readService.GetLeadsAsync(_salesmanAId, new LeadsListQuery(), ct);
        Assert.Single(leadsForA.Items);
        Assert.Equal("Lead A", leadsForA.Items[0].Name);

        var leadsForB = await _readService.GetLeadsAsync(_salesmanBId, new LeadsListQuery(), ct);
        Assert.Empty(leadsForB.Items);

        // 3. Bookings Isolation
        var flightBookingsForA = await _readService.GetFlightBookingsAsync(_salesmanAId, new FlightBookingsListQuery(), ct);
        Assert.Single(flightBookingsForA.Items);

        var flightBookingsForB = await _readService.GetFlightBookingsAsync(_salesmanBId, new FlightBookingsListQuery(), ct);
        Assert.Empty(flightBookingsForB.Items);

        // 4. Cash Isolation
        var cashForA = await _readService.GetCashInHandAsync(_salesmanAId, ct);
        Assert.NotNull(cashForA);
        Assert.Equal(120m, cashForA.Amount);

        var cashForB = await _readService.GetCashInHandAsync(_salesmanBId, ct);
        Assert.Null(cashForB);

        // 5. Deposits Isolation
        var depositsForA = await _readService.GetDepositsAsync(_salesmanAId, new DepositsListQuery(), ct);
        Assert.Single(depositsForA.Items);

        var depositsForB = await _readService.GetDepositsAsync(_salesmanBId, new DepositsListQuery(), ct);
        Assert.Empty(depositsForB.Items);

        // 6. Performance Isolation
        var perfForA = await _readService.GetPerformanceRecordsAsync(_salesmanAId, new PerformanceListQuery(), ct);
        Assert.Single(perfForA.Items);

        var perfForB = await _readService.GetPerformanceRecordsAsync(_salesmanBId, new PerformanceListQuery(), ct);
        Assert.Empty(perfForB.Items);
    }

    [Fact]
    public async Task Verify_HeadsListSortingAndSearching()
    {
        var ct = CancellationToken.None;
        await SeedDataAsync(ct);

        // Seed another head for Salesman A
        var headA2 = new Head
        {
            CreatedBy = _salesmanAId,
            FirstName = "Z_First",
            LastName = "Z_Last",
            Email = "head.z@gmail.com",
            MobileNo = "97399999999",
            Nationality = "Bahraini",
            Address = "Riffa",
            Status = "active"
        };
        _dbContext.HeadsDbSet.Add(headA2);
        await _dbContext.SaveChangesAsync(ct);

        // Search test
        var searchResult = await _readService.GetHeadsAsync(_salesmanAId, new HeadsListQuery(SearchTerm: "Z_First"), ct);
        Assert.Single(searchResult.Items);
        Assert.Equal("Z_First", searchResult.Items[0].FirstName);

        // Sort lastName asc
        var sortAsc = await _readService.GetHeadsAsync(_salesmanAId, new HeadsListQuery(SortBy: "lastName", SortOrder: "asc"), ct);
        Assert.Equal(2, sortAsc.Items.Count);
        Assert.Equal("A_Last", sortAsc.Items[0].LastName);

        // Sort lastName desc
        var sortDesc = await _readService.GetHeadsAsync(_salesmanAId, new HeadsListQuery(SortBy: "lastName", SortOrder: "desc"), ct);
        Assert.Equal(2, sortDesc.Items.Count);
        Assert.Equal("Z_Last", sortDesc.Items[0].LastName);
    }

    public async ValueTask DisposeAsync()
    {
        var connStr = _dbContext.Database.GetDbConnection().ConnectionString;
        var isLocal = connStr.Contains("localhost") || connStr.Contains("(localdb)") || connStr.Contains("127.0.0.1") || connStr.Contains("SQLEXPRESS");
        if (isLocal)
        {
            await _dbContext.Database.EnsureDeletedAsync();
        }
        await _dbContext.DisposeAsync();
    }
}
