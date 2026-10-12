using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHolidayAppIdBookingIdSupplierName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Safely drop index on booking_id if it exists
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_holiday_packages_booking_id' AND object_id = OBJECT_ID('holiday_packages'))
                BEGIN
                    DROP INDEX IX_holiday_packages_booking_id ON holiday_packages;
                END;

                -- Drop columns if they exist
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'booking_id')
                BEGIN
                    ALTER TABLE holiday_packages DROP COLUMN booking_id;
                END;

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'holiday_app_id')
                BEGIN
                    ALTER TABLE holiday_packages DROP COLUMN holiday_app_id;
                END;

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'supplier_name')
                BEGIN
                    ALTER TABLE holiday_packages DROP COLUMN supplier_name;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
