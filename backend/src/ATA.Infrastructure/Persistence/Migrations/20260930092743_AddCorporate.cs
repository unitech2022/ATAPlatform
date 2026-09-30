using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorporate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "booked_by_user_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "corporate_account_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "corporate_user_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "cost_center_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "guest_name",
                table: "trips",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "guest_phone",
                table: "trips",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "is_guest",
                table: "trips",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "trip_purpose",
                table: "trips",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "session_kind",
                table: "refresh_tokens",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "app")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    account_number = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    legal_name_ar = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    legal_name_en = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cr_number = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vat_number = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_address = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    contact_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contact_phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    credit_limit = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    billing_cycle = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_terms_days = table.Column<int>(type: "int", nullable: false),
                    default_policy_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_accounts_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_corporate_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    key_prefix = table.Column<string>(type: "char(8)", fixedLength: true, maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    key_hash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scopes = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_api_keys_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_corporate_api_keys_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_cost_centers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_cost_centers", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_cost_centers_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    invoice_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trips_count = table.Column<int>(type: "int", nullable: false),
                    subtotal_excl_vat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_incl_vat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    seller_snapshot = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    buyer_snapshot = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pdf_file_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    issued_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    issued_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    paid_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    payment_reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    void_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    period_active = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_invoices_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_corporate_invoices_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_corporate_invoices_pdf_file_id",
                        column: x => x.pdf_file_id,
                        principalTable: "stored_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    allowed_ride_category_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allowed_days = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time_windows = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allowed_zone_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    zone_match = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_fare_per_trip = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    monthly_budget_per_employee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    require_purpose = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    require_cost_center = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    allow_scheduled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    allow_guest_booking = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_policies_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    invoice_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_adjustments_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_corporate_adjustments_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_corporate_adjustments_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "corporate_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    invoice_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    line_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    trip_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trip_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    employee_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    employee_number = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    department = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cost_center_code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guest_name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    purpose = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pickup_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dropoff_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount_excl_vat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_incl_vat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_invoice_lines_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "corporate_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_corporate_invoice_lines_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    phone_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    employee_number = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    department = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cost_center_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    policy_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    monthly_budget = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    invited_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    activated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    disabled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_users_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_corporate_users_cost_center_id",
                        column: x => x.cost_center_id,
                        principalTable: "corporate_cost_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_corporate_users_policy_id",
                        column: x => x.policy_id,
                        principalTable: "corporate_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_corporate_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "corporate_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    corporate_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    phone_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    token_hash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    declined_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_invitations_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalTable: "corporate_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_corporate_invitations_corporate_user_id",
                        column: x => x.corporate_user_id,
                        principalTable: "corporate_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_trips_corporate_account_id_completed_at",
                table: "trips",
                columns: new[] { "corporate_account_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_corporate_user_id",
                table: "trips",
                column: "corporate_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_cost_center_id",
                table: "trips",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_accounts_city_id",
                table: "corporate_accounts",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_accounts_created_by",
                table: "corporate_accounts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_accounts_status",
                table: "corporate_accounts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_corporate_accounts_account_number",
                table: "corporate_accounts",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_corporate_accounts_cr_number",
                table: "corporate_accounts",
                column: "cr_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corporate_adjustments_corporate_account_id_invoice_id",
                table: "corporate_adjustments",
                columns: new[] { "corporate_account_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_corporate_adjustments_created_by",
                table: "corporate_adjustments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_adjustments_invoice_id",
                table: "corporate_adjustments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_api_keys_corporate_account_id",
                table: "corporate_api_keys",
                column: "corporate_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_api_keys_created_by",
                table: "corporate_api_keys",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_api_keys_key_prefix",
                table: "corporate_api_keys",
                column: "key_prefix");

            migrationBuilder.CreateIndex(
                name: "ux_corporate_cost_centers_corporate_account_id_code",
                table: "corporate_cost_centers",
                columns: new[] { "corporate_account_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invitations_corporate_account_id",
                table: "corporate_invitations",
                column: "corporate_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invitations_corporate_user_id",
                table: "corporate_invitations",
                column: "corporate_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invitations_phone_number_expires_at",
                table: "corporate_invitations",
                columns: new[] { "phone_number", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_corporate_invitations_token_hash",
                table: "corporate_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoice_lines_invoice_id",
                table: "corporate_invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoice_lines_trip_id",
                table: "corporate_invoice_lines",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoices_issued_by",
                table: "corporate_invoices",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoices_pdf_file_id",
                table: "corporate_invoices",
                column: "pdf_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoices_status_due_date",
                table: "corporate_invoices",
                columns: new[] { "status", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ux_corporate_invoices_corporate_account_id_period_start_period_active",
                table: "corporate_invoices",
                columns: new[] { "corporate_account_id", "period_start", "period_active" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_corporate_invoices_invoice_number",
                table: "corporate_invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corporate_policies_corporate_account_id",
                table: "corporate_policies",
                column: "corporate_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_users_cost_center_id",
                table: "corporate_users",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_users_phone_number_status",
                table: "corporate_users",
                columns: new[] { "phone_number", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_corporate_users_policy_id",
                table: "corporate_users",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_users_user_id_status",
                table: "corporate_users",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_corporate_users_corporate_account_id_phone_number",
                table: "corporate_users",
                columns: new[] { "corporate_account_id", "phone_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_corporate_account_id",
                table: "trips",
                column: "corporate_account_id",
                principalTable: "corporate_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_corporate_user_id",
                table: "trips",
                column: "corporate_user_id",
                principalTable: "corporate_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_cost_center_id",
                table: "trips",
                column: "cost_center_id",
                principalTable: "corporate_cost_centers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trips_corporate_account_id",
                table: "trips");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_corporate_user_id",
                table: "trips");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_cost_center_id",
                table: "trips");

            migrationBuilder.DropTable(
                name: "corporate_adjustments");

            migrationBuilder.DropTable(
                name: "corporate_api_keys");

            migrationBuilder.DropTable(
                name: "corporate_invitations");

            migrationBuilder.DropTable(
                name: "corporate_invoice_lines");

            migrationBuilder.DropTable(
                name: "corporate_users");

            migrationBuilder.DropTable(
                name: "corporate_invoices");

            migrationBuilder.DropTable(
                name: "corporate_cost_centers");

            migrationBuilder.DropTable(
                name: "corporate_policies");

            migrationBuilder.DropTable(
                name: "corporate_accounts");

            migrationBuilder.DropIndex(
                name: "ix_trips_corporate_account_id_completed_at",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_corporate_user_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_cost_center_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "booked_by_user_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "corporate_account_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "corporate_user_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "cost_center_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "guest_name",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "guest_phone",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "is_guest",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "trip_purpose",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "session_kind",
                table: "refresh_tokens");
        }
    }
}
