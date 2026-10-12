using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Persistence;

public interface ISalesPortalDbContext : IDisposable
{
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    DbSet<User> Users { get; }
    DbSet<Head> Heads { get; }
    DbSet<HeadLedger> HeadLedgers { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<FlightBooking> FlightBookings { get; }
    DbSet<FlightPassenger> FlightPassengers { get; }
    DbSet<HotelBooking> HotelBookings { get; }
    DbSet<HotelGuest> HotelGuests { get; }
    DbSet<TransportPassenger> TransportPassengers { get; }
    DbSet<VfsApplicant> VfsApplicants { get; }
    DbSet<HolidayPassenger> HolidayPassengers { get; }
    DbSet<OtherServiceApplicant> OtherServiceApplicants { get; }
    DbSet<SalesVisaApplication> SalesVisaApplications { get; }
    DbSet<SalesVisaApplicant> SalesVisaApplicants { get; }
    DbSet<VfsAppointment> VfsAppointments { get; }
    DbSet<HolidayPackage> HolidayPackages { get; }
    DbSet<Transportation> Transportations { get; }
    DbSet<OtherService> OtherServices { get; }
    DbSet<CashInHand> CashInHands { get; }
    DbSet<Deposit> Deposits { get; }
    DbSet<StaffMonthlyRecord> StaffMonthlyRecords { get; }
    DbSet<Lead> Leads { get; }
    DbSet<Dispute> Disputes { get; }
    DbSet<SalesVisaGovtFee> SalesVisaGovtFees { get; }
    DbSet<VfsAppointmentGovtFee> VfsAppointmentGovtFees { get; }
    DbSet<HolidayPackageGovtFee> HolidayPackageGovtFees { get; }
    DbSet<GovtFeePaymentLink> GovtFeePaymentLinks { get; }
    DbSet<HolidayPackageDocument> HolidayPackageDocuments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<CreditReceivable> CreditReceivables { get; }
    DbSet<ReceivableTransaction> ReceivableTransactions { get; }
    DbSet<CollectionBatch> CollectionBatches { get; }
    DbSet<ReceiptAllocation> ReceiptAllocations { get; }
    DbSet<ReceivablePayment> ReceivablePayments { get; }
    DbSet<CreditPolicy> CreditPolicies { get; }
    DbSet<CreditNote> CreditNotes { get; }
    DbSet<BadDebtWriteOff> BadDebtWriteOffs { get; }
    DbSet<BalanceTransaction> BalanceTransactions { get; }
    DbSet<CustomerAccountTransaction> CustomerAccountTransactions { get; }
    DbSet<SalesmanProfitRecord> SalesmanProfitRecords { get; }
    DbSet<SalesmanProfitAdjustment> SalesmanProfitAdjustments { get; }
    DbSet<Quotation> Quotations { get; }
    DbSet<QuotationItem> QuotationItems { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
