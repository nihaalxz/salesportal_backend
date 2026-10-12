using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditReceivablesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'BadDebtWriteOffs') DROP TABLE BadDebtWriteOffs;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CreditNotes') DROP TABLE CreditNotes;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiptAllocations') DROP TABLE ReceiptAllocations;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceivablePayments') DROP TABLE ReceivablePayments;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceivableTransactions') DROP TABLE ReceivableTransactions;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CollectionBatches') DROP TABLE CollectionBatches;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CreditPolicies') DROP TABLE CreditPolicies;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CreditReceivables') DROP TABLE CreditReceivables;
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BalanceTransactions')
                BEGIN
                    CREATE TABLE BalanceTransactions (
                        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                        HeadId BIGINT NOT NULL,
                        TransactionType INT NOT NULL,
                        Amount DECIMAL(15,3) NOT NULL,
                        Currency NVARCHAR(10) NOT NULL DEFAULT 'BHD',
                        CorrelationId NVARCHAR(450) NULL,
                        ReferenceId NVARCHAR(255) NULL,
                        TransactionDate DATETIME2 NOT NULL,
                        CreatedBy BIGINT NULL,
                        Remarks NVARCHAR(MAX) NULL
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BalanceTransactions') AND name = 'CorrelationId')
                    ALTER TABLE BalanceTransactions ADD CorrelationId NVARCHAR(450) NULL;
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerAccountTransactions')
                BEGIN
                    CREATE TABLE CustomerAccountTransactions (
                        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                        HeadId BIGINT NOT NULL,
                        TransactionType INT NOT NULL,
                        Amount DECIMAL(15,3) NOT NULL,
                        Currency NVARCHAR(10) NOT NULL DEFAULT 'BHD',
                        CorrelationId NVARCHAR(450) NULL,
                        ReferenceId NVARCHAR(255) NULL,
                        TransactionDate DATETIME2 NOT NULL,
                        CreatedBy BIGINT NULL,
                        Remarks NVARCHAR(MAX) NULL
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('receipts') AND name = 'ReceiptType')
                    ALTER TABLE receipts ADD ReceiptType INT NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('receipts') AND name = 'RowVersion')
                    ALTER TABLE receipts ADD RowVersion ROWVERSION NULL;
            ");

            migrationBuilder.CreateTable(
                name: "CollectionBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalesmanId = table.Column<long>(type: "bigint", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    ReceiptCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeadId = table.Column<long>(type: "bigint", nullable: false),
                    HeadType = table.Column<int>(type: "int", nullable: false),
                    MaxCreditLimit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    DefaultCreditTerm = table.Column<int>(type: "int", nullable: false),
                    AllowPartialPayments = table.Column<bool>(type: "bit", nullable: false),
                    RequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    AutoBlockOnOverdue = table.Column<bool>(type: "bit", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditReceivables",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingModule = table.Column<int>(type: "int", nullable: false),
                    HeadId = table.Column<long>(type: "bigint", nullable: true),
                    HeadType = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    AgentId = table.Column<long>(type: "bigint", nullable: true),
                    SalesmanId = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    CachedOutstandingAmount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreditTerm = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinancialSourceType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditReceivables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadDebtWriteOffs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WriteOffNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadDebtWriteOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadDebtWriteOffs_CreditReceivables_ReceivableId",
                        column: x => x.ReceivableId,
                        principalTable: "CreditReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNotes_CreditReceivables_ReceivableId",
                        column: x => x.ReceivableId,
                        principalTable: "CreditReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<long>(type: "bigint", nullable: false),
                    ReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_CreditReceivables_ReceivableId",
                        column: x => x.ReceivableId,
                        principalTable: "CreditReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceivablePayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptId = table.Column<long>(type: "bigint", nullable: false),
                    CollectionBatchId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedBy = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivablePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivablePayments_CollectionBatches_CollectionBatchId",
                        column: x => x.CollectionBatchId,
                        principalTable: "CollectionBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceivablePayments_CreditReceivables_ReceivableId",
                        column: x => x.ReceivableId,
                        principalTable: "CreditReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivablePayments_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceivableTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivableTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivableTransactions_CreditReceivables_ReceivableId",
                        column: x => x.ReceivableId,
                        principalTable: "CreditReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadDebtWriteOffs_ReceivableId",
                table: "BadDebtWriteOffs",
                column: "ReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_ReceivableId",
                table: "CreditNotes",
                column: "ReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_ReceiptId",
                table: "ReceiptAllocations",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_ReceivableId",
                table: "ReceiptAllocations",
                column: "ReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePayments_CollectionBatchId",
                table: "ReceivablePayments",
                column: "CollectionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePayments_ReceiptId",
                table: "ReceivablePayments",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePayments_ReceivableId",
                table: "ReceivablePayments",
                column: "ReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransaction_CorrelationId",
                table: "ReceivableTransactions",
                column: "CorrelationId",
                unique: true,
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_ReceivableId",
                table: "ReceivableTransactions",
                column: "ReceivableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadDebtWriteOffs");

            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropTable(
                name: "CreditPolicies");

            migrationBuilder.DropTable(
                name: "ReceiptAllocations");

            migrationBuilder.DropTable(
                name: "ReceivablePayments");

            migrationBuilder.DropTable(
                name: "ReceivableTransactions");

            migrationBuilder.DropTable(
                name: "CollectionBatches");

            migrationBuilder.DropTable(
                name: "CreditReceivables");

            migrationBuilder.DropColumn(
                name: "ReceiptType",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "receipts");
        }
    }
}

