using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacMfaReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "absolute_expires_at",
                table: "refresh_tokens",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                table: "refresh_tokens",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "refresh_tokens",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "mfa_secret",
                table: "admin_accounts",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "failed_login_count",
                table: "admin_accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until",
                table: "admin_accounts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "mfa_enrolled_at",
                table: "admin_accounts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mfa_failed_count",
                table: "admin_accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "mfa_last_step",
                table: "admin_accounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                table: "admin_accounts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                table: "admin_accounts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_recovery_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    admin_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_recovery_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_recovery_codes_admin_account_id",
                        column: x => x.admin_account_id,
                        principalTable: "admin_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    module = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "report_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    snapshot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scope_key = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    zone_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ride_category_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    metric_code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    numerator = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    denominator = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    computed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_snapshots", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_system = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "admin_account_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    admin_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_account_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_account_roles_admin_account_id",
                        column: x => x.admin_account_id,
                        principalTable: "admin_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_admin_account_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    permission_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_session_kind_revoked_at",
                table: "refresh_tokens",
                columns: new[] { "session_kind", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admin_account_roles_role_id",
                table: "admin_account_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ux_admin_account_roles_admin_account_id_role_id",
                table: "admin_account_roles",
                columns: new[] { "admin_account_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admin_recovery_codes_admin_account_id",
                table: "admin_recovery_codes",
                column: "admin_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_snapshots_metric_code_snapshot_date",
                table: "report_snapshots",
                columns: new[] { "metric_code", "snapshot_date" });

            migrationBuilder.CreateIndex(
                name: "ux_report_snapshots_snapshot_date_scope_key_metric_code",
                table: "report_snapshots",
                columns: new[] { "snapshot_date", "scope_key", "metric_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_roles_code",
                table: "roles",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_account_roles");

            migrationBuilder.DropTable(
                name: "admin_recovery_codes");

            migrationBuilder.DropTable(
                name: "report_snapshots");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_session_kind_revoked_at",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "absolute_expires_at",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "failed_login_count",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "mfa_enrolled_at",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "mfa_failed_count",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "mfa_last_step",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "must_change_password",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                table: "admin_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "mfa_secret",
                table: "admin_accounts",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(512)",
                oldMaxLength: 512,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
