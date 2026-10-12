using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesmanProfitAdjustmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'salesman_profit_records', N'U') IS NULL
                BEGIN
                    CREATE TABLE [salesman_profit_records] (
                        [id] bigint NOT NULL IDENTITY,
                        [salesman_id] bigint NOT NULL,
                        [booking_type] nvarchar(50) NOT NULL,
                        [booking_id] bigint NOT NULL,
                        [document_number] nvarchar(50) NULL,
                        [payable_amount] decimal(15,3) NOT NULL DEFAULT 0,
                        [final_supplier_cost] decimal(15,3) NOT NULL DEFAULT 0,
                        [profit_amount] decimal(15,3) NOT NULL DEFAULT 0,
                        [profit_type] nvarchar(30) NOT NULL DEFAULT 'standard',
                        [supplier_cost_source] nvarchar(30) NOT NULL DEFAULT 'manual',
                        [accounts_approved_total] decimal(15,3) NULL,
                        [rejection_fee] decimal(15,3) NULL,
                        [is_final] bit NOT NULL DEFAULT 0,
                        [booking_created_at] datetime2 NOT NULL,
                        [finalized_at] datetime2 NULL,
                        [last_reconciled_at] datetime2 NULL,
                        [created_at] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [updated_at] datetime2 NULL,
                        CONSTRAINT [PK_salesman_profit_records] PRIMARY KEY ([id]),
                        CONSTRAINT [FK_salesman_profit_records_users_salesman_id] FOREIGN KEY ([salesman_id]) REFERENCES [users] ([id])
                    );

                    CREATE UNIQUE INDEX [IX_salesman_profit_records_BookingType_BookingId] ON [salesman_profit_records] ([booking_type], [booking_id]);
                    CREATE INDEX [IX_salesman_profit_records_salesman_id] ON [salesman_profit_records] ([salesman_id]);
                    CREATE INDEX [IX_salesman_profit_records_salesman_id_booking_created_at] ON [salesman_profit_records] ([salesman_id], [booking_created_at]);
                    CREATE INDEX [IX_salesman_profit_records_is_final] ON [salesman_profit_records] ([is_final]);
                END
            ");

            migrationBuilder.CreateTable(
                name: "salesman_profit_adjustments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    salesman_profit_record_id = table.Column<long>(type: "bigint", nullable: false),
                    salesman_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    adjustment_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    reference_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    reference_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesman_profit_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "FK_salesman_profit_adjustments_salesman_profit_records_salesman_profit_record_id",
                        column: x => x.salesman_profit_record_id,
                        principalTable: "salesman_profit_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_adjustment_type",
                table: "salesman_profit_adjustments",
                column: "adjustment_type");

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_booking_type_booking_id",
                table: "salesman_profit_adjustments",
                columns: new[] { "booking_type", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_created_at",
                table: "salesman_profit_adjustments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_idempotency_key",
                table: "salesman_profit_adjustments",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_reference_type_reference_id",
                table: "salesman_profit_adjustments",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_salesman_id",
                table: "salesman_profit_adjustments",
                column: "salesman_id");

            migrationBuilder.CreateIndex(
                name: "IX_salesman_profit_adjustments_salesman_profit_record_id",
                table: "salesman_profit_adjustments",
                column: "salesman_profit_record_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "salesman_profit_adjustments");
        }
    }
}
