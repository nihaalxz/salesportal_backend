using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptExternalReferenceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "receipts",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "receipts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ExternalReference",
                table: "receipts",
                column: "ExternalReference",
                unique: true,
                filter: "[ExternalReference] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_ExternalReference",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "receipts");
        }
    }
}
