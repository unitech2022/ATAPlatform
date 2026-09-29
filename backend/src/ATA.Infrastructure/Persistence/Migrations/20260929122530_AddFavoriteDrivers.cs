using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteDrivers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "favorite_discount_rule_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "favorite_driver_id",
                table: "trips",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "favorite_status",
                table: "trips",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "mode",
                table: "matching_attempts",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "normal")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "favorite_driver_discount_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    discount_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    min_fare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    stackable_with_promotions = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    valid_from = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    valid_to = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ride_category_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    zone_ids = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    booking_types = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorite_driver_discount_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_favorite_driver_discount_rules_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "favorite_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    passenger_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    driver_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    source_trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorite_drivers", x => x.id);
                    table.ForeignKey(
                        name: "fk_favorite_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favorite_drivers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favorite_drivers_source_trip_id",
                        column: x => x.source_trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_trips_favorite_discount_rule_id",
                table: "trips",
                column: "favorite_discount_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_favorite_driver_id_favorite_status",
                table: "trips",
                columns: new[] { "favorite_driver_id", "favorite_status" });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_driver_discount_rules_created_by",
                table: "favorite_driver_discount_rules",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_favorite_driver_discount_rules_is_active_priority",
                table: "favorite_driver_discount_rules",
                columns: new[] { "is_active", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_drivers_driver_id",
                table: "favorite_drivers",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_favorite_drivers_source_trip_id",
                table: "favorite_drivers",
                column: "source_trip_id");

            migrationBuilder.CreateIndex(
                name: "ux_favorite_drivers_passenger_id_driver_id",
                table: "favorite_drivers",
                columns: new[] { "passenger_id", "driver_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_favorite_discount_rule_id",
                table: "trips",
                column: "favorite_discount_rule_id",
                principalTable: "favorite_driver_discount_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_favorite_driver_id",
                table: "trips",
                column: "favorite_driver_id",
                principalTable: "drivers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trips_favorite_discount_rule_id",
                table: "trips");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_favorite_driver_id",
                table: "trips");

            migrationBuilder.DropTable(
                name: "favorite_driver_discount_rules");

            migrationBuilder.DropTable(
                name: "favorite_drivers");

            migrationBuilder.DropIndex(
                name: "ix_trips_favorite_discount_rule_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_favorite_driver_id_favorite_status",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "favorite_discount_rule_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "favorite_driver_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "favorite_status",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "matching_attempts");
        }
    }
}
