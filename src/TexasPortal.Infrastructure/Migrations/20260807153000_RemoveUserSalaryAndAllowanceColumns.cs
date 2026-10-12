using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserSalaryAndAllowanceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "basic_salary",
                table: "users");

            migrationBuilder.DropColumn(
                name: "housing_allowance",
                table: "users");

            migrationBuilder.DropColumn(
                name: "transport_allowance",
                table: "users");

            migrationBuilder.DropColumn(
                name: "incentive",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "basic_salary",
                table: "users",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "housing_allowance",
                table: "users",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "transport_allowance",
                table: "users",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "incentive",
                table: "users",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
