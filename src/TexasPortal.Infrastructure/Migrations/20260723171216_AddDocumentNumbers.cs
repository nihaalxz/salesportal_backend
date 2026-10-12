using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "vfs_appointments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "transportations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "sales_visa_applications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "receipts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "other_services",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "hotel_bookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "holiday_packages",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "flight_bookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "disputes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "deposits",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // Backfill existing rows with unique document numbers using SQL sequences
            migrationBuilder.Sql(@"
                -- Create sequences (idempotent)
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_SalesVisaDocumentNo')
                    CREATE SEQUENCE dbo.Seq_SalesVisaDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_FlightDocumentNo')
                    CREATE SEQUENCE dbo.Seq_FlightDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_HotelDocumentNo')
                    CREATE SEQUENCE dbo.Seq_HotelDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_HolidayDocumentNo')
                    CREATE SEQUENCE dbo.Seq_HolidayDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_TransportDocumentNo')
                    CREATE SEQUENCE dbo.Seq_TransportDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_VfsDocumentNo')
                    CREATE SEQUENCE dbo.Seq_VfsDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_OtherServiceDocumentNo')
                    CREATE SEQUENCE dbo.Seq_OtherServiceDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_ReceiptDocumentNo')
                    CREATE SEQUENCE dbo.Seq_ReceiptDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_DisputeDocumentNo')
                    CREATE SEQUENCE dbo.Seq_DisputeDocumentNo START WITH 1 INCREMENT BY 1;
                IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_DepositDocumentNo')
                    CREATE SEQUENCE dbo.Seq_DepositDocumentNo START WITH 1 INCREMENT BY 1;

                -- Backfill: assign unique document numbers to existing rows (empty document_number)
                UPDATE sales_visa_applications
                SET document_number = 'TXSVISA' + FORMAT(NEXT VALUE FOR dbo.Seq_SalesVisaDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE flight_bookings
                SET document_number = 'TXSFLT' + FORMAT(NEXT VALUE FOR dbo.Seq_FlightDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE hotel_bookings
                SET document_number = 'TXSHOTL' + FORMAT(NEXT VALUE FOR dbo.Seq_HotelDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE holiday_packages
                SET document_number = 'TXSHOL' + FORMAT(NEXT VALUE FOR dbo.Seq_HolidayDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE transportations
                SET document_number = 'TXSTRANS' + FORMAT(NEXT VALUE FOR dbo.Seq_TransportDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE vfs_appointments
                SET document_number = 'TXSVFS' + FORMAT(NEXT VALUE FOR dbo.Seq_VfsDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE other_services
                SET document_number = 'TXSOTHR' + FORMAT(NEXT VALUE FOR dbo.Seq_OtherServiceDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE receipts
                SET document_number = 'TXSRCPT' + FORMAT(NEXT VALUE FOR dbo.Seq_ReceiptDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE disputes
                SET document_number = 'TXSDISP' + FORMAT(NEXT VALUE FOR dbo.Seq_DisputeDocumentNo, 'D6')
                WHERE document_number = '';

                UPDATE deposits
                SET document_number = 'TXSDEP' + FORMAT(NEXT VALUE FOR dbo.Seq_DepositDocumentNo, 'D6')
                WHERE document_number = '';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointments_document_number",
                table: "vfs_appointments",
                column: "document_number",
                unique: true);


            migrationBuilder.CreateIndex(
                name: "IX_transportations_document_number",
                table: "transportations",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_visa_applications_document_number",
                table: "sales_visa_applications",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipts_document_number",
                table: "receipts",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_other_services_document_number",
                table: "other_services",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_bookings_document_number",
                table: "hotel_bookings",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holiday_packages_document_number",
                table: "holiday_packages",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_flight_bookings_document_number",
                table: "flight_bookings",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disputes_document_number",
                table: "disputes",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposits_document_number",
                table: "deposits",
                column: "document_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vfs_appointments_document_number",
                table: "vfs_appointments");

            migrationBuilder.DropIndex(
                name: "IX_transportations_document_number",
                table: "transportations");

            migrationBuilder.DropIndex(
                name: "IX_sales_visa_applications_document_number",
                table: "sales_visa_applications");

            migrationBuilder.DropIndex(
                name: "IX_receipts_document_number",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_other_services_document_number",
                table: "other_services");

            migrationBuilder.DropIndex(
                name: "IX_hotel_bookings_document_number",
                table: "hotel_bookings");

            migrationBuilder.DropIndex(
                name: "IX_holiday_packages_document_number",
                table: "holiday_packages");

            migrationBuilder.DropIndex(
                name: "IX_flight_bookings_document_number",
                table: "flight_bookings");

            migrationBuilder.DropIndex(
                name: "IX_disputes_document_number",
                table: "disputes");

            migrationBuilder.DropIndex(
                name: "IX_deposits_document_number",
                table: "deposits");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "vfs_appointments");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "transportations");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "sales_visa_applications");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "other_services");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "holiday_packages");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "flight_bookings");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "deposits");
        }
    }
}

