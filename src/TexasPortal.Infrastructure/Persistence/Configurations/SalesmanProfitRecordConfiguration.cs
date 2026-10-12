using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Infrastructure.Persistence.Configurations;

public class SalesmanProfitRecordConfiguration : IEntityTypeConfiguration<SalesmanProfitRecord>
{
    public void Configure(EntityTypeBuilder<SalesmanProfitRecord> builder)
    {
        builder.ToTable("salesman_profit_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SalesmanId).HasColumnName("salesman_id");
        builder.Property(x => x.BookingType).HasColumnName("booking_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(50);

        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.FinalSupplierCost).HasColumnName("final_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ProfitAmount).HasColumnName("profit_amount").HasPrecision(15, 3);

        builder.Property(x => x.ProfitType)
            .HasColumnName("profit_type")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SupplierCostSource)
            .HasColumnName("supplier_cost_source")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.AccountsApprovedTotal).HasColumnName("accounts_approved_total").HasPrecision(15, 3);
        builder.Property(x => x.RejectionFee).HasColumnName("rejection_fee").HasPrecision(15, 3);
        builder.Property(x => x.IsFinal).HasColumnName("is_final");

        builder.Property(x => x.BookingCreatedAt).HasColumnName("booking_created_at");
        builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
        builder.Property(x => x.LastReconciledAt).HasColumnName("last_reconciled_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Salesman)
            .WithMany()
            .HasForeignKey(x => x.SalesmanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: BookingType + BookingId must identify a single profit record
        builder.HasIndex(x => new { x.BookingType, x.BookingId })
            .HasDatabaseName("IX_salesman_profit_records_BookingType_BookingId")
            .IsUnique();

        builder.HasIndex(x => x.SalesmanId);
        builder.HasIndex(x => new { x.SalesmanId, x.BookingCreatedAt });
        builder.HasIndex(x => x.IsFinal);
    }
}
