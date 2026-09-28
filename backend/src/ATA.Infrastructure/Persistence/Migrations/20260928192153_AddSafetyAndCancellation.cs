using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyAndCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "planned_route",
                table: "trips",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "planned_route_source",
                table: "trips",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "straight")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cancellation_reasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actor = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stages = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_excusable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_emergency = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    requires_note = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_selectable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cancellation_reasons", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cancellation_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actor = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stage = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    booking_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    free_window_seconds = table.Column<int>(type: "int", nullable: false),
                    fee_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fee_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    fee_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    min_fee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    max_fee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    driver_compensation_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    penalty_points = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cancellation_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_cancellation_rules_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cancellation_rules_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lost_item_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    report_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reporter_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    item_category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contact_phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    driver_response = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    driver_note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    driver_responded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    support_ticket_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    closed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    closed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lost_item_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_lost_item_reports_closed_by",
                        column: x => x.closed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lost_item_reports_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lost_item_reports_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lost_item_reports_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reliability_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    points = table.Column<int>(type: "int", nullable: true),
                    level = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reliability_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_reliability_adjustments_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reliability_adjustments_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reliability_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    window_days = table.Column<int>(type: "int", nullable: false),
                    trips_requested = table.Column<int>(type: "int", nullable: false),
                    offers_received = table.Column<int>(type: "int", nullable: false),
                    offers_accepted = table.Column<int>(type: "int", nullable: false),
                    trips_accepted = table.Column<int>(type: "int", nullable: false),
                    trips_completed = table.Column<int>(type: "int", nullable: false),
                    cancellations_at_fault = table.Column<int>(type: "int", nullable: false),
                    no_show_count = table.Column<int>(type: "int", nullable: false),
                    cancellation_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    acceptance_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    reliability_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    penalty_points = table.Column<int>(type: "int", nullable: false),
                    restriction_level = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    restricted_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    level_changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_computed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reliability_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_reliability_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reliability_thresholds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    min_penalty_points = table.Column<int>(type: "int", nullable: true),
                    min_cancellation_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    min_trips_for_rate = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    restriction_hours = table.Column<int>(type: "int", nullable: true),
                    deprioritize_factor = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    incentive_reduction_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reliability_thresholds", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "safety_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    case_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reporter_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reporter_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    report_category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    lng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    last_lat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    last_lng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    last_location_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    contacts_notified = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    escalated_to = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolution_code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolution = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reporter_cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    support_ticket_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    opened_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    first_response_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safety_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_safety_cases_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_safety_cases_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_safety_cases_subject_user_id",
                        column: x => x.subject_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_safety_cases_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "trip_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    sender_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    sender_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quick_reply_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trip_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_trip_messages_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_trip_messages_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "trusted_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    relationship = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    auto_share = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    notify_on_sos = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trusted_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_trusted_contacts_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cancellation_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    actor = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    at_fault = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stage = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    booking_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reason_code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rule_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    seconds_since_accept = table.Column<int>(type: "int", nullable: true),
                    seconds_since_arrival = table.Column<int>(type: "int", nullable: true),
                    estimated_fare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    fee_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    fee_charged = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    fee_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fee_method = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    compensation_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    penalty_points = table.Column<int>(type: "int", nullable: false),
                    counts_toward_rate = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    excuse_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    review_note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cancellation_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_cancellation_events_reason_id",
                        column: x => x.reason_id,
                        principalTable: "cancellation_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cancellation_events_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cancellation_events_rule_id",
                        column: x => x.rule_id,
                        principalTable: "cancellation_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cancellation_events_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cancellation_events_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "safety_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detected_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    lat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    lng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    metrics = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prompted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    respond_by = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    responded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    response = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    safety_case_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    dismissed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    closed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safety_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_safety_alerts_dismissed_by",
                        column: x => x.dismissed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_safety_alerts_safety_case_id",
                        column: x => x.safety_case_id,
                        principalTable: "safety_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_safety_alerts_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "safety_case_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    case_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    file_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    uploaded_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safety_case_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_safety_case_attachments_case_id",
                        column: x => x.case_id,
                        principalTable: "safety_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_safety_case_attachments_file_id",
                        column: x => x.file_id,
                        principalTable: "stored_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_safety_case_attachments_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "safety_case_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    case_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    author_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_internal = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safety_case_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_safety_case_notes_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_safety_case_notes_case_id",
                        column: x => x.case_id,
                        principalTable: "safety_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "trip_shares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    token = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trusted_contact_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    channel = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    view_count = table.Column<int>(type: "int", nullable: false),
                    last_viewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trip_shares", x => x.id);
                    table.ForeignKey(
                        name: "fk_trip_shares_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trip_shares_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trip_shares_trusted_contact_id",
                        column: x => x.trusted_contact_id,
                        principalTable: "trusted_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_events_created_at",
                table: "cancellation_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_events_excuse_status",
                table: "cancellation_events",
                column: "excuse_status");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_events_reason_id",
                table: "cancellation_events",
                column: "reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_events_reviewed_by",
                table: "cancellation_events",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_events_rule_id",
                table: "cancellation_events",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_events_user_id_created_at",
                table: "cancellation_events",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_cancellation_events_trip_id",
                table: "cancellation_events",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_cancellation_reasons_actor_code",
                table: "cancellation_reasons",
                columns: new[] { "actor", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_rules_actor_stage_is_active",
                table: "cancellation_rules",
                columns: new[] { "actor", "stage", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_rules_ride_category_id",
                table: "cancellation_rules",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_rules_zone_id",
                table: "cancellation_rules",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_lost_item_reports_closed_by",
                table: "lost_item_reports",
                column: "closed_by");

            migrationBuilder.CreateIndex(
                name: "ix_lost_item_reports_driver_id_status",
                table: "lost_item_reports",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lost_item_reports_reporter_user_id_created_at",
                table: "lost_item_reports",
                columns: new[] { "reporter_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_lost_item_reports_trip_id",
                table: "lost_item_reports",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ux_lost_item_reports_report_number",
                table: "lost_item_reports",
                column: "report_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reliability_adjustments_created_by",
                table: "reliability_adjustments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_reliability_adjustments_user_id_role_created_at",
                table: "reliability_adjustments",
                columns: new[] { "user_id", "role", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reliability_profiles_role_restriction_level",
                table: "reliability_profiles",
                columns: new[] { "role", "restriction_level" });

            migrationBuilder.CreateIndex(
                name: "ux_reliability_profiles_user_id_role",
                table: "reliability_profiles",
                columns: new[] { "user_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_reliability_thresholds_role_level",
                table: "reliability_thresholds",
                columns: new[] { "role", "level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_safety_alerts_dismissed_by",
                table: "safety_alerts",
                column: "dismissed_by");

            migrationBuilder.CreateIndex(
                name: "ix_safety_alerts_safety_case_id",
                table: "safety_alerts",
                column: "safety_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_safety_alerts_status_respond_by",
                table: "safety_alerts",
                columns: new[] { "status", "respond_by" });

            migrationBuilder.CreateIndex(
                name: "ix_safety_alerts_trip_id_type_status",
                table: "safety_alerts",
                columns: new[] { "trip_id", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_safety_case_attachments_case_id",
                table: "safety_case_attachments",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_safety_case_attachments_file_id",
                table: "safety_case_attachments",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_safety_case_attachments_uploaded_by",
                table: "safety_case_attachments",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_safety_case_notes_author_user_id",
                table: "safety_case_notes",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_safety_case_notes_case_id_created_at",
                table: "safety_case_notes",
                columns: new[] { "case_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_safety_cases_assigned_to_user_id",
                table: "safety_cases",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_safety_cases_reporter_user_id_opened_at",
                table: "safety_cases",
                columns: new[] { "reporter_user_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_safety_cases_status_priority_opened_at",
                table: "safety_cases",
                columns: new[] { "status", "priority", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_safety_cases_subject_user_id",
                table: "safety_cases",
                column: "subject_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_safety_cases_trip_id",
                table: "safety_cases",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ux_safety_cases_case_number",
                table: "safety_cases",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trip_messages_sender_user_id",
                table: "trip_messages",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_messages_trip_id_created_at",
                table: "trip_messages",
                columns: new[] { "trip_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_trip_shares_created_by_user_id",
                table: "trip_shares",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_shares_trip_id",
                table: "trip_shares",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_shares_trusted_contact_id",
                table: "trip_shares",
                column: "trusted_contact_id");

            migrationBuilder.CreateIndex(
                name: "ux_trip_shares_token",
                table: "trip_shares",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_trusted_contacts_user_id_phone_number",
                table: "trusted_contacts",
                columns: new[] { "user_id", "phone_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cancellation_events");

            migrationBuilder.DropTable(
                name: "lost_item_reports");

            migrationBuilder.DropTable(
                name: "reliability_adjustments");

            migrationBuilder.DropTable(
                name: "reliability_profiles");

            migrationBuilder.DropTable(
                name: "reliability_thresholds");

            migrationBuilder.DropTable(
                name: "safety_alerts");

            migrationBuilder.DropTable(
                name: "safety_case_attachments");

            migrationBuilder.DropTable(
                name: "safety_case_notes");

            migrationBuilder.DropTable(
                name: "trip_messages");

            migrationBuilder.DropTable(
                name: "trip_shares");

            migrationBuilder.DropTable(
                name: "cancellation_reasons");

            migrationBuilder.DropTable(
                name: "cancellation_rules");

            migrationBuilder.DropTable(
                name: "safety_cases");

            migrationBuilder.DropTable(
                name: "trusted_contacts");

            migrationBuilder.DropColumn(
                name: "planned_route",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "planned_route_source",
                table: "trips");
        }
    }
}
