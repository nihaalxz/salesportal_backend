using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeRefundColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "approved_refund_amount",
                table: "disputes",
                type: "decimal(15,3)",
                precision: 15,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "head_id",
                table: "disputes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_refund",
                table: "disputes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "refund_processed",
                table: "disputes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "resolution",
                table: "disputes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resolved_at",
                table: "disputes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolved_by",
                table: "disputes",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_disputes_head_id",
                table: "disputes",
                column: "head_id");

            migrationBuilder.AddForeignKey(
                name: "FK_disputes_heads_head_id",
                table: "disputes",
                column: "head_id",
                principalTable: "heads",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_disputes_heads_head_id",
                table: "disputes");

            migrationBuilder.DropIndex(
                name: "IX_disputes_head_id",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "approved_refund_amount",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "head_id",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "is_refund",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "refund_processed",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "resolution",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "resolved_at",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "resolved_by",
                table: "disputes");
        }
    }
}

