using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalCollectionCompanions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digital_collection_atm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collection_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_collection_atm_digital_collections_entity_id",
                        column: x => x.entity_id,
                        principalTable: "digital_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_collection_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collection_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_collection_aud_digital_collections_digital_collecti",
                        column: x => x.digital_collection_id,
                        principalTable: "digital_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_collection_item_atm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collection_item_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_collection_item_atm_digital_collection_items_entity",
                        column: x => x.entity_id,
                        principalTable: "digital_collection_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_collection_item_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_collection_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collection_item_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_collection_item_aud_digital_collection_items_digita",
                        column: x => x.digital_collection_item_id,
                        principalTable: "digital_collection_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_collection_item_lck",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    released_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lock_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collection_item_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_collection_item_lck_digital_collection_items_entity",
                        column: x => x.entity_id,
                        principalTable: "digital_collection_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digital_collection_item_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "digital_collection_lck",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    released_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lock_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collection_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_collection_lck_digital_collections_entity_id",
                        column: x => x.entity_id,
                        principalTable: "digital_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digital_collection_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_atm_entity_id",
                table: "digital_collection_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_aud_digital_collection_id_performed_at_u",
                table: "digital_collection_aud",
                columns: new[] { "digital_collection_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_item_atm_entity_id",
                table: "digital_collection_item_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_item_aud_digital_collection_item_id_perf",
                table: "digital_collection_item_aud",
                columns: new[] { "digital_collection_item_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_item_lck_entity_id_released_at_utc",
                table: "digital_collection_item_lck",
                columns: new[] { "entity_id", "released_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_item_lck_locked_by_id",
                table: "digital_collection_item_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_lck_entity_id_released_at_utc",
                table: "digital_collection_lck",
                columns: new[] { "entity_id", "released_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_collection_lck_locked_by_id",
                table: "digital_collection_lck",
                column: "locked_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digital_collection_atm");

            migrationBuilder.DropTable(
                name: "digital_collection_aud");

            migrationBuilder.DropTable(
                name: "digital_collection_item_atm");

            migrationBuilder.DropTable(
                name: "digital_collection_item_aud");

            migrationBuilder.DropTable(
                name: "digital_collection_item_lck");

            migrationBuilder.DropTable(
                name: "digital_collection_lck");
        }
    }
}
