using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelDateAirlineToFlightBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'travel_date')
                BEGIN
                    ALTER TABLE flight_bookings ADD travel_date datetime2 NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'airline_company')
                BEGIN
                    ALTER TABLE flight_bookings ADD airline_company nvarchar(255) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'ticket_pnr')
                BEGIN
                    ALTER TABLE flight_bookings ADD ticket_pnr nvarchar(255) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_passengers') AND name = 'ticket_pnr')
                BEGIN
                    ALTER TABLE flight_passengers ADD ticket_pnr nvarchar(255) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'travel_date')
                BEGIN
                    ALTER TABLE flight_bookings DROP COLUMN travel_date;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'airline_company')
                BEGIN
                    ALTER TABLE flight_bookings DROP COLUMN airline_company;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'ticket_pnr')
                BEGIN
                    ALTER TABLE flight_bookings DROP COLUMN ticket_pnr;
                END
            ");
        }
    }
}
