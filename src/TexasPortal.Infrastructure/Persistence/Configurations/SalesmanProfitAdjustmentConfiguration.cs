using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Infrastructure.Persistence.Configurations;

public class SalesmanProfitAdjustmentConfiguration : IEntityTypeConfiguration<SalesmanProfitAdjustment>
{
    public void Configure(EntityTypeBuilder<SalesmanProfitAdjustment> builder)
    {
        builder.ToTable("salesman_profit_adjustments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SalesmanProfitRecordId).HasColumnName("salesman_profit_record_id");
        builder.Property(x => x.SalesmanId).HasColumnName("salesman_id");
        builder.Property(x => x.BookingType).HasColumnName("booking_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(50);

        builder.Property(x => x.AdjustmentType)
            .HasColumnName("adjustment_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 3);
        builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceId).HasColumnName("reference_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();

        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.SalesmanProfitRecord)
            .WithMany()
            .HasForeignKey(x => x.SalesmanProfitRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint for Idempotency
        builder.HasIndex(x => x.IdempotencyKey)
            .HasDatabaseName("IX_salesman_profit_adjustments_idempotency_key")
            .IsUnique();

        builder.HasIndex(x => x.SalesmanProfitRecordId);
        builder.HasIndex(x => x.SalesmanId);
        builder.HasIndex(x => new { x.BookingType, x.BookingId });
        builder.HasIndex(x => x.AdjustmentType);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        builder.HasIndex(x => x.CreatedAt);
    }
}
