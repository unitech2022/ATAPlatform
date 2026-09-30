using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledRidesAndAirport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "airport_direction",
                table: "trips",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "airport_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "airport_zone_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "flight_number",
                table: "trips",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "reserved_driver_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "terminal_code",
                table: "trips",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "waiting_policy",
                table: "trips",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    lng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    geofence = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requires_pickup_zone = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    default_free_waiting_minutes = table.Column<int>(type: "int", nullable: true),
                    default_waiting_per_minute = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    queue_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_airports", x => x.id);
                    table.ForeignKey(
                        name: "fk_airports_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scheduled_ride_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    source = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reserved_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    confirm_requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    final_confirm_requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    released_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    release_reason = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_late_release = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    penalty_points = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_ride_reservations", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_reservations_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_reservations_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scheduled_ride_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    max_days_ahead = table.Column<int>(type: "int", nullable: false),
                    min_lead_minutes = table.Column<int>(type: "int", nullable: false),
                    max_open_per_passenger = table.Column<int>(type: "int", nullable: false),
                    lock_demand_normal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    marketplace_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    marketplace_radius_km = table.Column<int>(type: "int", nullable: false),
                    favorite_exclusive_minutes = table.Column<int>(type: "int", nullable: false),
                    driver_assignment_lead_minutes = table.Column<int>(type: "int", nullable: false),
                    confirmation_timeout_minutes = table.Column<int>(type: "int", nullable: false),
                    final_confirmation_minutes_before = table.Column<int>(type: "int", nullable: false),
                    final_confirmation_timeout_minutes = table.Column<int>(type: "int", nullable: false),
                    search_start_minutes_before = table.Column<int>(type: "int", nullable: false),
                    rider_reminder_offsets = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    driver_reminder_offsets = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    free_cancel_minutes_before = table.Column<int>(type: "int", nullable: false),
                    late_cancel_fee_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    late_cancel_fee_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    late_cancel_fee_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    late_cancel_driver_compensation_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    driver_free_release_minutes_before = table.Column<int>(type: "int", nullable: false),
                    driver_late_release_penalty_points = table.Column<int>(type: "int", nullable: false),
                    driver_confirmation_missed_penalty_points = table.Column<int>(type: "int", nullable: false),
                    driver_no_show_penalty_points = table.Column<int>(type: "int", nullable: false),
                    driver_no_show_grace_minutes = table.Column<int>(type: "int", nullable: false),
                    max_reservations_per_driver = table.Column<int>(type: "int", nullable: false),
                    reservation_gap_minutes = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_ride_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_rules_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_rules_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "airport_queue_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    airport_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    offered_trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    left_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    left_reason = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_airport_queue_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_airport_queue_entries_airport_id",
                        column: x => x.airport_id,
                        principalTable: "airports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_airport_queue_entries_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_airport_queue_entries_offered_trip_id",
                        column: x => x.offered_trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_airport_queue_entries_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "airport_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    airport_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    terminal_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    polygon = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    lng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    instructions_ar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    instructions_en = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    free_waiting_minutes = table.Column<int>(type: "int", nullable: true),
                    waiting_per_minute = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_airport_zones", x => x.id);
                    table.ForeignKey(
                        name: "fk_airport_zones_airport_id",
                        column: x => x.airport_id,
                        principalTable: "airports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scheduled_ride_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reservation_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    recipient_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recipient_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    offset_minutes = table.Column<int>(type: "int", nullable: false),
                    send_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_ride_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_reminders_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_reminders_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "scheduled_ride_reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_scheduled_ride_reminders_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_trips_airport_id",
                table: "trips",
                column: "airport_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_airport_zone_id",
                table: "trips",
                column: "airport_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_reserved_driver_id",
                table: "trips",
                column: "reserved_driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_status_scheduled_at",
                table: "trips",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_airport_queue_entries_airport_id_status_entered_at",
                table: "airport_queue_entries",
                columns: new[] { "airport_id", "status", "entered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_airport_queue_entries_driver_id_status",
                table: "airport_queue_entries",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_airport_queue_entries_offered_trip_id",
                table: "airport_queue_entries",
                column: "offered_trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_airport_queue_entries_ride_category_id",
                table: "airport_queue_entries",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ux_airport_zones_airport_id_code",
                table: "airport_zones",
                columns: new[] { "airport_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_airports_city_id",
                table: "airports",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ux_airports_code",
                table: "airports",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_reminders_recipient_user_id",
                table: "scheduled_ride_reminders",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_reminders_reservation_id",
                table: "scheduled_ride_reminders",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_reminders_status_send_at",
                table: "scheduled_ride_reminders",
                columns: new[] { "status", "send_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_reminders_trip_id",
                table: "scheduled_ride_reminders",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_reservations_driver_id_status",
                table: "scheduled_ride_reservations",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_reservations_trip_id_status",
                table: "scheduled_ride_reservations",
                columns: new[] { "trip_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_ride_rules_ride_category_id",
                table: "scheduled_ride_rules",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ux_scheduled_ride_rules_city_id_ride_category_id",
                table: "scheduled_ride_rules",
                columns: new[] { "city_id", "ride_category_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_airport_id",
                table: "trips",
                column: "airport_id",
                principalTable: "airports",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_airport_zone_id",
                table: "trips",
                column: "airport_zone_id",
                principalTable: "airport_zones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_reserved_driver_id",
                table: "trips",
                column: "reserved_driver_id",
                principalTable: "drivers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trips_airport_id",
                table: "trips");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_airport_zone_id",
                table: "trips");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_reserved_driver_id",
                table: "trips");

            migrationBuilder.DropTable(
                name: "airport_queue_entries");

            migrationBuilder.DropTable(
                name: "airport_zones");

            migrationBuilder.DropTable(
                name: "scheduled_ride_reminders");

            migrationBuilder.DropTable(
                name: "scheduled_ride_rules");

            migrationBuilder.DropTable(
                name: "airports");

            migrationBuilder.DropTable(
                name: "scheduled_ride_reservations");

            migrationBuilder.DropIndex(
                name: "ix_trips_airport_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_airport_zone_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_reserved_driver_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_status_scheduled_at",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "airport_direction",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "airport_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "airport_zone_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "flight_number",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "reserved_driver_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "terminal_code",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "waiting_policy",
                table: "trips");
        }
    }
}
