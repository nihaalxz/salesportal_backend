using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierToVisaAndVfsGovtFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "supplier_id",
                table: "vfs_appointment_govt_fees",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "vfs_appointment_govt_fees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "supplier_id",
                table: "sales_visa_govt_fees",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "sales_visa_govt_fees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "vfs_appointment_govt_fees");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "vfs_appointment_govt_fees");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "sales_visa_govt_fees");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "sales_visa_govt_fees");
        }
    }
}
