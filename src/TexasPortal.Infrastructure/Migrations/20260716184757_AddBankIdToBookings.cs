using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBankIdToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "bank_id",
                table: "vfs_appointments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "vfs_appointments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bank_id",
                table: "transportations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "transportations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bank_id",
                table: "other_services",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "other_services",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bank_id",
                table: "holiday_packages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "holiday_packages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bank_id",
                table: "flight_bookings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "flight_bookings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "vfs_appointments");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "vfs_appointments");

            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "transportations");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "transportations");

            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "other_services");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "other_services");

            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "holiday_packages");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "holiday_packages");

            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "flight_bookings");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "flight_bookings");
        }
    }
}

