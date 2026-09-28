using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demand_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    multiplier = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    color = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_demand_levels", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "matching_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    round = table.Column<int>(type: "int", nullable: false),
                    radius_meters = table.Column<int>(type: "int", nullable: false),
                    candidates_count = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    outcome = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matching_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_matching_attempts_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    polygon = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    center_lat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    center_lng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    operating_hours = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zones", x => x.id);
                    table.ForeignKey(
                        name: "fk_zones_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "matching_candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    attempt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    distance_m = table.Column<int>(type: "int", nullable: false),
                    eta_s = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<decimal>(type: "decimal(6,4)", precision: 6, scale: 4, nullable: false),
                    rank = table.Column<int>(type: "int", nullable: false),
                    offered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    response = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matching_candidates", x => x.id);
                    table.ForeignKey(
                        name: "fk_matching_candidates_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "matching_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_matching_candidates_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "demand_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    demand_level_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    starts_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_demand_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_demand_overrides_demand_level_id",
                        column: x => x.demand_level_id,
                        principalTable: "demand_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_demand_overrides_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_demand_overrides_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "demand_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    metric = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    window_minutes = table.Column<int>(type: "int", nullable: false),
                    threshold_moderate = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    threshold_high = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    threshold_very_high = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_demand_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_demand_rules_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_demand_rules_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "demand_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    computed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    requests_count = table.Column<int>(type: "int", nullable: false),
                    online_drivers = table.Column<int>(type: "int", nullable: false),
                    ratio = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    demand_level_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_demand_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_demand_snapshots_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "fare_quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    group_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    passenger_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pickup_zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    dropoff_zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    distance_m = table.Column<int>(type: "int", nullable: false),
                    duration_s = table.Column<int>(type: "int", nullable: false),
                    breakdown = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    demand_level_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    total = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    driver_net_earnings = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    driver_share_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    offer_min = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    offer_max = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    pricing_rule_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    used_trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fare_quotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fare_quotes_dropoff_zone_id",
                        column: x => x.dropoff_zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_fare_quotes_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fare_quotes_pickup_zone_id",
                        column: x => x.pickup_zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_fare_quotes_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fare_quotes_used_trip_id",
                        column: x => x.used_trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "matching_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    radius_meters = table.Column<int>(type: "int", nullable: false),
                    max_radius_meters = table.Column<int>(type: "int", nullable: false),
                    radius_step_meters = table.Column<int>(type: "int", nullable: false),
                    offer_timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    search_timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    max_candidates = table.Column<int>(type: "int", nullable: false),
                    weights = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allow_category_upgrade = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    prefer_favorite_driver = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matching_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_matching_settings_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_matching_settings_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    base_fare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    per_km = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    per_minute = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    booking_fee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    service_fee_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    min_fare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    waiting_per_minute = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    free_waiting_minutes = table.Column<int>(type: "int", nullable: false),
                    cancellation_fee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    driver_share_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    effective_from = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    effective_to = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_pricing_rules_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pricing_rules_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "zone_category_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    surge_cap = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zone_category_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_zone_category_settings_ride_category_id",
                        column: x => x.ride_category_id,
                        principalTable: "ride_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_zone_category_settings_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pricing_time_multipliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pricing_rule_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    day_of_week = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    from_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    to_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    multiplier = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    label = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_time_multipliers", x => x.id);
                    table.ForeignKey(
                        name: "fk_pricing_time_multipliers_pricing_rule_id",
                        column: x => x.pricing_rule_id,
                        principalTable: "pricing_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_demand_levels_code",
                table: "demand_levels",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_demand_overrides_demand_level_id",
                table: "demand_overrides",
                column: "demand_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_demand_overrides_ride_category_id",
                table: "demand_overrides",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_demand_overrides_zone_id_starts_at_ends_at",
                table: "demand_overrides",
                columns: new[] { "zone_id", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_demand_rules_ride_category_id",
                table: "demand_rules",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_demand_rules_zone_id_ride_category_id_is_active",
                table: "demand_rules",
                columns: new[] { "zone_id", "ride_category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_demand_snapshots_zone_id_computed_at",
                table: "demand_snapshots",
                columns: new[] { "zone_id", "computed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fare_quotes_dropoff_zone_id",
                table: "fare_quotes",
                column: "dropoff_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_quotes_group_id",
                table: "fare_quotes",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_quotes_passenger_id_created_at",
                table: "fare_quotes",
                columns: new[] { "passenger_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fare_quotes_pickup_zone_id",
                table: "fare_quotes",
                column: "pickup_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_quotes_ride_category_id",
                table: "fare_quotes",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_quotes_used_trip_id",
                table: "fare_quotes",
                column: "used_trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_matching_attempts_trip_id_round",
                table: "matching_attempts",
                columns: new[] { "trip_id", "round" });

            migrationBuilder.CreateIndex(
                name: "ix_matching_candidates_attempt_id_rank",
                table: "matching_candidates",
                columns: new[] { "attempt_id", "rank" });

            migrationBuilder.CreateIndex(
                name: "ix_matching_candidates_driver_id",
                table: "matching_candidates",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_matching_settings_ride_category_id",
                table: "matching_settings",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ux_matching_settings_zone_id_ride_category_id",
                table: "matching_settings",
                columns: new[] { "zone_id", "ride_category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_ride_category_id_zone_id_is_active",
                table: "pricing_rules",
                columns: new[] { "ride_category_id", "zone_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_zone_id",
                table: "pricing_rules",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_time_multipliers_pricing_rule_id",
                table: "pricing_time_multipliers",
                column: "pricing_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_category_settings_ride_category_id",
                table: "zone_category_settings",
                column: "ride_category_id");

            migrationBuilder.CreateIndex(
                name: "ux_zone_category_settings_zone_id_ride_category_id",
                table: "zone_category_settings",
                columns: new[] { "zone_id", "ride_category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_zones_city_id_is_active",
                table: "zones",
                columns: new[] { "city_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_zones_code",
                table: "zones",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demand_overrides");

            migrationBuilder.DropTable(
                name: "demand_rules");

            migrationBuilder.DropTable(
                name: "demand_snapshots");

            migrationBuilder.DropTable(
                name: "fare_quotes");

            migrationBuilder.DropTable(
                name: "matching_candidates");

            migrationBuilder.DropTable(
                name: "matching_settings");

            migrationBuilder.DropTable(
                name: "pricing_time_multipliers");

            migrationBuilder.DropTable(
                name: "zone_category_settings");

            migrationBuilder.DropTable(
                name: "demand_levels");

            migrationBuilder.DropTable(
                name: "matching_attempts");

            migrationBuilder.DropTable(
                name: "pricing_rules");

            migrationBuilder.DropTable(
                name: "zones");
        }
    }
}
