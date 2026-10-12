using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    notifiable_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    notifiable_id = table.Column<long>(type: "bigint", nullable: false),
                    data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    user_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    remember_token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_login_ip = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    profit_target = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    monthly_target = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit_generated = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    incentive = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    basic_salary = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    housing_allowance = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    transport_allowance = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_in_hands",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_in_hands", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_in_hands_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deposits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    destination_id = table.Column<long>(type: "bigint", nullable: false),
                    narration = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    api_transaction_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposits", x => x.id);
                    table.ForeignKey(
                        name: "FK_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    disputable_id = table.Column<long>(type: "bigint", nullable: false),
                    disputable_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    resolution_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refund_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: true),
                    refund_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    refund_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.id);
                    table.ForeignKey(
                        name: "FK_disputes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "heads",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    mobile_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    nationality = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    balance = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    balance_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    wallet = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_heads", x => x.id);
                    table.ForeignKey(
                        name: "FK_heads_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    country_code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    mobile_no = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    services = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lead_source = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    reminder_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reminder_active = table.Column<bool>(type: "bit", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.id);
                    table.ForeignKey(
                        name: "FK_leads_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_monthly_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    month = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    profit_generated = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    incentive = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit_target = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    monthly_target = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    target_achieved = table.Column<bool>(type: "bit", nullable: false),
                    basic_salary = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    housing_allowance = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    transport_allowance = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_monthly_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_monthly_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flight_bookings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    care_of = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    supplier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    from_location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    to_location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_flight_bookings_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flight_bookings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "head_ledgers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    head_id = table.Column<long>(type: "bigint", nullable: false),
                    service_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    debit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    credit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_head_ledgers", x => x.id);
                    table.ForeignKey(
                        name: "FK_head_ledgers_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "holiday_packages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    booking_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    holiday_app_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    passenger_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    supplier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    country = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    days = table.Column<int>(type: "int", nullable: false),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_mode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_packages", x => x.id);
                    table.ForeignKey(
                        name: "FK_holiday_packages_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_holiday_packages_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hotel_bookings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    is_inhouse = table.Column<bool>(type: "bit", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    guest_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    care_of = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    supplier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    country = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    days = table.Column<int>(type: "int", nullable: false),
                    check_in_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    order_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    bank_id = table.Column<long>(type: "bigint", nullable: true),
                    bank_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_hotel_bookings_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_bookings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "other_services",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    care_of = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    supplier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    service_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_other_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_other_services_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_other_services_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    receipt_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    booking_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    narration = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    service = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    payment_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    bank_id = table.Column<long>(type: "bigint", nullable: true),
                    bank_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_receipts_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_receipts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_visa_applications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    country_id = table.Column<int>(type: "int", nullable: false),
                    visa_pack_id = table.Column<int>(type: "int", nullable: false),
                    visa_pack_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    visa_document = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    approval_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    rejection_document = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    rejection_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refund_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    rejection_fee = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_visa_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_visa_applications_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_visa_applications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transportations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    care_of = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    supplier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    pickup = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    drop = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    drop_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    drop_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vehicle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    is_edited = table.Column<bool>(type: "bit", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transportations", x => x.id);
                    table.ForeignKey(
                        name: "FK_transportations_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transportations_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vfs_appointments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    head_id = table.Column<long>(type: "bigint", nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    vfs_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vfs_app_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    supplier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    country = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    visa_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_mode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    appointment_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    appointment_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    vfs_receipt = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    passport_copy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vfs_appointments", x => x.id);
                    table.ForeignKey(
                        name: "FK_vfs_appointments_heads_head_id",
                        column: x => x.head_id,
                        principalTable: "heads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vfs_appointments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "flight_passengers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    flight_booking_id = table.Column<long>(type: "bigint", nullable: false),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    passenger_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    is_inhouse = table.Column<bool>(type: "bit", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_passengers", x => x.id);
                    table.ForeignKey(
                        name: "FK_flight_passengers_flight_bookings_flight_booking_id",
                        column: x => x.flight_booking_id,
                        principalTable: "flight_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "holiday_package_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    holiday_package_id = table.Column<long>(type: "bigint", nullable: false),
                    document_path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_package_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_holiday_package_documents_holiday_packages_holiday_package_id",
                        column: x => x.holiday_package_id,
                        principalTable: "holiday_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "holiday_package_govt_fees",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    holiday_package_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_package_govt_fees", x => x.id);
                    table.ForeignKey(
                        name: "FK_holiday_package_govt_fees_holiday_packages_holiday_package_id",
                        column: x => x.holiday_package_id,
                        principalTable: "holiday_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_visa_applicants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sales_visa_application_id = table.Column<long>(type: "bigint", nullable: false),
                    visa_app_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    f_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    l_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    dob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    nationality = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    care_of = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    contact_no = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    passport_no = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    iqama_hotel_no = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payable_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    unpaid_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: true),
                    bank_doc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    passport_front = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    passport_back = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    processed_by = table.Column<long>(type: "bigint", nullable: true),
                    payment_confirmed_by = table.Column<long>(type: "bigint", nullable: true),
                    supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    expected_supplier_cost = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    profit = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    is_inhouse = table.Column<bool>(type: "bit", nullable: false),
                    bank_id = table.Column<long>(type: "bigint", nullable: true),
                    bank_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    govt_fee_amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: true),
                    govt_fee_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    govt_fee_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    govt_fee_payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_visa_applicants", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_visa_applicants_sales_visa_applications_sales_visa_application_id",
                        column: x => x.sales_visa_application_id,
                        principalTable: "sales_visa_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vfs_appointment_govt_fees",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vfs_appointment_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    payment_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vfs_appointment_govt_fees", x => x.id);
                    table.ForeignKey(
                        name: "FK_vfs_appointment_govt_fees_vfs_appointments_vfs_appointment_id",
                        column: x => x.vfs_appointment_id,
                        principalTable: "vfs_appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_visa_govt_fees",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sales_visa_applicant_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    payment_proof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_visa_govt_fees", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_visa_govt_fees_sales_visa_applicants_sales_visa_applicant_id",
                        column: x => x.sales_visa_applicant_id,
                        principalTable: "sales_visa_applicants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_in_hands_user_id",
                table: "cash_in_hands",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposits_user_id",
                table: "deposits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_user_id",
                table: "disputes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_flight_bookings_head_id",
                table: "flight_bookings",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_flight_bookings_user_id",
                table: "flight_bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_flight_passengers_flight_booking_id",
                table: "flight_passengers",
                column: "flight_booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_head_ledgers_head_id",
                table: "head_ledgers",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "unique_service_ledger_entry",
                table: "head_ledgers",
                columns: new[] { "service_type", "service_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_heads_created_by",
                table: "heads",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_heads_email",
                table: "heads",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_heads_user_id",
                table: "heads",
                column: "user_id",
                unique: true,
                filter: "[user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_holiday_package_documents_holiday_package_id",
                table: "holiday_package_documents",
                column: "holiday_package_id");

            migrationBuilder.CreateIndex(
                name: "IX_holiday_package_govt_fees_holiday_package_id",
                table: "holiday_package_govt_fees",
                column: "holiday_package_id");

            migrationBuilder.CreateIndex(
                name: "IX_holiday_packages_booking_id",
                table: "holiday_packages",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holiday_packages_created_by",
                table: "holiday_packages",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_holiday_packages_head_id",
                table: "holiday_packages",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_bookings_head_id",
                table: "hotel_bookings",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_bookings_user_id",
                table: "hotel_bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_created_by",
                table: "leads",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_other_services_created_by",
                table: "other_services",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_other_services_head_id",
                table: "other_services",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_created_by",
                table: "receipts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_head_id",
                table: "receipts",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_receipt_id",
                table: "receipts",
                column: "receipt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_visa_applicants_sales_visa_application_id",
                table: "sales_visa_applicants",
                column: "sales_visa_application_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_visa_applications_head_id",
                table: "sales_visa_applications",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_visa_applications_user_id",
                table: "sales_visa_applications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_visa_govt_fees_sales_visa_applicant_id",
                table: "sales_visa_govt_fees",
                column: "sales_visa_applicant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_monthly_records_user_id",
                table: "staff_monthly_records",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transportations_created_by",
                table: "transportations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_transportations_head_id",
                table: "transportations",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointment_govt_fees_vfs_appointment_id",
                table: "vfs_appointment_govt_fees",
                column: "vfs_appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointments_created_by",
                table: "vfs_appointments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointments_head_id",
                table: "vfs_appointments",
                column: "head_id");

            migrationBuilder.CreateIndex(
                name: "IX_vfs_appointments_vfs_id",
                table: "vfs_appointments",
                column: "vfs_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_in_hands");

            migrationBuilder.DropTable(
                name: "deposits");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "flight_passengers");

            migrationBuilder.DropTable(
                name: "head_ledgers");

            migrationBuilder.DropTable(
                name: "holiday_package_documents");

            migrationBuilder.DropTable(
                name: "holiday_package_govt_fees");

            migrationBuilder.DropTable(
                name: "hotel_bookings");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "other_services");

            migrationBuilder.DropTable(
                name: "receipts");

            migrationBuilder.DropTable(
                name: "sales_visa_govt_fees");

            migrationBuilder.DropTable(
                name: "staff_monthly_records");

            migrationBuilder.DropTable(
                name: "transportations");

            migrationBuilder.DropTable(
                name: "vfs_appointment_govt_fees");

            migrationBuilder.DropTable(
                name: "flight_bookings");

            migrationBuilder.DropTable(
                name: "holiday_packages");

            migrationBuilder.DropTable(
                name: "sales_visa_applicants");

            migrationBuilder.DropTable(
                name: "vfs_appointments");

            migrationBuilder.DropTable(
                name: "sales_visa_applications");

            migrationBuilder.DropTable(
                name: "heads");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
