using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Profit;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.UnitTests.Sales
{
    public class SalesmanProfitServiceTests
    {
        private class FakeAgentsApiClient : IAgentsApiClient
        {
            public BookingSupplierPaymentSummary? SupplierPaymentSummary { get; set; }

            public Task<BookingSupplierPaymentSummary?> GetBookingSupplierPaymentsAsync(string serviceType, string externalReference, CancellationToken ct = default)
            {
                return Task.FromResult(SupplierPaymentSummary);
            }

            public Task<(bool HasReturn, decimal? ReturnAmount, bool HasPurchaseReturn, decimal? PurchaseReturnAmount)> CheckSalesReturnAsync(string serviceType, string searchRef, CancellationToken ct = default)
            {
                return Task.FromResult((false, (decimal?)null, false, (decimal?)null));
            }

            public Task<List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>> GetAgentsAsync(long salesmanId, string? search, int limit, CancellationToken ct) => throw new NotImplementedException();
            public Task<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse?> GetAgentByIdAsync(long agentId, CancellationToken ct) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Receipts.BankResponse>> GetBanksAsync(CancellationToken ct) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>> GetSuppliersAsync(CancellationToken ct) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>> GetVisaCountriesAsync(CancellationToken ct = default) => throw new NotImplementedException();
            public Task<List<TexasPortal.Contracts.Sales.Bookings.VisaPackageMasterResponse>> GetVisaPackagesAsync(long? countryId = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task CreditAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct) => throw new NotImplementedException();
            public Task DebitAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct) => throw new NotImplementedException();
            public Task<TexasPortal.Contracts.Common.PagedResponse<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>> GetAgentLedgerAsync(long agentId, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
            public Task CreateGovtSupplierPaymentAsync(long supplierId, decimal amount, string? referenceNumber, string? externalReference, string? serviceType, string? description, long? createdByUserId = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task SyncGovtSupplierPaymentsAsync(string externalReference, string serviceType, long? createdByUserId, List<GovtFeeSyncItem> fees, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<string?> GetBookingApproverAsync(string serviceType, string bookingId, string? docNumber = null, CancellationToken ct = default) => throw new NotImplementedException();
        }

        private ISalesPortalDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new SalesPortalDbContext(options);
        }

        [Fact]
        public void CalculateInhouseProfit_WithPayableAmount_ReturnsPayableMinusCostPlusAdjustments()
        {
            // Business Rule: When approved / supplier cost is 0, profit should be the sale amount + adjustments
            var profit = SalesmanProfitService.CalculateInhouseProfit(8.000m, 0.000m, 0m);
            Assert.Equal(8.000m, profit);

            var profitWithAdjustments = SalesmanProfitService.CalculateInhouseProfit(8.000m, 0.000m, 2.500m);
            Assert.Equal(10.500m, profitWithAdjustments);
        }

        [Fact]
        public void CalculateNormalProfit_WithZeroSupplierCost_ReturnsFullPayableAmount()
        {
            var profit = SalesmanProfitService.CalculateNormalProfit(8.000m, 0.000m);
            Assert.Equal(8.000m, profit);
        }

        [Fact]
        public async Task FinalizeOrUpdateProfitAsync_WhenApprovedCostIsZero_RecognizesFullSaleAmountAsProfit()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var fakeClient = new FakeAgentsApiClient
            {
                SupplierPaymentSummary = new BookingSupplierPaymentSummary(
                    ServiceType: "Visa",
                    ExternalReference: "368",
                    ApprovedTotal: 0.000m,
                    Payments: new List<ApprovedSupplierPaymentItem>
                    {
                        new(
                            Id: 592,
                            Amount: 0.000m,
                            Reference: "TXSPAY124861",
                            Description: "Supplier Payment for Visa Application TXSVISA416750",
                            Status: "approved",
                            ApprovedAt: DateTime.UtcNow
                        )
                    }
                )
            };

            var user = new User
            {
                Id = 5,
                Name = "GEETHESH",
                Email = "geethesh@texas.com",
                Password = "hashed_secret_password",
                UserType = "Salesman",
                ProfitGenerated = 0m
            };
            db.Users.Add(user);

            var visa = new SalesVisaApplication
            {
                Id = 368,
                UserId = 5,
                ClientType = "Walk-in",
                DocumentNumber = "TXSVISA416750",
                PayableAmount = 8.000m,
                ExpectedSupplierCost = 0.000m,
                Profit = 0.000m,
                ProcessingStatus = "approved",
                IsDealClosed = true,
                IsInhouse = true
            };
            db.SalesVisaApplications.Add(visa);
            await db.SaveChangesAsync(default);

            var service = new SalesmanProfitService(db, fakeClient, NullLogger<SalesmanProfitService>.Instance);

            // Act
            var record = await service.FinalizeOrUpdateProfitAsync("Visa", "368");

            // Assert
            Assert.NotNull(record);
            Assert.Equal(8.000m, record.PayableAmount);
            Assert.Equal(0.000m, record.FinalSupplierCost);
            Assert.Equal(8.000m, record.ProfitAmount); // Full sale recognized
            Assert.Equal(ProfitType.Normal, record.ProfitType);
            Assert.Equal(SupplierCostSource.Approved, record.SupplierCostSource);
            Assert.Equal(0.000m, record.AccountsApprovedTotal);

            // Verify synced to booking entity
            var updatedVisa = await db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == 368L);
            Assert.NotNull(updatedVisa);
            Assert.Equal(8.000m, updatedVisa.Profit);

            // Verify user total profit updated
            var updatedUser = await db.Users.FirstOrDefaultAsync(x => x.Id == 5L);
            Assert.NotNull(updatedUser);
            Assert.Equal(8.000m, updatedUser.ProfitGenerated);
        }

        [Fact]
        public async Task FinalizeOrUpdateProfitAsync_InhouseWithoutApprovedPayment_RecognizesFullSaleAmountAsProfit()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var fakeClient = new FakeAgentsApiClient
            {
                SupplierPaymentSummary = null // No approved supplier payment
            };

            var user = new User
            {
                Id = 10,
                Name = "Salesman A",
                Email = "salesmanA@texas.com",
                Password = "hashed_secret_password",
                UserType = "Salesman",
                ProfitGenerated = 0m
            };
            db.Users.Add(user);

            var visa = new SalesVisaApplication
            {
                Id = 100,
                UserId = 10,
                ClientType = "Walk-in",
                DocumentNumber = "TXSVISA999999",
                PayableAmount = 15.000m,
                ExpectedSupplierCost = 0.000m,
                Profit = 0.000m,
                ProcessingStatus = "completed",
                IsDealClosed = true,
                IsInhouse = true
            };
            db.SalesVisaApplications.Add(visa);
            await db.SaveChangesAsync(default);

            var service = new SalesmanProfitService(db, fakeClient, NullLogger<SalesmanProfitService>.Instance);

            // Act
            var record = await service.FinalizeOrUpdateProfitAsync("Visa", "100");

            // Assert
            Assert.NotNull(record);
            Assert.Equal(15.000m, record.PayableAmount);
            Assert.Equal(0.000m, record.FinalSupplierCost);
            Assert.Equal(15.000m, record.ProfitAmount); // Full sale recognized
            Assert.Equal(ProfitType.Inhouse, record.ProfitType);
            Assert.Equal(SupplierCostSource.Estimated, record.SupplierCostSource);

            // Synced to booking
            var updatedVisa = await db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == 100L);
            Assert.NotNull(updatedVisa);
            Assert.Equal(15.000m, updatedVisa.Profit);
        }
    }
}
