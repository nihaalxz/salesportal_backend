using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Infrastructure.Persistence;

public class SalesPortalDbContext : DbContext, ISalesPortalDbContext
{
    public SalesPortalDbContext(DbContextOptions<SalesPortalDbContext> options) : base(options)
    {
    }

    public DbSet<User> UsersDbSet { get; set; } = null!;
    public DbSet<Head> HeadsDbSet { get; set; } = null!;
    public DbSet<HeadLedger> HeadLedgersDbSet { get; set; } = null!;
    public DbSet<Receipt> ReceiptsDbSet { get; set; } = null!;
    public DbSet<FlightBooking> FlightBookingsDbSet { get; set; } = null!;
    public DbSet<FlightPassenger> FlightPassengersDbSet { get; set; } = null!;
    public DbSet<HotelBooking> HotelBookingsDbSet { get; set; } = null!;
    public DbSet<HotelGuest> HotelGuestsDbSet { get; set; } = null!;
    public DbSet<TransportPassenger> TransportPassengersDbSet { get; set; } = null!;
    public DbSet<VfsApplicant> VfsApplicantsDbSet { get; set; } = null!;
    public DbSet<HolidayPassenger> HolidayPassengersDbSet { get; set; } = null!;
    public DbSet<OtherServiceApplicant> OtherServiceApplicantsDbSet { get; set; } = null!;
    public DbSet<SalesVisaApplication> SalesVisaApplicationsDbSet { get; set; } = null!;
    public DbSet<SalesVisaApplicant> SalesVisaApplicantsDbSet { get; set; } = null!;
    public DbSet<VfsAppointment> VfsAppointmentsDbSet { get; set; } = null!;
    public DbSet<HolidayPackage> HolidayPackagesDbSet { get; set; } = null!;
    public DbSet<Transportation> TransportationsDbSet { get; set; } = null!;
    public DbSet<OtherService> OtherServicesDbSet { get; set; } = null!;
    public DbSet<CashInHand> CashInHandsDbSet { get; set; } = null!;
    public DbSet<Deposit> DepositsDbSet { get; set; } = null!;
    public DbSet<StaffMonthlyRecord> StaffMonthlyRecordsDbSet { get; set; } = null!;
    public DbSet<Lead> LeadsDbSet { get; set; } = null!;
    public DbSet<Dispute> DisputesDbSet { get; set; } = null!;
    public DbSet<SalesVisaGovtFee> SalesVisaGovtFeesDbSet { get; set; } = null!;
    public DbSet<VfsAppointmentGovtFee> VfsAppointmentGovtFeesDbSet { get; set; } = null!;
    public DbSet<HolidayPackageGovtFee> HolidayPackageGovtFeesDbSet { get; set; } = null!;
    public DbSet<GovtFeePaymentLink> GovtFeePaymentLinksDbSet { get; set; } = null!;
    public DbSet<HolidayPackageDocument> HolidayPackageDocumentsDbSet { get; set; } = null!;
    public DbSet<Notification> NotificationsDbSet { get; set; } = null!;
    public DbSet<IdempotencyRecord> IdempotencyRecordsDbSet { get; set; } = null!;

    // ISalesPortalDbContext query mappings (returns tracked DbSets directly for application-level flexibility)
    DbSet<User> ISalesPortalDbContext.Users => UsersDbSet;
    DbSet<Head> ISalesPortalDbContext.Heads => HeadsDbSet;
    DbSet<HeadLedger> ISalesPortalDbContext.HeadLedgers => HeadLedgersDbSet;
    DbSet<Receipt> ISalesPortalDbContext.Receipts => ReceiptsDbSet;
    DbSet<FlightBooking> ISalesPortalDbContext.FlightBookings => FlightBookingsDbSet;
    DbSet<FlightPassenger> ISalesPortalDbContext.FlightPassengers => FlightPassengersDbSet;
    DbSet<HotelBooking> ISalesPortalDbContext.HotelBookings => HotelBookingsDbSet;
    DbSet<HotelGuest> ISalesPortalDbContext.HotelGuests => HotelGuestsDbSet;
    DbSet<TransportPassenger> ISalesPortalDbContext.TransportPassengers => TransportPassengersDbSet;
    DbSet<VfsApplicant> ISalesPortalDbContext.VfsApplicants => VfsApplicantsDbSet;
    DbSet<HolidayPassenger> ISalesPortalDbContext.HolidayPassengers => HolidayPassengersDbSet;
    DbSet<OtherServiceApplicant> ISalesPortalDbContext.OtherServiceApplicants => OtherServiceApplicantsDbSet;
    DbSet<SalesVisaApplication> ISalesPortalDbContext.SalesVisaApplications => SalesVisaApplicationsDbSet;
    DbSet<SalesVisaApplicant> ISalesPortalDbContext.SalesVisaApplicants => SalesVisaApplicantsDbSet;
    DbSet<VfsAppointment> ISalesPortalDbContext.VfsAppointments => VfsAppointmentsDbSet;
    DbSet<HolidayPackage> ISalesPortalDbContext.HolidayPackages => HolidayPackagesDbSet;
    DbSet<Transportation> ISalesPortalDbContext.Transportations => TransportationsDbSet;
    DbSet<OtherService> ISalesPortalDbContext.OtherServices => OtherServicesDbSet;
    DbSet<CashInHand> ISalesPortalDbContext.CashInHands => CashInHandsDbSet;
    DbSet<Deposit> ISalesPortalDbContext.Deposits => DepositsDbSet;
    DbSet<StaffMonthlyRecord> ISalesPortalDbContext.StaffMonthlyRecords => StaffMonthlyRecordsDbSet;
    DbSet<Lead> ISalesPortalDbContext.Leads => LeadsDbSet;
    DbSet<Dispute> ISalesPortalDbContext.Disputes => DisputesDbSet;
    DbSet<SalesVisaGovtFee> ISalesPortalDbContext.SalesVisaGovtFees => SalesVisaGovtFeesDbSet;
    DbSet<VfsAppointmentGovtFee> ISalesPortalDbContext.VfsAppointmentGovtFees => VfsAppointmentGovtFeesDbSet;
    DbSet<HolidayPackageGovtFee> ISalesPortalDbContext.HolidayPackageGovtFees => HolidayPackageGovtFeesDbSet;
    DbSet<GovtFeePaymentLink> ISalesPortalDbContext.GovtFeePaymentLinks => GovtFeePaymentLinksDbSet;
    DbSet<HolidayPackageDocument> ISalesPortalDbContext.HolidayPackageDocuments => HolidayPackageDocumentsDbSet;
    DbSet<Notification> ISalesPortalDbContext.Notifications => NotificationsDbSet;
    DbSet<IdempotencyRecord> ISalesPortalDbContext.IdempotencyRecords => IdempotencyRecordsDbSet;
    public DbSet<OutboxMessage> OutboxMessagesDbSet { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages => OutboxMessagesDbSet;

    public DbSet<CreditReceivable> CreditReceivablesDbSet { get; set; } = null!;
    public DbSet<CreditReceivable> CreditReceivables => CreditReceivablesDbSet;

    public DbSet<ReceivableTransaction> ReceivableTransactionsDbSet { get; set; } = null!;
    public DbSet<ReceivableTransaction> ReceivableTransactions => ReceivableTransactionsDbSet;

    public DbSet<CollectionBatch> CollectionBatchesDbSet { get; set; } = null!;
    public DbSet<CollectionBatch> CollectionBatches => CollectionBatchesDbSet;

    public DbSet<ReceiptAllocation> ReceiptAllocationsDbSet { get; set; } = null!;
    public DbSet<ReceiptAllocation> ReceiptAllocations => ReceiptAllocationsDbSet;

    public DbSet<ReceivablePayment> ReceivablePaymentsDbSet { get; set; } = null!;
    public DbSet<ReceivablePayment> ReceivablePayments => ReceivablePaymentsDbSet;

    public DbSet<CreditPolicy> CreditPoliciesDbSet { get; set; } = null!;
    public DbSet<CreditPolicy> CreditPolicies => CreditPoliciesDbSet;

    public DbSet<CreditNote> CreditNotesDbSet { get; set; } = null!;
    public DbSet<CreditNote> CreditNotes => CreditNotesDbSet;

    public DbSet<BadDebtWriteOff> BadDebtWriteOffsDbSet { get; set; } = null!;
    public DbSet<BadDebtWriteOff> BadDebtWriteOffs => BadDebtWriteOffsDbSet;

    public DbSet<BalanceTransaction> BalanceTransactionsDbSet { get; set; } = null!;
    public DbSet<BalanceTransaction> BalanceTransactions => BalanceTransactionsDbSet;

    public DbSet<CustomerAccountTransaction> CustomerAccountTransactionsDbSet { get; set; } = null!;
    public DbSet<CustomerAccountTransaction> CustomerAccountTransactions => CustomerAccountTransactionsDbSet;

    public DbSet<SalesmanProfitRecord> SalesmanProfitRecordsDbSet { get; set; } = null!;
    public DbSet<SalesmanProfitRecord> SalesmanProfitRecords => SalesmanProfitRecordsDbSet;

    public DbSet<SalesmanProfitAdjustment> SalesmanProfitAdjustmentsDbSet { get; set; } = null!;
    public DbSet<SalesmanProfitAdjustment> SalesmanProfitAdjustments => SalesmanProfitAdjustmentsDbSet;

    public DbSet<Quotation> QuotationsDbSet { get; set; } = null!;
    public DbSet<Quotation> Quotations => QuotationsDbSet;

    public DbSet<QuotationItem> QuotationItemsDbSet { get; set; } = null!;
    public DbSet<QuotationItem> QuotationItems => QuotationItemsDbSet;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesPortalDbContext).Assembly);
        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages");
        modelBuilder.Entity<CreditReceivable>().ToTable("CreditReceivables");
        modelBuilder.Entity<ReceivableTransaction>().ToTable("ReceivableTransactions");
        modelBuilder.Entity<CollectionBatch>().ToTable("CollectionBatches");
        modelBuilder.Entity<ReceiptAllocation>().ToTable("ReceiptAllocations");
        modelBuilder.Entity<ReceivablePayment>().ToTable("ReceivablePayments");
        modelBuilder.Entity<CreditPolicy>().ToTable("CreditPolicies");
        modelBuilder.Entity<CreditNote>().ToTable("CreditNotes");
        modelBuilder.Entity<BadDebtWriteOff>().ToTable("BadDebtWriteOffs");
        modelBuilder.Entity<BalanceTransaction>(entity =>
        {
            entity.ToTable("BalanceTransactions");
            entity.Property(e => e.Amount).HasPrecision(15, 3);
            entity.HasIndex(e => e.HeadId);
            entity.HasIndex(e => e.CorrelationId);
        });
        modelBuilder.Entity<CustomerAccountTransaction>(entity =>
        {
            entity.ToTable("CustomerAccountTransactions");
            entity.Property(e => e.Amount).HasPrecision(15, 3);
            entity.HasIndex(e => e.HeadId);
            entity.HasIndex(e => e.CorrelationId);
        });
        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.ToTable("quotations");
            entity.Property(e => e.TotalAmount).HasPrecision(15, 3);
            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Quotation)
                  .HasForeignKey(i => i.QuotationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.ToTable("quotation_items");
            entity.Property(e => e.UnitPrice).HasPrecision(15, 3);
            entity.Property(e => e.TotalAmount).HasPrecision(15, 3);
        });

        // Ignore Head.Balance C# alias property in EF Core so only Head.Wallet is mapped to 'Wallet' DB column
        modelBuilder.Entity<Head>()
            .Ignore(h => h.Balance);

        // ── Optimistic Concurrency ──────────────────────────────────────────────
        modelBuilder.Entity<Receipt>()
            .Property(r => r.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        modelBuilder.Entity<CreditReceivable>()
            .Property(r => r.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        // ── ReceiptType stored as int ───────────────────────────────────────────
        modelBuilder.Entity<Receipt>()
            .Property(r => r.ReceiptType)
            .HasConversion<int>()
            .HasDefaultValue(TexasPortal.Domain.Enums.ReceiptType.General);

        // ── Idempotent Ledger: unique filtered index on CorrelationId ───────────
        modelBuilder.Entity<ReceivableTransaction>()
            .HasIndex(t => t.CorrelationId)
            .IsUnique()
            .HasFilter("[CorrelationId] IS NOT NULL")
            .HasDatabaseName("IX_ReceivableTransaction_CorrelationId");

        base.OnModelCreating(modelBuilder);
    }
}
