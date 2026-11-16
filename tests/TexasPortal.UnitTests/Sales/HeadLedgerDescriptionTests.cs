using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.UnitTests.Sales
{
    public class HeadLedgerDescriptionTests
    {
        private class FakeAgentsApiClient : IAgentsApiClient
        {
            public Task<List<VisaCountryMasterResponse>> GetVisaCountriesAsync(CancellationToken ct = default)
            {
                return Task.FromResult(new List<VisaCountryMasterResponse>
                {
                    new VisaCountryMasterResponse(1, "Bahrain", "BH", true, 5),
                    new VisaCountryMasterResponse(2, "Saudi Arabia", "SA", true, 5)
                });
            }

            public Task<BookingSupplierPaymentSummary?> GetBookingSupplierPaymentsAsync(string serviceType, string externalReference, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<(bool HasReturn, decimal? ReturnAmount, bool HasPurchaseReturn, decimal? PurchaseReturnAmount)> CheckSalesReturnAsync(string serviceType, string searchRef, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>> GetAgentsAsync(long salesmanId, string? search, int limit, CancellationToken ct) => throw new NotImplementedException();
            public Task<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse?> GetAgentByIdAsync(long agentId, CancellationToken ct) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Receipts.BankResponse>> GetBanksAsync(CancellationToken ct) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>> GetSuppliersAsync(CancellationToken ct) => throw new NotImplementedException();
            public Task<List<VisaPackageMasterResponse>> GetVisaPackagesAsync(long? countryId = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task CreditAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct) => throw new NotImplementedException();
            public Task DebitAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct) => throw new NotImplementedException();
            public Task<TexasPortal.Contracts.Common.PagedResponse<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>> GetAgentLedgerAsync(long agentId, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
            public Task CreateGovtSupplierPaymentAsync(long supplierId, decimal amount, string? referenceNumber, string? externalReference, string? serviceType, string? description, long? createdByUserId = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task SyncGovtSupplierPaymentsAsync(string externalReference, string serviceType, long? createdByUserId, List<GovtFeeSyncItem> fees, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<string?> GetBookingApproverAsync(string serviceType, string bookingId, string? docNumber = null, CancellationToken ct = default) => throw new NotImplementedException();
        }

        private class FakeDbContextFactory : ISalesPortalDbContextFactory
        {
            private readonly DbContextOptions<SalesPortalDbContext> _options;
            public FakeDbContextFactory(DbContextOptions<SalesPortalDbContext> options) => _options = options;
            public ISalesPortalDbContext CreateDbContext() => new SalesPortalDbContext(_options);
        }

        private class FakeAssignmentClient : IOperationalAssignmentClient
        {
            public Task<IReadOnlyDictionary<long, OperationalAssignmentSummary>> GetActiveAssignmentsAsync(
                string entityType, IEnumerable<long> entityIds, CancellationToken ct = default)
            {
                return Task.FromResult<IReadOnlyDictionary<long, OperationalAssignmentSummary>>(
                    new Dictionary<long, OperationalAssignmentSummary>());
            }
        }

        private (ISalesPortalDbContext, OperationsReadService) CreateTestHarness()
        {
            var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var db = new SalesPortalDbContext(options);
            var factory = new FakeDbContextFactory(options);
            var client = new FakeAgentsApiClient();
            var assignment = new FakeAssignmentClient();
            var service = new OperationsReadService(db, factory, client, assignment, null!);
            return (db, service);
        }

        [Fact]
        public async Task GetHeadLedgerAsync_FormatsVisaApplicationWithApplicantCountryAndPack()
        {
            var (db, service) = CreateTestHarness();

            var head = new Head
            {
                Id = 10,
                FirstName = "Ali",
                LastName = "Khan",
                Email = "ali@example.com",
                MobileNo = "12345678",
                Address = "Manama",
                CreatedBy = 1
            };
            db.Heads.Add(head);

            var visa = new SalesVisaApplication
            {
                Id = 101,
                HeadId = 10,
                UserId = 1,
                ClientType = "customer",
                CountryId = 1, // Bahrain
                VisaPackName = "Tourist 30 Days",
                PayableAmount = 55.000m,
                CreatedAt = DateTime.UtcNow,
                Applicants = new List<SalesVisaApplicant>
                {
                    new SalesVisaApplicant
                    {
                        Id = 1,
                        FName = "Abdul",
                        LName = "Kader"
                    }
                }
            };
            db.SalesVisaApplications.Add(visa);
            await db.SaveChangesAsync(CancellationToken.None);

            var ledger = await service.GetHeadLedgerAsync(10, 1, 20);

            Assert.NotNull(ledger);
            Assert.Single(ledger.Items);
            var entry = ledger.Items[0];
            Assert.Equal("visa_application", entry.Type);
            Assert.Contains("Abdul Kader", entry.Description);
            Assert.Contains("Bahrain", entry.Description);
            Assert.Contains("Tourist 30 Days", entry.Description);
        }

        [Fact]
        public async Task GetHeadLedgerAsync_FormatsReceiptsWithBankAndCashAccounts()
        {
            var (db, service) = CreateTestHarness();

            var head = new Head
            {
                Id = 20,
                FirstName = "Sara",
                LastName = "Ahmed",
                Email = "sara@example.com",
                MobileNo = "87654321",
                Address = "Manama",
                CreatedBy = 1
            };
            db.Heads.Add(head);

            var onlineReceipt = new Receipt
            {
                Id = 201,
                ReceiptId = "TX1001",
                DocumentNumber = "TXSRCPT-001",
                HeadId = 20,
                Service = "deposit",
                Amount = 150m,
                PaymentMode = "online",
                BankName = "Ahli United Bank",
                Status = "success",
                Narration = "Customer deposit",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var cashReceipt = new Receipt
            {
                Id = 202,
                ReceiptId = "TX1002",
                DocumentNumber = "TXSRCPT-002",
                HeadId = 20,
                Service = "deposit",
                Amount = 50m,
                PaymentMode = "cash",
                BankName = null,
                Status = "success",
                Narration = "Cash deposit",
                CreatedAt = DateTime.UtcNow
            };

            db.Receipts.AddRange(onlineReceipt, cashReceipt);
            await db.SaveChangesAsync(CancellationToken.None);

            var ledger = await service.GetHeadLedgerAsync(20, 1, 20);

            Assert.NotNull(ledger);
            Assert.Equal(2, ledger.Items.Count);

            var onlineEntry = ledger.Items.FirstOrDefault(e => e.Id == 201);
            Assert.NotNull(onlineEntry);
            Assert.Contains("Ahli United Bank", onlineEntry.Description);

            var cashEntry = ledger.Items.FirstOrDefault(e => e.Id == 202);
            Assert.NotNull(cashEntry);
            Assert.Contains("Cash Account", cashEntry.Description);
        }
    }
}
