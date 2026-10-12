using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TexasPortal.Infrastructure.Persistence;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations;

public partial class AddQuotationsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'quotations')
            BEGIN
                CREATE TABLE [dbo].[quotations] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL,
                    [DocumentNumber] NVARCHAR(100) NOT NULL,
                    [UserId] BIGINT NOT NULL,
                    [HeadId] BIGINT NULL,
                    [ClientType] NVARCHAR(50) NOT NULL DEFAULT 'customer',
                    [CustomerName] NVARCHAR(255) NOT NULL,
                    [CustomerContact] NVARCHAR(100) NULL,
                    [CustomerEmail] NVARCHAR(255) NULL,
                    [TotalAmount] DECIMAL(15,3) NOT NULL DEFAULT 0,
                    [Remarks] NVARCHAR(MAX) NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] DATETIME2 NULL,
                    CONSTRAINT [PK_quotations] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [FK_quotations_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([id]),
                    CONSTRAINT [FK_quotations_heads_HeadId] FOREIGN KEY ([HeadId]) REFERENCES [dbo].[heads] ([id])
                );

                CREATE INDEX [IX_quotations_UserId] ON [dbo].[quotations] ([UserId]);
                CREATE INDEX [IX_quotations_HeadId] ON [dbo].[quotations] ([HeadId]);
                CREATE INDEX [IX_quotations_DocumentNumber] ON [dbo].[quotations] ([DocumentNumber]);
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'quotation_items')
            BEGIN
                CREATE TABLE [dbo].[quotation_items] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL,
                    [QuotationId] BIGINT NOT NULL,
                    [ServiceType] NVARCHAR(100) NOT NULL,
                    [Description] NVARCHAR(MAX) NOT NULL,
                    [Quantity] INT NOT NULL DEFAULT 1,
                    [UnitPrice] DECIMAL(15,3) NOT NULL DEFAULT 0,
                    [TotalAmount] DECIMAL(15,3) NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_quotation_items] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [FK_quotation_items_quotations_QuotationId] FOREIGN KEY ([QuotationId]) REFERENCES [dbo].[quotations] ([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_quotation_items_QuotationId] ON [dbo].[quotation_items] ([QuotationId]);
            END

            IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'QuotationSequence')
            BEGIN
                CREATE SEQUENCE [dbo].[QuotationSequence] START WITH 1 INCREMENT BY 1;
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'quotation_items')
            BEGIN
                DROP TABLE [dbo].[quotation_items];
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'quotations')
            BEGIN
                DROP TABLE [dbo].[quotations];
            END
        ");
    }
}
