using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TexasPortal.Infrastructure.Persistence;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations;

public partial class AddStatusToQuotations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'quotations')
            BEGIN
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE Name = N'Status' AND Object_ID = Object_ID(N'dbo.quotations')
                )
                BEGIN
                    ALTER TABLE [dbo].[quotations] 
                    ADD [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_quotations_Status] DEFAULT ('pending');
                END
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF EXISTS (
                SELECT * FROM sys.columns 
                WHERE Name = N'Status' AND Object_ID = Object_ID(N'dbo.quotations')
            )
            BEGIN
                ALTER TABLE [dbo].[quotations] DROP CONSTRAINT IF EXISTS [DF_quotations_Status];
                ALTER TABLE [dbo].[quotations] DROP COLUMN [Status];
            END
        ");
    }
}
