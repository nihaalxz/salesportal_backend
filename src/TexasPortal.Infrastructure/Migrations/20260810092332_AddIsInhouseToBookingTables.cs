using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsInhouseToBookingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "is_inhouse",
                table: "vfs_appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'supplier_name'
                )
                BEGIN
                    ALTER TABLE vfs_appointments ADD supplier_name nvarchar(255) NULL;
                END
            ");

            migrationBuilder.AddColumn<bool>(
                name: "is_inhouse",
                table: "transportations",
                type: "bit",
                nullable: false,
                defaultValue: false);



            migrationBuilder.AddColumn<bool>(
                name: "is_inhouse",
                table: "sales_visa_applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_inhouse",
                table: "other_services",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_inhouse",
                table: "holiday_packages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'supplier_name'
                )
                BEGIN
                    ALTER TABLE holiday_packages ADD supplier_name nvarchar(255) NULL;
                END
            ");

            migrationBuilder.AddColumn<decimal>(
                name: "expected_supplier_cost",
                table: "flight_bookings",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_inhouse",
                table: "flight_bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "profit",
                table: "flight_bookings",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "supplier_id",
                table: "flight_bookings",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'supplier_name'
                )
                BEGIN
                    ALTER TABLE flight_bookings ADD supplier_name nvarchar(255) NULL;
                END
            ");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalanceTransactions");

            migrationBuilder.DropTable(
                name: "CustomerAccountTransactions");

            migrationBuilder.DropColumn(
                name: "is_inhouse",
                table: "vfs_appointments");

            migrationBuilder.DropColumn(
                name: "is_inhouse",
                table: "transportations");

            migrationBuilder.DropColumn(
                name: "document_request_date",
                table: "sales_visa_applications");

            migrationBuilder.DropColumn(
                name: "document_request_note",
                table: "sales_visa_applications");

            migrationBuilder.DropColumn(
                name: "is_inhouse",
                table: "sales_visa_applications");

            migrationBuilder.DropColumn(
                name: "is_inhouse",
                table: "other_services");

            migrationBuilder.DropColumn(
                name: "is_inhouse",
                table: "holiday_packages");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "holiday_packages");

            migrationBuilder.DropColumn(
                name: "expected_supplier_cost",
                table: "flight_bookings");

            migrationBuilder.DropColumn(
                name: "is_inhouse",
                table: "flight_bookings");

            migrationBuilder.DropColumn(
                name: "profit",
                table: "flight_bookings");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "flight_bookings");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "flight_bookings");

            migrationBuilder.AddColumn<string>(
                name: "vfs_app_id",
                table: "vfs_appointments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vfs_id",
                table: "vfs_appointments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointments_vfs_id",
                table: "vfs_appointments",
                column: "vfs_id",
                unique: true);
        }
    }
}
