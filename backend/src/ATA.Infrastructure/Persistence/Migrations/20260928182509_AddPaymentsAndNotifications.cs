using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discount_total",
                table: "trips",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "fare_breakdown",
                table: "trips",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "payment_method_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "default_payment_method_id",
                table: "passengers",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "campaign_id",
                table: "notifications",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "notifications",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "system")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "transaction_id",
                table: "ledger_entries",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "journal_id",
                table: "ledger_entries",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "on_duty",
                table: "admin_accounts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "document_expiry_notices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    offset_days = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_expiry_notices", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_expiry_notices_driver_document_id",
                        column: x => x.driver_document_id,
                        principalTable: "driver_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ledger_journals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reference_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reference_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    idempotency_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_journals", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channels = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    audience = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_ar = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_en = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deep_link = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scheduled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    target_count = table.Column<int>(type: "int", nullable: false),
                    inapp_created = table.Column<int>(type: "int", nullable: false),
                    push_sent = table.Column<int>(type: "int", nullable: false),
                    push_failed = table.Column<int>(type: "int", nullable: false),
                    push_skipped = table.Column<int>(type: "int", nullable: false),
                    sms_sent = table.Column<int>(type: "int", nullable: false),
                    sms_failed = table.Column<int>(type: "int", nullable: false),
                    opened_count = table.Column<int>(type: "int", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_campaigns", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_ar = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_en = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    provider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gateway_token = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    brand = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last4 = table.Column<string>(type: "char(4)", fixedLength: true, maxLength: 4, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expiry_month = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    expiry_year = table.Column<short>(type: "smallint", nullable: false),
                    holder_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    verified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    removed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_methods_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payment_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    provider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    event_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    event_type = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gateway_payment_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signature_valid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    payload = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    processing_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    error = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    received_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    processed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_webhook_events", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payout_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    batch_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payouts_count = table.Column<int>(type: "int", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    export_file_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    exported_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    exported_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    bank_reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paid_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payout_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_payout_batches_export_file_id",
                        column: x => x.export_file_id,
                        principalTable: "stored_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "settlement_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    batch_number = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    period_start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    period_end = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drivers_count = table.Column<int>(type: "int", nullable: false),
                    total_trips = table.Column<int>(type: "int", nullable: false),
                    total_gross_fares = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_earnings = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_commission = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_cash_collected = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_incentives = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_compensation = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_adjustments = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total_net = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    error = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    generated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    finalized_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    finalized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settlement_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_settlement_batches_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    notification_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    phone_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    event_code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    campaign_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    skipped_reason = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_message_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    error_code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    error_message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attempts = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    opened_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    payload = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "notification_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    purpose = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    wallet_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    payment_method_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    method = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    authorized_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    captured_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    refunded_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capture_mode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gateway_payment_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gateway_status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    return_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failure_code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    failure_message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    idempotency_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    captured_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    voided_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    metadata = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    payout_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    wallet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    iban_masked = table.Column<string>(type: "varchar(34)", maxLength: 34, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iban_encrypted = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_holder_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    batch_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    idempotency_key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    approved_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    paid_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    bank_reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rejected_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    rejected_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_payouts_batch_id",
                        column: x => x.batch_id,
                        principalTable: "payout_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payouts_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payouts_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    refund_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    destination = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requested_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    approved_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    rejected_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    rejected_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gateway_refund_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    failure_message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    processed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    dispute_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refunds", x => x.id);
                    table.ForeignKey(
                        name: "fk_refunds_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refunds_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    batch_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trips_count = table.Column<int>(type: "int", nullable: false),
                    gross_fares = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    earnings = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    commission = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    cash_collected = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    incentives = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    cancellation_compensation = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    adjustments = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    fees = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    topups = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    payouts_in_period = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    opening_balance = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    closing_balance = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    direction = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payout_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_settlements_batch_id",
                        column: x => x.batch_id,
                        principalTable: "settlement_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_settlements_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_settlements_payout_id",
                        column: x => x.payout_id,
                        principalTable: "payouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_trips_payment_method_id",
                table: "trips",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_passengers_default_payment_method_id",
                table: "passengers",
                column: "default_payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_campaign_id",
                table: "notifications",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_journal_id",
                table: "ledger_entries",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "ux_document_expiry_notices_driver_document_id_offset_days",
                table: "document_expiry_notices",
                columns: new[] { "driver_document_id", "offset_days" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journals_created_at",
                table: "ledger_journals",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journals_reference_type_reference_id",
                table: "ledger_journals",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ux_ledger_journals_idempotency_key",
                table: "ledger_journals",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_campaigns_status_scheduled_at",
                table: "notification_campaigns",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_campaign_id",
                table: "notification_deliveries",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_notification_id",
                table: "notification_deliveries",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_status_next_attempt_at",
                table: "notification_deliveries",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_user_id_created_at",
                table: "notification_deliveries",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_templates_code_channel",
                table: "notification_templates",
                columns: new[] { "code", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_user_id_status",
                table: "payment_methods",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_payment_methods_user_id_fingerprint",
                table: "payment_methods",
                columns: new[] { "user_id", "fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_gateway_payment_id",
                table: "payment_webhook_events",
                column: "gateway_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_processing_status_received_at",
                table: "payment_webhook_events",
                columns: new[] { "processing_status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ux_payment_webhook_events_provider_event_id",
                table: "payment_webhook_events",
                columns: new[] { "provider", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_method_id",
                table: "payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status_created_at",
                table: "payments",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_trip_id",
                table: "payments",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_user_id_created_at",
                table: "payments",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_wallet_id",
                table: "payments",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ux_payments_gateway_payment_id",
                table: "payments",
                column: "gateway_payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payments_idempotency_key",
                table: "payments",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payout_batches_export_file_id",
                table: "payout_batches",
                column: "export_file_id");

            migrationBuilder.CreateIndex(
                name: "ux_payout_batches_batch_number",
                table: "payout_batches",
                column: "batch_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payouts_batch_id",
                table: "payouts",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_payouts_driver_id_requested_at",
                table: "payouts",
                columns: new[] { "driver_id", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payouts_status",
                table: "payouts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payouts_wallet_id",
                table: "payouts",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ux_payouts_idempotency_key",
                table: "payouts",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payouts_payout_number",
                table: "payouts",
                column: "payout_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refunds_payment_id",
                table: "refunds",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_status_created_at",
                table: "refunds",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_refunds_trip_id",
                table: "refunds",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_user_id",
                table: "refunds",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refunds_refund_number",
                table: "refunds",
                column: "refund_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settlement_batches_city_id_period_start",
                table: "settlement_batches",
                columns: new[] { "city_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ux_settlement_batches_batch_number",
                table: "settlement_batches",
                column: "batch_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settlements_driver_id",
                table: "settlements",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_payout_id",
                table: "settlements",
                column: "payout_id");

            migrationBuilder.CreateIndex(
                name: "ux_settlements_batch_id_driver_id",
                table: "settlements",
                columns: new[] { "batch_id", "driver_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ledger_entries_journal_id",
                table: "ledger_entries",
                column: "journal_id",
                principalTable: "ledger_journals",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_campaign_id",
                table: "notifications",
                column: "campaign_id",
                principalTable: "notification_campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_passengers_default_payment_method_id",
                table: "passengers",
                column: "default_payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_payment_method_id",
                table: "trips",
                column: "payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // F13: legacy snake_case notification types become catalogue event codes (clients also normalise old values).
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver.application.approved', category = 'system' WHERE type = 'driver_application_approved';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver.application.rejected', category = 'system' WHERE type = 'driver_application_rejected';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver.application.under_review', category = 'system' WHERE type = 'driver_application_under_review';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver.suspended', category = 'system' WHERE type = 'driver_suspended';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver.reinstated', category = 'system' WHERE type = 'driver_reinstated';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip.driver_assigned', category = 'trips' WHERE type = 'trip_driver_assigned';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip.driver_arrived', category = 'trips' WHERE type = 'trip_driver_arrived';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip.completed', category = 'trips' WHERE type = 'trip_completed';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip.cancelled', category = 'trips' WHERE type = 'trip_cancelled';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip.no_drivers', category = 'trips' WHERE type = 'trip_no_drivers';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the pre-F13 notification types.
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver_application_approved' WHERE type = 'driver.application.approved';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver_application_rejected' WHERE type = 'driver.application.rejected';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver_application_under_review' WHERE type = 'driver.application.under_review';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver_suspended' WHERE type = 'driver.suspended';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'driver_reinstated' WHERE type = 'driver.reinstated';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip_driver_assigned' WHERE type = 'trip.driver_assigned';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip_driver_arrived' WHERE type = 'trip.driver_arrived';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip_completed' WHERE type = 'trip.completed';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip_cancelled' WHERE type = 'trip.cancelled';");
            migrationBuilder.Sql("UPDATE notifications SET type = 'trip_no_drivers' WHERE type = 'trip.no_drivers';");

            migrationBuilder.DropForeignKey(
                name: "fk_ledger_entries_journal_id",
                table: "ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_notifications_campaign_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "fk_passengers_default_payment_method_id",
                table: "passengers");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_payment_method_id",
                table: "trips");

            migrationBuilder.DropTable(
                name: "document_expiry_notices");

            migrationBuilder.DropTable(
                name: "ledger_journals");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "payment_webhook_events");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "settlements");

            migrationBuilder.DropTable(
                name: "notification_campaigns");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "settlement_batches");

            migrationBuilder.DropTable(
                name: "payouts");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "payout_batches");

            migrationBuilder.DropIndex(
                name: "ix_trips_payment_method_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_passengers_default_payment_method_id",
                table: "passengers");

            migrationBuilder.DropIndex(
                name: "ix_notifications_campaign_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_ledger_entries_journal_id",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "discount_total",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "fare_breakdown",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "payment_method_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "default_payment_method_id",
                table: "passengers");

            migrationBuilder.DropColumn(
                name: "campaign_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "category",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "journal_id",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "on_duty",
                table: "admin_accounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "transaction_id",
                table: "ledger_entries",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}
