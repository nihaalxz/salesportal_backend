using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountsPaymentIdToGovtFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "accounts_payment_id",
                table: "vfs_appointment_govt_fees",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "accounts_payment_id",
                table: "sales_visa_govt_fees",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "accounts_payment_id",
                table: "holiday_package_govt_fees",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accounts_payment_id",
                table: "vfs_appointment_govt_fees");

            migrationBuilder.DropColumn(
                name: "accounts_payment_id",
                table: "sales_visa_govt_fees");

            migrationBuilder.DropColumn(
                name: "accounts_payment_id",
                table: "holiday_package_govt_fees");
        }
    }
}
