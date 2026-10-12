using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TexasPortal.Infrastructure.Persistence;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations;

public partial class AddOperationClosedAndBookingApplicants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            -- 1. Add operation_closed to all 7 booking tables
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('flight_bookings') AND name = 'operation_closed')
                ALTER TABLE flight_bookings ADD operation_closed bit NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('hotel_bookings') AND name = 'operation_closed')
                ALTER TABLE hotel_bookings ADD operation_closed bit NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'operation_closed')
                ALTER TABLE sales_visa_applications ADD operation_closed bit NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'operation_closed')
                ALTER TABLE vfs_appointments ADD operation_closed bit NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('holiday_packages') AND name = 'operation_closed')
                ALTER TABLE holiday_packages ADD operation_closed bit NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('transportations') AND name = 'operation_closed')
                ALTER TABLE transportations ADD operation_closed bit NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('other_services') AND name = 'operation_closed')
                ALTER TABLE other_services ADD operation_closed bit NOT NULL DEFAULT 0;

            -- 2. Add hotel_name to hotel_bookings
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('hotel_bookings') AND name = 'hotel_name')
                ALTER TABLE hotel_bookings ADD hotel_name nvarchar(255) NULL;

            -- 3. Create child tables
            IF OBJECT_ID(N'hotel_guests', N'U') IS NULL
            BEGIN
                CREATE TABLE [hotel_guests] (
                    [id] bigint NOT NULL IDENTITY,
                    [hotel_booking_id] bigint NOT NULL,
                    [prefix] nvarchar(255) NULL,
                    [guest_name] nvarchar(255) NOT NULL,
                    [payable_amount] decimal(15,3) NOT NULL,
                    [supplier_cost] decimal(15,3) NOT NULL,
                    [expected_supplier_cost] decimal(15,3) NOT NULL,
                    [profit] decimal(15,3) NOT NULL,
                    [is_inhouse] bit NOT NULL DEFAULT 0,
                    [supplier_id] bigint NULL,
                    [supplier_name] nvarchar(255) NULL,
                    [created_at] datetime2 NULL,
                    [updated_at] datetime2 NULL,
                    CONSTRAINT [PK_hotel_guests] PRIMARY KEY ([id]),
                    CONSTRAINT [FK_hotel_guests_hotel_bookings_hotel_booking_id] FOREIGN KEY ([hotel_booking_id]) REFERENCES [hotel_bookings] ([id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_hotel_guests_hotel_booking_id] ON [hotel_guests] ([hotel_booking_id]);
            END;

            IF OBJECT_ID(N'transport_passengers', N'U') IS NULL
            BEGIN
                CREATE TABLE [transport_passengers] (
                    [id] bigint NOT NULL IDENTITY,
                    [transportation_id] bigint NOT NULL,
                    [prefix] nvarchar(255) NULL,
                    [name] nvarchar(255) NOT NULL,
                    [payable_amount] decimal(15,3) NOT NULL,
                    [supplier_cost] decimal(15,3) NOT NULL,
                    [expected_supplier_cost] decimal(15,3) NOT NULL,
                    [profit] decimal(15,3) NOT NULL,
                    [is_inhouse] bit NOT NULL DEFAULT 0,
                    [supplier_id] bigint NULL,
                    [supplier_name] nvarchar(255) NULL,
                    [created_at] datetime2 NULL,
                    [updated_at] datetime2 NULL,
                    CONSTRAINT [PK_transport_passengers] PRIMARY KEY ([id]),
                    CONSTRAINT [FK_transport_passengers_transportations_transportation_id] FOREIGN KEY ([transportation_id]) REFERENCES [transportations] ([id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_transport_passengers_transportation_id] ON [transport_passengers] ([transportation_id]);
            END;

            IF OBJECT_ID(N'vfs_applicants', N'U') IS NULL
            BEGIN
                CREATE TABLE [vfs_applicants] (
                    [id] bigint NOT NULL IDENTITY,
                    [vfs_appointment_id] bigint NOT NULL,
                    [prefix] nvarchar(255) NULL,
                    [name] nvarchar(255) NOT NULL,
                    [passport_copy] nvarchar(255) NULL,
                    [payable_amount] decimal(15,3) NOT NULL,
                    [supplier_cost] decimal(15,3) NOT NULL,
                    [expected_supplier_cost] decimal(15,3) NOT NULL,
                    [profit] decimal(15,3) NOT NULL,
                    [is_inhouse] bit NOT NULL DEFAULT 0,
                    [supplier_id] bigint NULL,
                    [supplier_name] nvarchar(255) NULL,
                    [created_at] datetime2 NULL,
                    [updated_at] datetime2 NULL,
                    CONSTRAINT [PK_vfs_applicants] PRIMARY KEY ([id]),
                    CONSTRAINT [FK_vfs_applicants_vfs_appointments_vfs_appointment_id] FOREIGN KEY ([vfs_appointment_id]) REFERENCES [vfs_appointments] ([id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_vfs_applicants_vfs_appointment_id] ON [vfs_applicants] ([vfs_appointment_id]);
            END;

            IF OBJECT_ID(N'holiday_passengers', N'U') IS NULL
            BEGIN
                CREATE TABLE [holiday_passengers] (
                    [id] bigint NOT NULL IDENTITY,
                    [holiday_package_id] bigint NOT NULL,
                    [prefix] nvarchar(255) NULL,
                    [passenger_name] nvarchar(255) NOT NULL,
                    [payable_amount] decimal(15,3) NOT NULL,
                    [supplier_cost] decimal(15,3) NOT NULL,
                    [expected_supplier_cost] decimal(15,3) NOT NULL,
                    [profit] decimal(15,3) NOT NULL,
                    [is_inhouse] bit NOT NULL DEFAULT 0,
                    [supplier_id] bigint NULL,
                    [supplier_name] nvarchar(255) NULL,
                    [created_at] datetime2 NULL,
                    [updated_at] datetime2 NULL,
                    CONSTRAINT [PK_holiday_passengers] PRIMARY KEY ([id]),
                    CONSTRAINT [FK_holiday_passengers_holiday_packages_holiday_package_id] FOREIGN KEY ([holiday_package_id]) REFERENCES [holiday_packages] ([id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_holiday_passengers_holiday_package_id] ON [holiday_passengers] ([holiday_package_id]);
            END;

            IF OBJECT_ID(N'other_service_applicants', N'U') IS NULL
            BEGIN
                CREATE TABLE [other_service_applicants] (
                    [id] bigint NOT NULL IDENTITY,
                    [other_service_id] bigint NOT NULL,
                    [prefix] nvarchar(255) NULL,
                    [name] nvarchar(255) NOT NULL,
                    [payable_amount] decimal(15,3) NOT NULL,
                    [supplier_cost] decimal(15,3) NOT NULL,
                    [expected_supplier_cost] decimal(15,3) NOT NULL,
                    [profit] decimal(15,3) NOT NULL,
                    [is_inhouse] bit NOT NULL DEFAULT 0,
                    [supplier_id] bigint NULL,
                    [supplier_name] nvarchar(255) NULL,
                    [created_at] datetime2 NULL,
                    [updated_at] datetime2 NULL,
                    CONSTRAINT [PK_other_service_applicants] PRIMARY KEY ([id]),
                    CONSTRAINT [FK_other_service_applicants_other_services_other_service_id] FOREIGN KEY ([other_service_id]) REFERENCES [other_services] ([id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_other_service_applicants_other_service_id] ON [other_service_applicants] ([other_service_id]);
            END;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DROP TABLE IF EXISTS [other_service_applicants];
            DROP TABLE IF EXISTS [holiday_passengers];
            DROP TABLE IF EXISTS [vfs_applicants];
            DROP TABLE IF EXISTS [transport_passengers];
            DROP TABLE IF EXISTS [hotel_guests];

            ALTER TABLE hotel_bookings DROP COLUMN IF EXISTS hotel_name;
            ALTER TABLE flight_bookings DROP COLUMN IF EXISTS operation_closed;
            ALTER TABLE hotel_bookings DROP COLUMN IF EXISTS operation_closed;
            ALTER TABLE sales_visa_applications DROP COLUMN IF EXISTS operation_closed;
            ALTER TABLE vfs_appointments DROP COLUMN IF EXISTS operation_closed;
            ALTER TABLE holiday_packages DROP COLUMN IF EXISTS operation_closed;
            ALTER TABLE transportations DROP COLUMN IF EXISTS operation_closed;
            ALTER TABLE other_services DROP COLUMN IF EXISTS operation_closed;
        ");
    }
}
