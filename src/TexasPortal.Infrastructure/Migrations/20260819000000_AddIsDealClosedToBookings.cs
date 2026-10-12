using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDealClosedToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE flight_bookings ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('hotel_bookings') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE hotel_bookings ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE sales_visa_applications ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE vfs_appointments ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE holiday_packages ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('transportations') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE transportations ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('other_services') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE other_services ADD is_deal_closed bit NOT NULL DEFAULT 0;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE flight_bookings DROP COLUMN is_deal_closed;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('hotel_bookings') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE hotel_bookings DROP COLUMN is_deal_closed;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE sales_visa_applications DROP COLUMN is_deal_closed;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE vfs_appointments DROP COLUMN is_deal_closed;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE holiday_packages DROP COLUMN is_deal_closed;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('transportations') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE transportations DROP COLUMN is_deal_closed;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('other_services') AND name = 'is_deal_closed')
                BEGIN
                    ALTER TABLE other_services DROP COLUMN is_deal_closed;
                END
            ");
        }
    }
}
