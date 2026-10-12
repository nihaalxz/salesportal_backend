using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProofToTransportAndOtherServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_proof",
                table: "transportations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_proof",
                table: "other_services",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_proof",
                table: "transportations");

            migrationBuilder.DropColumn(
                name: "payment_proof",
                table: "other_services");
        }
    }
}

