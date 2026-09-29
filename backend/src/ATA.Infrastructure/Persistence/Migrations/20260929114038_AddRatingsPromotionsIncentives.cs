using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingsPromotionsIncentives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "rating_reminded_at",
                table: "trips",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tier_commission_discount_percent",
                table: "trips",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "base_amount",
                table: "fare_quotes",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "driver_incentives",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_ar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_en = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    zone_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ride_category_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_trips = table.Column<int>(type: "int", nullable: false),
                    reward_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    min_trip_fare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    starts_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    days_of_week = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    daily_from = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    daily_to = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    min_tier = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    min_rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    requires_opt_in = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    max_participants = table.Column<int>(type: "int", nullable: true),
                    budget_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    spent_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    notify_on_publish = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver_incentives", x => x.id);
                    table.ForeignKey(
                        name: "fk_driver_incentives_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driver_incentives_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "driver_tier_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_tier = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    to_tier = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metrics = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    computed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver_tier_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_driver_tier_history_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_driver_tier_history_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "driver_tier_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tier = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    min_completed_trips = table.Column<int>(type: "int", nullable: false),
                    min_rating_avg = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    min_acceptance_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    max_cancellation_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    commission_discount_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    matching_norm = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    benefits_ar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    benefits_en = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver_tier_rules", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_ar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_en = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    max_discount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    min_fare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    valid_from = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    valid_to = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    total_usage_limit = table.Column<int>(type: "int", nullable: true),
                    per_user_limit = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    usage_count = table.Column<int>(type: "int", nullable: false),
                    budget_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    spent_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    first_trip_only = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    new_users_only = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    new_user_days = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ride_category_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    zone_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_methods = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    booking_types = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_stackable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_public = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotions_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotions_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rating_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rating_tags", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rater_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rater_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ratee_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ratee_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stars = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    tags = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment_hidden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hidden_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    hidden_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hidden_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ratings", x => x.id);
                    table.ForeignKey(
                        name: "fk_ratings_hidden_by",
                        column: x => x.hidden_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ratings_ratee_user_id",
                        column: x => x.ratee_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ratings_rater_user_id",
                        column: x => x.rater_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ratings_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "driver_incentive_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    incentive_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    period_start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    period_end = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    opted_in_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_trips = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    achieved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    incentive_multiplier = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    reward_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    wallet_transaction_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    voided_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver_incentive_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_driver_incentive_progress_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_driver_incentive_progress_incentive_id",
                        column: x => x.incentive_id,
                        principalTable: "driver_incentives",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "promotion_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    promotion_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    passenger_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reserved_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    discount_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    reserved_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    applied_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    released_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    release_reason = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_redemptions_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promotion_redemptions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_redemptions_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rating_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rating_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    value = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rating_flags", x => x.id);
                    table.ForeignKey(
                        name: "fk_rating_flags_rating_id",
                        column: x => x.rating_id,
                        principalTable: "ratings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rating_flags_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rating_flags_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "driver_incentive_trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    progress_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    counted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver_incentive_trips", x => x.id);
                    table.ForeignKey(
                        name: "fk_driver_incentive_trips_progress_id",
                        column: x => x.progress_id,
                        principalTable: "driver_incentive_progress",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_driver_incentive_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_driver_incentive_progress_driver_id_status",
                table: "driver_incentive_progress",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_driver_incentive_progress_status_period_end",
                table: "driver_incentive_progress",
                columns: new[] { "status", "period_end" });

            migrationBuilder.CreateIndex(
                name: "ux_driver_incentive_progress_incentive_id_driver_id_period_start",
                table: "driver_incentive_progress",
                columns: new[] { "incentive_id", "driver_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_driver_incentive_trips_trip_id",
                table: "driver_incentive_trips",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ux_driver_incentive_trips_progress_id_trip_id",
                table: "driver_incentive_trips",
                columns: new[] { "progress_id", "trip_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_driver_incentives_city_id_is_active_starts_at_ends_at",
                table: "driver_incentives",
                columns: new[] { "city_id", "is_active", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_driver_incentives_created_by",
                table: "driver_incentives",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_driver_tier_history_changed_by",
                table: "driver_tier_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "ix_driver_tier_history_driver_id_computed_at",
                table: "driver_tier_history",
                columns: new[] { "driver_id", "computed_at" });

            migrationBuilder.CreateIndex(
                name: "ux_driver_tier_rules_tier",
                table: "driver_tier_rules",
                column: "tier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_redemptions_passenger_id_promotion_id",
                table: "promotion_redemptions",
                columns: new[] { "passenger_id", "promotion_id" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_redemptions_promotion_id_status",
                table: "promotion_redemptions",
                columns: new[] { "promotion_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_promotion_redemptions_trip_id",
                table: "promotion_redemptions",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotions_city_id",
                table: "promotions",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_created_by",
                table: "promotions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_is_active_valid_from_valid_to",
                table: "promotions",
                columns: new[] { "is_active", "valid_from", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "ux_promotions_code",
                table: "promotions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rating_flags_rating_id",
                table: "rating_flags",
                column: "rating_id");

            migrationBuilder.CreateIndex(
                name: "ix_rating_flags_reviewed_by",
                table: "rating_flags",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_rating_flags_status_created_at",
                table: "rating_flags",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_flags_user_id_type_status",
                table: "rating_flags",
                columns: new[] { "user_id", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_rating_tags_target_role_code",
                table: "rating_tags",
                columns: new[] { "target_role", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ratings_hidden_by",
                table: "ratings",
                column: "hidden_by");

            migrationBuilder.CreateIndex(
                name: "ix_ratings_ratee_user_id_ratee_role_created_at",
                table: "ratings",
                columns: new[] { "ratee_user_id", "ratee_role", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ratings_rater_user_id_created_at",
                table: "ratings",
                columns: new[] { "rater_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ratings_trip_id_rater_role",
                table: "ratings",
                columns: new[] { "trip_id", "rater_role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "driver_incentive_trips");

            migrationBuilder.DropTable(
                name: "driver_tier_history");

            migrationBuilder.DropTable(
                name: "driver_tier_rules");

            migrationBuilder.DropTable(
                name: "promotion_redemptions");

            migrationBuilder.DropTable(
                name: "rating_flags");

            migrationBuilder.DropTable(
                name: "rating_tags");

            migrationBuilder.DropTable(
                name: "driver_incentive_progress");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "driver_incentives");

            migrationBuilder.DropColumn(
                name: "rating_reminded_at",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "tier_commission_discount_percent",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "base_amount",
                table: "fare_quotes");
        }
    }
}
