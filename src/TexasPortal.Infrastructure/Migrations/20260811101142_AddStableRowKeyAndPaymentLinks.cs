using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStableRowKeyAndPaymentLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "row_key",
                table: "vfs_appointment_govt_fees",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "row_key",
                table: "sales_visa_govt_fees",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "row_key",
                table: "holiday_package_govt_fees",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "govt_fee_payment_links",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fee_table = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    row_key = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    accounts_payment_id = table.Column<long>(type: "bigint", nullable: false),
                    amount_at_link_time = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    linked_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    superseded_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    superseded_reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_govt_fee_payment_links", x => x.id);
                });

            migrationBuilder.Sql("UPDATE sales_visa_govt_fees SET row_key = LOWER(NEWID()) WHERE row_key IS NULL OR row_key = '';");
            migrationBuilder.Sql("UPDATE vfs_appointment_govt_fees SET row_key = LOWER(NEWID()) WHERE row_key IS NULL OR row_key = '';");
            migrationBuilder.Sql("UPDATE holiday_package_govt_fees SET row_key = LOWER(NEWID()) WHERE row_key IS NULL OR row_key = '';");

            migrationBuilder.CreateIndex(
                name: "uq_vfs_appointment_govt_fees_row_key",
                table: "vfs_appointment_govt_fees",
                column: "row_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_visa_govt_fees_row_key",
                table: "sales_visa_govt_fees",
                column: "row_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_holiday_package_govt_fees_row_key",
                table: "holiday_package_govt_fees",
                column: "row_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_govt_fee_payment_links_active",
                table: "govt_fee_payment_links",
                columns: new[] { "row_key", "superseded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "govt_fee_payment_links");

            migrationBuilder.DropIndex(
                name: "uq_vfs_appointment_govt_fees_row_key",
                table: "vfs_appointment_govt_fees");

            migrationBuilder.DropIndex(
                name: "uq_sales_visa_govt_fees_row_key",
                table: "sales_visa_govt_fees");

            migrationBuilder.DropIndex(
                name: "uq_holiday_package_govt_fees_row_key",
                table: "holiday_package_govt_fees");

            migrationBuilder.DropColumn(
                name: "row_key",
                table: "vfs_appointment_govt_fees");

            migrationBuilder.DropColumn(
                name: "row_key",
                table: "sales_visa_govt_fees");

            migrationBuilder.DropColumn(
                name: "row_key",
                table: "holiday_package_govt_fees");
        }
    }
}
