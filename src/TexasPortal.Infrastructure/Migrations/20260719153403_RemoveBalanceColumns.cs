using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBalanceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "balance",
                table: "heads");

            migrationBuilder.DropColumn(
                name: "balance_amount",
                table: "heads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "balance",
                table: "heads",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "balance_amount",
                table: "heads",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}

