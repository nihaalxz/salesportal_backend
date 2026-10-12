using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointments_created_at",
                table: "vfs_appointments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_transportations_created_at",
                table: "transportations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_sales_visa_applications_created_at",
                table: "sales_visa_applications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_other_services_created_at",
                table: "other_services",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_bookings_created_at",
                table: "hotel_bookings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_holiday_packages_created_at",
                table: "holiday_packages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_flight_bookings_created_at",
                table: "flight_bookings",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vfs_appointments_created_at",
                table: "vfs_appointments");

            migrationBuilder.DropIndex(
                name: "IX_transportations_created_at",
                table: "transportations");

            migrationBuilder.DropIndex(
                name: "IX_sales_visa_applications_created_at",
                table: "sales_visa_applications");

            migrationBuilder.DropIndex(
                name: "IX_other_services_created_at",
                table: "other_services");

            migrationBuilder.DropIndex(
                name: "IX_hotel_bookings_created_at",
                table: "hotel_bookings");

            migrationBuilder.DropIndex(
                name: "IX_holiday_packages_created_at",
                table: "holiday_packages");

            migrationBuilder.DropIndex(
                name: "IX_flight_bookings_created_at",
                table: "flight_bookings");
        }
    }
}
