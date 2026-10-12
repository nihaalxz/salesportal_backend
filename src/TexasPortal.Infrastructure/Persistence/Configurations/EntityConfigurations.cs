using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Avatar).HasColumnName("avatar").HasMaxLength(255);
        builder.Property(x => x.UserType).HasColumnName("user_type").HasMaxLength(255);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.Password).HasColumnName("password").HasMaxLength(255);
        builder.Property(x => x.RememberToken).HasColumnName("remember_token").HasMaxLength(100);
        builder.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(x => x.LastLoginIp).HasColumnName("last_login_ip").HasMaxLength(255);
        builder.Property(x => x.ProfitTarget).HasColumnName("profit_target").HasPrecision(15, 3);
        builder.Property(x => x.MonthlyTarget).HasColumnName("monthly_target").HasPrecision(15, 3);
        builder.Property(x => x.ProfitGenerated).HasColumnName("profit_generated").HasPrecision(15, 3);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}

public class HeadConfiguration : IEntityTypeConfiguration<Head>
{
    public void Configure(EntityTypeBuilder<Head> builder)
    {
        builder.ToTable("heads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(255);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(255);
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(255);
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(255);
        builder.Property(x => x.MobileNo).HasColumnName("mobile_no").HasMaxLength(20);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Nationality).HasColumnName("nationality").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.Wallet).HasColumnName("wallet").HasPrecision(15, 3);
        builder.Ignore(x => x.Balance);
        builder.Property(x => x.Address).HasColumnName("address");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HeadLedgerConfiguration : IEntityTypeConfiguration<HeadLedger>
{
    public void Configure(EntityTypeBuilder<HeadLedger> builder)
    {
        builder.ToTable("head_ledgers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ServiceType).HasColumnName("service_type").HasMaxLength(255);
        builder.Property(x => x.ServiceId).HasColumnName("service_id");
        builder.Property(x => x.Debit).HasColumnName("debit").HasPrecision(15, 3);
        builder.Property(x => x.Credit).HasColumnName("credit").HasPrecision(15, 3);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Head)
            .WithMany(h => h.Ledgers)
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.HeadId);
        builder.HasIndex(x => new { x.ServiceType, x.ServiceId })
            .HasDatabaseName("unique_service_ledger_entry")
            .IsUnique();
    }
}

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.ReceiptId).HasColumnName("receipt_id").HasMaxLength(255);
        builder.HasIndex(x => x.ReceiptId).IsUnique();
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.BookingId).HasColumnName("booking_id").HasMaxLength(255);
        builder.Property(x => x.Narration).HasColumnName("narration").HasMaxLength(255);
        builder.Property(x => x.Service).HasColumnName("service").HasMaxLength(255);
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 3);
        builder.Property(x => x.PaymentMode).HasColumnName("payment_mode").HasMaxLength(20);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.ReceiptType).HasColumnName("ReceiptType").HasDefaultValue(TexasPortal.Domain.Enums.ReceiptType.General);
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion().IsRequired(false);

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FlightBookingConfiguration : IEntityTypeConfiguration<FlightBooking>
{
    public void Configure(EntityTypeBuilder<FlightBooking> builder)
    {
        builder.ToTable("flight_bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.CareOf).HasColumnName("care_of").HasMaxLength(255);
        builder.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(255);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.FromLocation).HasColumnName("from_location").HasMaxLength(255);
        builder.Property(x => x.ToLocation).HasColumnName("to_location").HasMaxLength(255);
        builder.Property(x => x.TravelDate).HasColumnName("travel_date");
        builder.Property(x => x.AirlineCompany).HasColumnName("airline_company").HasMaxLength(255);
        builder.Property(x => x.TicketPnr).HasColumnName("ticket_pnr").HasMaxLength(255);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class FlightPassengerConfiguration : IEntityTypeConfiguration<FlightPassenger>
{
    public void Configure(EntityTypeBuilder<FlightPassenger> builder)
    {
        builder.ToTable("flight_passengers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FlightBookingId).HasColumnName("flight_booking_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.PassengerName).HasColumnName("passenger_name").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.TicketPnr).HasColumnName("ticket_pnr").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.FlightBooking)
            .WithMany(b => b.Passengers)
            .HasForeignKey(x => x.FlightBookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HotelBookingConfiguration : IEntityTypeConfiguration<HotelBooking>
{
    public void Configure(EntityTypeBuilder<HotelBooking> builder)
    {
        builder.ToTable("hotel_bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.GuestName).HasColumnName("guest_name").HasMaxLength(255);
        builder.Property(x => x.HotelName).HasColumnName("hotel_name").HasMaxLength(255);
        builder.Property(x => x.CareOf).HasColumnName("care_of").HasMaxLength(255);
        builder.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(255);
        builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(255);
        builder.Property(x => x.Days).HasColumnName("days");
        builder.Property(x => x.CheckInDate).HasColumnName("check_in_date");
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.OrderStatus).HasColumnName("order_status").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class SalesVisaApplicationConfiguration : IEntityTypeConfiguration<SalesVisaApplication>
{
    public void Configure(EntityTypeBuilder<SalesVisaApplication> builder)
    {
        builder.ToTable("sales_visa_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.CountryId).HasColumnName("country_id");
        builder.Property(x => x.VisaPackId).HasColumnName("visa_pack_id");
        builder.Property(x => x.VisaPackName).HasColumnName("visa_pack_name").HasMaxLength(255);
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.VisaDocument).HasColumnName("visa_document").HasMaxLength(255);
        builder.Property(x => x.ApprovalNote).HasColumnName("approval_note");
        builder.Property(x => x.RejectionDocument).HasColumnName("rejection_document").HasMaxLength(255);
        builder.Property(x => x.RejectionNote).HasColumnName("rejection_note");
        builder.Property(x => x.RefundNote).HasColumnName("refund_note");
        builder.Property(x => x.DocumentRequestNote).HasColumnName("document_request_note");
        builder.Property(x => x.DocumentRequestDate).HasColumnName("document_request_date");
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.RejectionFee).HasColumnName("rejection_fee").HasPrecision(15, 3);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class SalesVisaApplicantConfiguration : IEntityTypeConfiguration<SalesVisaApplicant>
{
    public void Configure(EntityTypeBuilder<SalesVisaApplicant> builder)
    {
        builder.ToTable("sales_visa_applicants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SalesVisaApplicationId).HasColumnName("sales_visa_application_id");
        builder.Property(x => x.VisaAppId).HasColumnName("visa_app_id").HasMaxLength(255);
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.FName).HasColumnName("f_name").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.LName).HasColumnName("l_name").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.Dob).HasColumnName("dob");
        builder.Property(x => x.Nationality).HasColumnName("nationality").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.CareOf).HasColumnName("care_of").HasMaxLength(255);
        builder.Property(x => x.ContactNo).HasColumnName("contact_no").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.EmailId).HasColumnName("email_id").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.PassportNo).HasColumnName("passport_no").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.IqamaHotelNo).HasColumnName("iqama_hotel_no").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.UnpaidAmount).HasColumnName("unpaid_amount").HasPrecision(15, 3);
        builder.Property(x => x.BankDoc).HasColumnName("bank_doc").HasMaxLength(255);
        builder.Property(x => x.PassportCopy).HasColumnName("passport_copy").HasMaxLength(255);
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.ProcessedBy).HasColumnName("processed_by");
        builder.Property(x => x.PaymentConfirmedBy).HasColumnName("payment_confirmed_by");
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.GovtFeeAmount).HasColumnName("govt_fee_amount").HasPrecision(15, 3);
        builder.Property(x => x.GovtFeeReference).HasColumnName("govt_fee_reference").HasMaxLength(255);
        builder.Property(x => x.GovtFeeStatus).HasColumnName("govt_fee_status").HasMaxLength(255);
        builder.Property(x => x.GovtFeePaymentProof).HasColumnName("govt_fee_payment_proof").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.SalesVisaApplication)
            .WithMany(a => a.Applicants)
            .HasForeignKey(x => x.SalesVisaApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VfsAppointmentConfiguration : IEntityTypeConfiguration<VfsAppointment>
{
    public void Configure(EntityTypeBuilder<VfsAppointment> builder)
    {
        builder.ToTable("vfs_appointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(255);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(255);
        builder.Property(x => x.VisaType).HasColumnName("visa_type").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.PaymentMode).HasColumnName("payment_mode").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.AppointmentDate).HasColumnName("appointment_date");
        builder.Property(x => x.AppointmentTime).HasColumnName("appointment_time");
        builder.Property(x => x.VfsReceipt).HasColumnName("vfs_receipt").HasMaxLength(255);
        builder.Property(x => x.PassportCopy).HasColumnName("passport_copy").HasMaxLength(255);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class HolidayPackageConfiguration : IEntityTypeConfiguration<HolidayPackage>
{
    public void Configure(EntityTypeBuilder<HolidayPackage> builder)
    {
        builder.ToTable("holiday_packages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.PassengerName).HasColumnName("passenger_name").HasMaxLength(255);
        builder.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(255);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(255);
        builder.Property(x => x.Days).HasColumnName("days");
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.PaymentMode).HasColumnName("payment_mode").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class TransportationConfiguration : IEntityTypeConfiguration<Transportation>
{
    public void Configure(EntityTypeBuilder<Transportation> builder)
    {
        builder.ToTable("transportations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.CareOf).HasColumnName("care_of").HasMaxLength(255);
        builder.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(255);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.Pickup).HasColumnName("pickup").HasMaxLength(255);
        builder.Property(x => x.Drop).HasColumnName("drop").HasMaxLength(255);
        builder.Property(x => x.DropDate).HasColumnName("drop_date");
        builder.Property(x => x.DropTime).HasColumnName("drop_time");
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.Vehicle).HasColumnName("vehicle").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.IsEdited).HasColumnName("is_edited");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class OtherServiceConfiguration : IEntityTypeConfiguration<OtherService>
{
    public void Configure(EntityTypeBuilder<OtherService> builder)
    {
        builder.ToTable("other_services");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.ClientType).HasColumnName("client_type").HasMaxLength(255);
        builder.Property(x => x.Agent).HasColumnName("agent").HasMaxLength(255);
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.CareOf).HasColumnName("care_of").HasMaxLength(255);
        builder.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(255);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(255);
        builder.Property(x => x.Narration).HasColumnName("narration");
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
        builder.Property(x => x.BankId).HasColumnName("bank_id");
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(255);
        builder.Property(x => x.IsDealClosed).HasColumnName("is_deal_closed");
        builder.Property(x => x.OperationClosed).HasColumnName("operation_closed");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
    }
}

public class CashInHandConfiguration : IEntityTypeConfiguration<CashInHand>
{
    public void Configure(EntityTypeBuilder<CashInHand> builder)
    {
        builder.ToTable("cash_in_hands");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 3);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<CashInHand>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.ToTable("deposits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 3);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(255);
        builder.Property(x => x.DestinationId).HasColumnName("destination_id");
        builder.Property(x => x.Narration).HasColumnName("narration").HasMaxLength(255);
        builder.Property(x => x.Date).HasColumnName("date");
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.ApiTransactionId).HasColumnName("api_transaction_id").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffMonthlyRecordConfiguration : IEntityTypeConfiguration<StaffMonthlyRecord>
{
    public void Configure(EntityTypeBuilder<StaffMonthlyRecord> builder)
    {
        builder.ToTable("staff_monthly_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Month).HasColumnName("month").HasMaxLength(255);
        builder.Property(x => x.Year).HasColumnName("year");
        builder.Property(x => x.ProfitGenerated).HasColumnName("profit_generated").HasPrecision(15, 3);
        builder.Property(x => x.Incentive).HasColumnName("incentive").HasPrecision(15, 3);
        builder.Property(x => x.ProfitTarget).HasColumnName("profit_target").HasPrecision(15, 3);
        builder.Property(x => x.MonthlyTarget).HasColumnName("monthly_target").HasPrecision(15, 3);
        builder.Property(x => x.TargetAchieved).HasColumnName("target_achieved");
        builder.Property(x => x.BasicSalary).HasColumnName("basic_salary").HasPrecision(15, 3);
        builder.Property(x => x.HousingAllowance).HasColumnName("housing_allowance").HasPrecision(15, 3);
        builder.Property(x => x.TransportAllowance).HasColumnName("transport_allowance").HasPrecision(15, 3);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany(u => u.MonthlyRecords)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(255);
        builder.Property(x => x.MobileNo).HasColumnName("mobile_no").HasMaxLength(255);
        builder.Property(x => x.Services).HasColumnName("services");
        builder.Property(x => x.LeadSource).HasColumnName("lead_source").HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
        builder.Property(x => x.ReminderDate).HasColumnName("reminder_date");
        builder.Property(x => x.ReminderActive).HasColumnName("reminder_active");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(30);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.HeadId).HasColumnName("head_id");
        builder.Property(x => x.DisputableId).HasColumnName("disputable_id");
        builder.Property(x => x.DisputableType).HasColumnName("disputable_type").HasMaxLength(255);
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
        builder.Property(x => x.Resolution).HasColumnName("resolution");
        builder.Property(x => x.ResolutionNote).HasColumnName("resolution_note");
        builder.Property(x => x.ResolvedBy).HasColumnName("resolved_by").HasMaxLength(255);
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(x => x.IsRefund).HasColumnName("is_refund");
        builder.Property(x => x.RefundAmount).HasColumnName("refund_amount").HasPrecision(15, 3);
        builder.Property(x => x.ApprovedRefundAmount).HasColumnName("approved_refund_amount").HasPrecision(15, 3);
        builder.Property(x => x.RefundProcessed).HasColumnName("refund_processed");
        builder.Property(x => x.RefundStatus).HasColumnName("refund_status").HasMaxLength(255);
        builder.Property(x => x.RefundDate).HasColumnName("refund_date");
        builder.Property(x => x.Iban).HasColumnName("iban").HasMaxLength(100);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalesVisaGovtFeeConfiguration : IEntityTypeConfiguration<SalesVisaGovtFee>
{
    public void Configure(EntityTypeBuilder<SalesVisaGovtFee> builder)
    {
        builder.ToTable("sales_visa_govt_fees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RowKey).HasColumnName("row_key").HasMaxLength(36).IsRequired();
        builder.HasIndex(x => x.RowKey).IsUnique().HasDatabaseName("uq_sales_visa_govt_fees_row_key");
        builder.Property(x => x.SalesVisaApplicantId).HasColumnName("sales_visa_applicant_id");
        builder.Property(x => x.AccountsPaymentId).HasColumnName("accounts_payment_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 3);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.SalesVisaApplicant)
            .WithMany(a => a.GovtFees)
            .HasForeignKey(x => x.SalesVisaApplicantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VfsAppointmentGovtFeeConfiguration : IEntityTypeConfiguration<VfsAppointmentGovtFee>
{
    public void Configure(EntityTypeBuilder<VfsAppointmentGovtFee> builder)
    {
        builder.ToTable("vfs_appointment_govt_fees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RowKey).HasColumnName("row_key").HasMaxLength(36).IsRequired();
        builder.HasIndex(x => x.RowKey).IsUnique().HasDatabaseName("uq_vfs_appointment_govt_fees_row_key");
        builder.Property(x => x.VfsAppointmentId).HasColumnName("vfs_appointment_id");
        builder.Property(x => x.AccountsPaymentId).HasColumnName("accounts_payment_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 3);
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.VfsAppointment)
            .WithMany(a => a.GovtFees)
            .HasForeignKey(x => x.VfsAppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HolidayPackageGovtFeeConfiguration : IEntityTypeConfiguration<HolidayPackageGovtFee>
{
    public void Configure(EntityTypeBuilder<HolidayPackageGovtFee> builder)
    {
        builder.ToTable("holiday_package_govt_fees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RowKey).HasColumnName("row_key").HasMaxLength(36).IsRequired();
        builder.HasIndex(x => x.RowKey).IsUnique().HasDatabaseName("uq_holiday_package_govt_fees_row_key");
        builder.Property(x => x.HolidayPackageId).HasColumnName("holiday_package_id");
        builder.Property(x => x.AccountsPaymentId).HasColumnName("accounts_payment_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 3);
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
        builder.Property(x => x.PaymentProof).HasColumnName("payment_proof").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.HolidayPackage)
            .WithMany(p => p.GovtFees)
            .HasForeignKey(x => x.HolidayPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GovtFeePaymentLinkConfiguration : IEntityTypeConfiguration<GovtFeePaymentLink>
{
    public void Configure(EntityTypeBuilder<GovtFeePaymentLink> builder)
    {
        builder.ToTable("govt_fee_payment_links");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FeeTable).HasColumnName("fee_table").HasMaxLength(50).IsRequired();
        builder.Property(x => x.RowKey).HasColumnName("row_key").HasMaxLength(36).IsRequired();
        builder.Property(x => x.AccountsPaymentId).HasColumnName("accounts_payment_id").IsRequired();
        builder.Property(x => x.AmountAtLinkTime).HasColumnName("amount_at_link_time").HasPrecision(15, 3).IsRequired();
        builder.Property(x => x.LinkedAt).HasColumnName("linked_at").IsRequired();
        builder.Property(x => x.SupersededAt).HasColumnName("superseded_at");
        builder.Property(x => x.SupersededReason).HasColumnName("superseded_reason").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.RowKey, x.SupersededAt })
            .HasDatabaseName("ix_govt_fee_payment_links_active");
    }
}

public class HolidayPackageDocumentConfiguration : IEntityTypeConfiguration<HolidayPackageDocument>
{
    public void Configure(EntityTypeBuilder<HolidayPackageDocument> builder)
    {
        builder.ToTable("holiday_package_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HolidayPackageId).HasColumnName("holiday_package_id");
        builder.Property(x => x.DocumentPath).HasColumnName("document_path").HasMaxLength(255);
        builder.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.HolidayPackage)
            .WithMany(p => p.Documents)
            .HasForeignKey(x => x.HolidayPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(255);
        builder.Property(x => x.NotifiableType).HasColumnName("notifiable_type").HasMaxLength(255);
        builder.Property(x => x.NotifiableId).HasColumnName("notifiable_id");
        builder.Property(x => x.Data).HasColumnName("data");
        builder.Property(x => x.ReadAt).HasColumnName("read_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResponseCode).HasColumnName("response_code");
        builder.Property(x => x.ResponsePayload).HasColumnName("response_payload");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();

        // Unique index on idempotency key
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("IX_IdempotencyRecords_Key");
    }
}

public class CollectionBatchConfiguration : IEntityTypeConfiguration<CollectionBatch>
{
    public void Configure(EntityTypeBuilder<CollectionBatch> builder)
    {
        builder.ToTable("CollectionBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalAmount).HasPrecision(15, 3);
    }
}

public class CreditPolicyConfiguration : IEntityTypeConfiguration<CreditPolicy>
{
    public void Configure(EntityTypeBuilder<CreditPolicy> builder)
    {
        builder.ToTable("CreditPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaxCreditLimit).HasPrecision(15, 3);
    }
}

public class CreditReceivableConfiguration : IEntityTypeConfiguration<CreditReceivable>
{
    public void Configure(EntityTypeBuilder<CreditReceivable> builder)
    {
        builder.ToTable("CreditReceivables");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalAmount).HasPrecision(15, 3);
        builder.Property(x => x.CachedOutstandingAmount).HasPrecision(15, 3);
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired(false);
        builder.Ignore(x => x.FinancialSourceType);
    }
}

public class BadDebtWriteOffConfiguration : IEntityTypeConfiguration<BadDebtWriteOff>
{
    public void Configure(EntityTypeBuilder<BadDebtWriteOff> builder)
    {
        builder.ToTable("BadDebtWriteOffs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(15, 3);
    }
}

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("CreditNotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(15, 3);
    }
}

public class ReceiptAllocationConfiguration : IEntityTypeConfiguration<ReceiptAllocation>
{
    public void Configure(EntityTypeBuilder<ReceiptAllocation> builder)
    {
        builder.ToTable("ReceiptAllocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AllocatedAmount).HasPrecision(15, 3);
    }
}

public class ReceivablePaymentConfiguration : IEntityTypeConfiguration<ReceivablePayment>
{
    public void Configure(EntityTypeBuilder<ReceivablePayment> builder)
    {
        builder.ToTable("ReceivablePayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(15, 3);
    }
}

public class ReceivableTransactionConfiguration : IEntityTypeConfiguration<ReceivableTransaction>
{
    public void Configure(EntityTypeBuilder<ReceivableTransaction> builder)
    {
        builder.ToTable("ReceivableTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(15, 3);
        builder.HasIndex(t => t.CorrelationId)
            .IsUnique()
            .HasFilter("[CorrelationId] IS NOT NULL")
            .HasDatabaseName("IX_ReceivableTransaction_CorrelationId");
    }
}

public class BalanceTransactionConfiguration : IEntityTypeConfiguration<BalanceTransaction>
{
    public void Configure(EntityTypeBuilder<BalanceTransaction> builder)
    {
        builder.ToTable("BalanceTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HeadId).HasColumnName("HeadId");
        builder.Property(x => x.Amount).HasPrecision(15, 3);
        builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("BHD");
        builder.Property(x => x.CorrelationId).HasMaxLength(450);
        builder.Property(x => x.ReferenceId).HasMaxLength(255);

        builder.HasOne(x => x.Head)
            .WithMany(x => x.BalanceTransactions)
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.HeadId).HasDatabaseName("IX_BalanceTransactions_HeadId");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_BalanceTransactions_CorrelationId");
    }
}

public class CustomerAccountTransactionConfiguration : IEntityTypeConfiguration<CustomerAccountTransaction>
{
    public void Configure(EntityTypeBuilder<CustomerAccountTransaction> builder)
    {
        builder.ToTable("CustomerAccountTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HeadId).HasColumnName("HeadId");
        builder.Property(x => x.Amount).HasPrecision(15, 3);
        builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("BHD");
        builder.Property(x => x.CorrelationId).HasMaxLength(450);
        builder.Property(x => x.ReferenceId).HasMaxLength(255);

        builder.HasOne(x => x.Head)
            .WithMany()
            .HasForeignKey(x => x.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.HeadId).HasDatabaseName("IX_CustomerAccountTransactions_HeadId");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_CustomerAccountTransactions_CorrelationId");
    }
}


public class HotelGuestConfiguration : IEntityTypeConfiguration<HotelGuest>
{
    public void Configure(EntityTypeBuilder<HotelGuest> builder)
    {
        builder.ToTable("hotel_guests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HotelBookingId).HasColumnName("hotel_booking_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.GuestName).HasColumnName("guest_name").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.HotelBooking)
            .WithMany(b => b.Guests)
            .HasForeignKey(x => x.HotelBookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TransportPassengerConfiguration : IEntityTypeConfiguration<TransportPassenger>
{
    public void Configure(EntityTypeBuilder<TransportPassenger> builder)
    {
        builder.ToTable("transport_passengers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TransportationId).HasColumnName("transportation_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Transportation)
            .WithMany(b => b.Passengers)
            .HasForeignKey(x => x.TransportationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VfsApplicantConfiguration : IEntityTypeConfiguration<VfsApplicant>
{
    public void Configure(EntityTypeBuilder<VfsApplicant> builder)
    {
        builder.ToTable("vfs_applicants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.VfsAppointmentId).HasColumnName("vfs_appointment_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.PassportCopy).HasColumnName("passport_copy").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.VfsAppointment)
            .WithMany(b => b.Applicants)
            .HasForeignKey(x => x.VfsAppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HolidayPassengerConfiguration : IEntityTypeConfiguration<HolidayPassenger>
{
    public void Configure(EntityTypeBuilder<HolidayPassenger> builder)
    {
        builder.ToTable("holiday_passengers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HolidayPackageId).HasColumnName("holiday_package_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.PassengerName).HasColumnName("passenger_name").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.HolidayPackage)
            .WithMany(b => b.Passengers)
            .HasForeignKey(x => x.HolidayPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OtherServiceApplicantConfiguration : IEntityTypeConfiguration<OtherServiceApplicant>
{
    public void Configure(EntityTypeBuilder<OtherServiceApplicant> builder)
    {
        builder.ToTable("other_service_applicants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OtherServiceId).HasColumnName("other_service_id");
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount").HasPrecision(15, 3);
        builder.Property(x => x.SupplierCost).HasColumnName("supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.ExpectedSupplierCost).HasColumnName("expected_supplier_cost").HasPrecision(15, 3);
        builder.Property(x => x.Profit).HasColumnName("profit").HasPrecision(15, 3);
        builder.Property(x => x.IsInhouse).HasColumnName("is_inhouse");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierName).HasColumnName("supplier_name").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.OtherService)
            .WithMany(b => b.Applicants)
            .HasForeignKey(x => x.OtherServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
