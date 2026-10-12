using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePassportFrontBackWithPassportCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Run your passport layout changes safely
            migrationBuilder.DropColumn(
                name: "passport_back",
                table: "sales_visa_applicants");

            migrationBuilder.RenameColumn(
                name: "passport_front",
                table: "sales_visa_applicants",
                newName: "passport_copy");

            // 2. Wrap OutboxMessages table creation inside a database existence check
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.OutboxMessages', 'U') IS NULL
                BEGIN
                    CREATE TABLE [OutboxMessages] (
                        [Id] uniqueidentifier NOT NULL,
                        [EventName] nvarchar(max) NOT NULL,
                        [Status] nvarchar(max) NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [ProcessedUtc] datetime2 NULL,
                        [CorrelationId] nvarchar(max) NOT NULL,
                        [Payload] nvarchar(max) NOT NULL,
                        [AttemptCount] int NOT NULL,
                        [LastError] nvarchar(max) NULL,
                        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.RenameColumn(
                name: "passport_copy",
                table: "sales_visa_applicants",
                newName: "passport_front");

            migrationBuilder.AddColumn<string>(
                name: "passport_back",
                table: "sales_visa_applicants",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}

