using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "canned_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_ar = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_en = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ticket_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canned_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_canned_responses_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "help_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ar = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_en = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    icon = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    audience = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_help_categories", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "support_sla_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    priority = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    first_response_minutes = table.Column<int>(type: "int", nullable: false),
                    resolution_minutes = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_sla_policies", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ticket_number = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requester_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    requester_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    subject = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    assigned_to_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    first_response_due_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    resolution_due_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    first_response_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sla_paused_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sla_paused_seconds = table.Column<int>(type: "int", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    closed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_message_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_message_by = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unread_by_user = table.Column<int>(type: "int", nullable: false),
                    csat_score = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    csat_comment = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    safety_case_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    lost_item_report_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_support_tickets_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_support_tickets_requester_user_id",
                        column: x => x.requester_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_tickets_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "help_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    category_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    slug = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_ar = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_en = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_ar = table.Column<string>(type: "mediumtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_en = table.Column<string>(type: "mediumtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    audience = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tags = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    view_count = table.Column<int>(type: "int", nullable: false),
                    helpful_yes = table.Column<int>(type: "int", nullable: false),
                    helpful_no = table.Column<int>(type: "int", nullable: false),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_help_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_help_articles_category_id",
                        column: x => x.category_id,
                        principalTable: "help_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_help_articles_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "fare_disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ticket_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    trip_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    requester_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    charged_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    requested_refund_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolution = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approved_refund_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    refund_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    resolved_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    resolved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    resolution_note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fare_disputes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fare_disputes_requester_user_id",
                        column: x => x.requester_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fare_disputes_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_fare_disputes_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "support_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fare_disputes_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "support_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ticket_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    author_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    author_role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_internal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_support_messages_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_support_messages_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "support_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "support_message_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    message_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    file_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_message_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_support_message_attachments_file_id",
                        column: x => x.file_id,
                        principalTable: "stored_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_message_attachments_message_id",
                        column: x => x.message_id,
                        principalTable: "support_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_canned_responses_created_by",
                table: "canned_responses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ux_canned_responses_code",
                table: "canned_responses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fare_disputes_requester_user_id",
                table: "fare_disputes",
                column: "requester_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_disputes_resolved_by",
                table: "fare_disputes",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "ix_fare_disputes_status_created_at",
                table: "fare_disputes",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_fare_disputes_ticket_id",
                table: "fare_disputes",
                column: "ticket_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fare_disputes_trip_id",
                table: "fare_disputes",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_help_articles_category_id_is_published_sort_order",
                table: "help_articles",
                columns: new[] { "category_id", "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_help_articles_is_published_view_count",
                table: "help_articles",
                columns: new[] { "is_published", "view_count" });

            migrationBuilder.CreateIndex(
                name: "ix_help_articles_title_ar_title_en_body_ar_body_en",
                table: "help_articles",
                columns: new[] { "title_ar", "title_en", "body_ar", "body_en" })
                .Annotation("MySql:FullTextIndex", true);

            migrationBuilder.CreateIndex(
                name: "ix_help_articles_updated_by",
                table: "help_articles",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ux_help_articles_slug",
                table: "help_articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_help_categories_is_active_sort_order",
                table: "help_categories",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_help_categories_code",
                table: "help_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_message_attachments_file_id",
                table: "support_message_attachments",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_message_attachments_message_id",
                table: "support_message_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_messages_author_user_id",
                table: "support_messages",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_messages_ticket_id_created_at",
                table: "support_messages",
                columns: new[] { "ticket_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_support_sla_policies_priority",
                table: "support_sla_policies",
                column: "priority",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_assigned_to_user_id_status",
                table: "support_tickets",
                columns: new[] { "assigned_to_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_requester_user_id_created_at",
                table: "support_tickets",
                columns: new[] { "requester_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_status_priority_resolution_due_at",
                table: "support_tickets",
                columns: new[] { "status", "priority", "resolution_due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_trip_id",
                table: "support_tickets",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ux_support_tickets_ticket_number",
                table: "support_tickets",
                column: "ticket_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "canned_responses");

            migrationBuilder.DropTable(
                name: "fare_disputes");

            migrationBuilder.DropTable(
                name: "help_articles");

            migrationBuilder.DropTable(
                name: "support_message_attachments");

            migrationBuilder.DropTable(
                name: "support_sla_policies");

            migrationBuilder.DropTable(
                name: "help_categories");

            migrationBuilder.DropTable(
                name: "support_messages");

            migrationBuilder.DropTable(
                name: "support_tickets");
        }
    }
}
