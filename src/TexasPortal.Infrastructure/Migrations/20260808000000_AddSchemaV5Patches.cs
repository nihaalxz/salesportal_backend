using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaV5Patches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All DDL is guarded with IF NOT EXISTS / IF NOT EXISTS checks so this
            // migration is safe to apply against a database that already received
            // the equivalent schema via the old raw-SQL startup patches.

            migrationBuilder.Sql(@"
                -- receipts: ReceiptType discriminator column
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('receipts') AND name = 'ReceiptType')
                BEGIN
                    ALTER TABLE receipts ADD ReceiptType INT NOT NULL DEFAULT 0;
                    UPDATE receipts SET ReceiptType = 2 WHERE Service = 'credit_settlement';
                    UPDATE receipts SET ReceiptType = 1 WHERE Service IN ('deposit', 'wallet_deposit');
                END;

                -- receipts: RowVersion for optimistic concurrency
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('receipts') AND name = 'RowVersion')
                    ALTER TABLE receipts ADD RowVersion ROWVERSION NULL;

                -- sales_visa_applications: document request columns
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'document_request_note')
                    ALTER TABLE sales_visa_applications ADD document_request_note NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'document_request_date')
                    ALTER TABLE sales_visa_applications ADD document_request_date DATETIME2 NULL;

                -- holiday_package_govt_fees: supplier and description columns
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'supplier_id')
                    ALTER TABLE holiday_package_govt_fees ADD supplier_id BIGINT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'supplier_name')
                    ALTER TABLE holiday_package_govt_fees ADD supplier_name NVARCHAR(255) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'description')
                    ALTER TABLE holiday_package_govt_fees ADD description NVARCHAR(MAX) NULL;

                -- BalanceTransactions table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BalanceTransactions')
                BEGIN
                    CREATE TABLE BalanceTransactions (
                        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        HeadId      BIGINT NOT NULL,
                        TransactionType INT NOT NULL,
                        Amount      DECIMAL(15,3) NOT NULL,
                        Currency    NVARCHAR(10) NOT NULL DEFAULT 'BHD',
                        CorrelationId NVARCHAR(450) NULL,
                        ReferenceId NVARCHAR(255) NULL,
                        TransactionDate DATETIME2 NOT NULL,
                        CreatedBy   BIGINT NULL,
                        Remarks     NVARCHAR(MAX) NULL
                    );
                    CREATE INDEX IX_BalanceTransactions_HeadId ON BalanceTransactions(HeadId);
                    CREATE INDEX IX_BalanceTransactions_CorrelationId ON BalanceTransactions(CorrelationId);
                END;

                -- CustomerAccountTransactions table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerAccountTransactions')
                BEGIN
                    CREATE TABLE CustomerAccountTransactions (
                        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        HeadId      BIGINT NOT NULL,
                        TransactionType INT NOT NULL,
                        Amount      DECIMAL(15,3) NOT NULL,
                        Currency    NVARCHAR(10) NOT NULL DEFAULT 'BHD',
                        CorrelationId NVARCHAR(450) NULL,
                        ReferenceId NVARCHAR(255) NULL,
                        TransactionDate DATETIME2 NOT NULL,
                        CreatedBy   BIGINT NULL,
                        Remarks     NVARCHAR(MAX) NULL
                    );
                    CREATE INDEX IX_CustomerAccountTransactions_HeadId ON CustomerAccountTransactions(HeadId);
                    CREATE INDEX IX_CustomerAccountTransactions_CorrelationId ON CustomerAccountTransactions(CorrelationId);
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerAccountTransactions')
                    DROP TABLE CustomerAccountTransactions;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'BalanceTransactions')
                    DROP TABLE BalanceTransactions;
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'document_request_date')
                    ALTER TABLE sales_visa_applications DROP COLUMN document_request_date;
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('sales_visa_applications') AND name = 'document_request_note')
                    ALTER TABLE sales_visa_applications DROP COLUMN document_request_note;
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('receipts') AND name = 'RowVersion')
                    ALTER TABLE receipts DROP COLUMN RowVersion;
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('receipts') AND name = 'ReceiptType')
                    ALTER TABLE receipts DROP COLUMN ReceiptType;
            ");
        }
    }
}
