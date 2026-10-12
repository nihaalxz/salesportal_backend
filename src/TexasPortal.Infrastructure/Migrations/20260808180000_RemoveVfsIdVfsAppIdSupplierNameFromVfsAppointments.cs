using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVfsIdVfsAppIdSupplierNameFromVfsAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Safely drop index on vfs_id if it exists
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_vfs_appointments_vfs_id' AND object_id = OBJECT_ID('vfs_appointments'))
                BEGIN
                    DROP INDEX IX_vfs_appointments_vfs_id ON vfs_appointments;
                END;

                -- Drop columns if they exist
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'vfs_id')
                BEGIN
                    ALTER TABLE vfs_appointments DROP COLUMN vfs_id;
                END;

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'vfs_app_id')
                BEGIN
                    ALTER TABLE vfs_appointments DROP COLUMN vfs_app_id;
                END;

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('vfs_appointments') AND name = 'supplier_name')
                BEGIN
                    ALTER TABLE vfs_appointments DROP COLUMN supplier_name;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
