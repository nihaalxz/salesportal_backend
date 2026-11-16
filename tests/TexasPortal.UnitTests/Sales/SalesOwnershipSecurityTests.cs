using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.UnitTests.Sales
{
    public class SalesOwnershipSecurityTests
    {
        private ISalesPortalDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new SalesPortalDbContext(options);
        }

        private class FakeAgentsApiClient : IAgentsApiClient
        {
            public Task<List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>> GetAgentsAsync(long salesmanId, string? search, int limit, CancellationToken ct) => Task.FromResult(new List<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse>());
            public Task<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse?> GetAgentByIdAsync(long agentId, CancellationToken ct) => Task.FromResult<TexasPortal.Contracts.Sales.Heads.HeadLookupResponse?>(null);
            public Task<List<TexasPortal.Contracts.Sales.Receipts.BankResponse>> GetBanksAsync(CancellationToken ct) => Task.FromResult(new List<TexasPortal.Contracts.Sales.Receipts.BankResponse>());
            public Task<List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>> GetSuppliersAsync(CancellationToken ct) => Task.FromResult(new List<TexasPortal.Contracts.Sales.Receipts.SupplierResponse>());
            public Task<List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>> GetVisaCountriesAsync(CancellationToken ct = default) => Task.FromResult(new List<TexasPortal.Contracts.Sales.Bookings.VisaCountryMasterResponse>());
            public Task<List<TexasPortal.Contracts.Sales.Bookings.VisaPackageMasterResponse>> GetVisaPackagesAsync(long? countryId = null, CancellationToken ct = default) => Task.FromResult(new List<TexasPortal.Contracts.Sales.Bookings.VisaPackageMasterResponse>());
            public Task CreditAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct) => Task.CompletedTask;
            public Task DebitAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct) => Task.CompletedTask;
            public Task<TexasPortal.Contracts.Common.PagedResponse<TexasPortal.Contracts.Sales.Heads.HeadLedgerEntryResponse>> GetAgentLedgerAsync(long agentId, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
            public Task CreateGovtSupplierPaymentAsync(long supplierId, decimal amount, string? referenceNumber, string? externalReference, string? serviceType, string? description, long? createdByUserId = null, CancellationToken ct = default) => Task.CompletedTask;
            public Task SyncGovtSupplierPaymentsAsync(string externalReference, string serviceType, long? createdByUserId, List<GovtFeeSyncItem> fees, CancellationToken ct = default) => Task.CompletedTask;
            public Task<string?> GetBookingApproverAsync(string serviceType, string bookingId, string? docNumber = null, CancellationToken ct = default) => Task.FromResult<string?>(null);
            public Task<BookingSupplierPaymentSummary?> GetBookingSupplierPaymentsAsync(string serviceType, string externalReference, CancellationToken ct = default) => Task.FromResult<BookingSupplierPaymentSummary?>(null);
            public Task<(bool HasReturn, decimal? ReturnAmount, bool HasPurchaseReturn, decimal? PurchaseReturnAmount)> CheckSalesReturnAsync(string serviceType, string searchRef, CancellationToken ct = default) => Task.FromResult((false, (decimal?)null, false, (decimal?)null));
        }

        private class FakeOperationalAssignmentClient : IOperationalAssignmentClient
        {
            public Task<IReadOnlyDictionary<long, OperationalAssignmentSummary>> GetActiveAssignmentsAsync(
                string entityType, IEnumerable<long> entityIds, CancellationToken ct = default)
            {
                return Task.FromResult<IReadOnlyDictionary<long, OperationalAssignmentSummary>>(new Dictionary<long, OperationalAssignmentSummary>());
            }
        }

        private class FakeSupplierCostReconciliationService : TexasPortal.Application.Sales.Services.ISupplierCostReconciliationService
        {
            public Task<TexasPortal.Application.Sales.Services.SupplierCostReconciliationResult> ReconcileVisaFeesAsync(
                long visaApplicationId, List<TexasPortal.Application.Sales.Services.ReconciledFeeItem> localFees, CancellationToken ct = default)
            {
                return Task.FromResult(new TexasPortal.Application.Sales.Services.SupplierCostReconciliationResult("Visa", "REF", 0m, 0m, new List<TexasPortal.Application.Sales.Services.ReconciledFeeItem>()));
            }

            public Task<TexasPortal.Application.Sales.Services.SupplierCostReconciliationResult> ReconcileVfsFeesAsync(
                long vfsAppointmentId, List<TexasPortal.Application.Sales.Services.ReconciledFeeItem> localFees, CancellationToken ct = default)
            {
                return Task.FromResult(new TexasPortal.Application.Sales.Services.SupplierCostReconciliationResult("Vfs", "REF", 0m, 0m, new List<TexasPortal.Application.Sales.Services.ReconciledFeeItem>()));
            }

            public Task<TexasPortal.Application.Sales.Services.SupplierCostReconciliationResult> ReconcileHolidayFeesAsync(
                long holidayPackageId, List<TexasPortal.Application.Sales.Services.ReconciledFeeItem> localFees, CancellationToken ct = default)
            {
                return Task.FromResult(new TexasPortal.Application.Sales.Services.SupplierCostReconciliationResult("Holiday", "REF", 0m, 0m, new List<TexasPortal.Application.Sales.Services.ReconciledFeeItem>()));
            }
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanFlightBooking()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.FlightBookings.Add(new FlightBooking
            {
                Id = 101,
                DocumentNumber = "FLI-00101",
                UserId = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                FromLocation = "BAH",
                ToLocation = "DXB",
                PaymentMethod = "cash"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var resultById = await service.GetFlightBookingByIdAsync(userId: 100, idOrDoc: "101");
            var resultByDoc = await service.GetFlightBookingByIdAsync(userId: 100, idOrDoc: "FLI-00101");

            Assert.Null(resultById);
            Assert.Null(resultByDoc);

            var ownerResult = await service.GetFlightBookingByIdAsync(userId: 200, idOrDoc: "101");
            Assert.NotNull(ownerResult);
            Assert.Equal("FLI-00101", ownerResult.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanVisaBooking()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.SalesVisaApplications.Add(new SalesVisaApplication
            {
                Id = 202,
                DocumentNumber = "VISA-00202",
                UserId = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                CountryId = 1,
                VisaPackId = 1,
                VisaPackName = "Tourist Visa"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetVisaApplicationByIdAsync(userId: 100, idOrDoc: "202");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetVisaApplicationByIdAsync(userId: 200, idOrDoc: "202");
            Assert.NotNull(ownerAccess);
            Assert.Equal("VISA-00202", ownerAccess.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanHotelBooking()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.HotelBookings.Add(new HotelBooking
            {
                Id = 303,
                DocumentNumber = "HOT-00303",
                UserId = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                Country = "Bahrain",
                GuestName = "John Doe",
                PaymentMethod = "cash"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetHotelBookingByIdAsync(userId: 100, idOrDoc: "303");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetHotelBookingByIdAsync(userId: 200, idOrDoc: "303");
            Assert.NotNull(ownerAccess);
            Assert.Equal("HOT-00303", ownerAccess.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanVfsAppointment()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.VfsAppointments.Add(new VfsAppointment
            {
                Id = 404,
                DocumentNumber = "VFS-00404",
                CreatedBy = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                Country = "United Kingdom",
                Name = "Jane Doe",
                PaymentMode = "cash",
                Prefix = "Ms.",
                Supplier = "VFS Global",
                VisaType = "Standard Tourist"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetVfsAppointmentByIdAsync(userId: 100, idOrDoc: "404");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetVfsAppointmentByIdAsync(userId: 200, idOrDoc: "404");
            Assert.NotNull(ownerAccess);
            Assert.Equal("VFS-00404", ownerAccess.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanHolidayPackage()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.HolidayPackages.Add(new HolidayPackage
            {
                Id = 505,
                DocumentNumber = "PKG-00505",
                CreatedBy = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                Country = "Georgia",
                PassengerName = "Alice Doe",
                PaymentMode = "cash",
                Prefix = "Mrs.",
                Supplier = "Texas Travel"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetHolidayPackageByIdAsync(userId: 100, idOrDoc: "505");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetHolidayPackageByIdAsync(userId: 200, idOrDoc: "505");
            Assert.NotNull(ownerAccess);
            Assert.Equal("PKG-00505", ownerAccess.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanTransportation()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.Transportations.Add(new Transportation
            {
                Id = 606,
                DocumentNumber = "TRN-00606",
                CreatedBy = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                Name = "Bob Doe",
                PaymentMethod = "cash",
                Prefix = "Mr.",
                Supplier = "Texas Fleet",
                Pickup = "Airport",
                Drop = "Hotel"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetTransportationByIdAsync(userId: 100, idOrDoc: "606");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetTransportationByIdAsync(userId: 200, idOrDoc: "606");
            Assert.NotNull(ownerAccess);
            Assert.Equal("TRN-00606", ownerAccess.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanOtherService()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.OtherServices.Add(new OtherService
            {
                Id = 707,
                DocumentNumber = "OTH-00707",
                CreatedBy = 200,
                ClientType = "customer",
                PaymentStatus = "paid",
                ProcessingStatus = "pending",
                PaymentMethod = "cash",
                ServiceName = "Document Translation",
                Prefix = "Mr.",
                Name = "David Doe"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetOtherServiceByIdAsync(userId: 100, idOrDoc: "707");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetOtherServiceByIdAsync(userId: 200, idOrDoc: "707");
            Assert.NotNull(ownerAccess);
            Assert.Equal("OTH-00707", ownerAccess.DocumentNumber);
        }

        [Fact]
        public async Task SalesmanCannotAccessOtherSalesmanReceipt()
        {
            var db = CreateInMemoryDbContext();
            var service = new SalesReadService(db, new FakeAgentsApiClient(), new FakeOperationalAssignmentClient(), null!, new FakeSupplierCostReconciliationService());

            db.Receipts.Add(new Receipt
            {
                Id = 808,
                ReceiptId = "RCP-00808",
                CreatedBy = 200,
                Status = "approved",
                PaymentMode = "cash",
                Service = "Flight Booking",
                Narration = "Settlement"
            });
            await db.SaveChangesAsync(CancellationToken.None);

            var accessDenied = await service.GetReceiptByIdAsync(userId: 100, idOrDoc: "808");
            Assert.Null(accessDenied);

            var ownerAccess = await service.GetReceiptByIdAsync(userId: 200, idOrDoc: "808");
            Assert.NotNull(ownerAccess);
            Assert.Equal("RCP-00808", ownerAccess.ReceiptId);
        }
    }
}
