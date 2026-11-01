using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetCompanionRecordsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "payment_amount",
                table: "asset_checkouts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "payment_honored",
                table: "asset_checkouts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_honored_at_utc",
                table: "asset_checkouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "payment_required",
                table: "asset_checkouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_verified_by_id",
                table: "asset_checkouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "asset_atms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_checkout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attachment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("pk_asset_atms", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_atms_asset_checkouts_asset_checkout_id",
                        column: x => x.asset_checkout_id,
                        principalTable: "asset_checkouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asset_atms_assets_entity_id",
                        column: x => x.entity_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_checkout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    record_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_aud", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_lcks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_asset_lcks", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_lcks_assets_entity_id",
                        column: x => x.entity_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asset_lcks_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_atms_asset_checkout_id",
                table: "asset_atms",
                column: "asset_checkout_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_atms_entity_id",
                table: "asset_atms",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_aud_asset_id_performed_at_utc",
                table: "asset_aud",
                columns: new[] { "asset_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_lcks_entity_id",
                table: "asset_lcks",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_lcks_locked_by_id",
                table: "asset_lcks",
                column: "locked_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_atms");

            migrationBuilder.DropTable(
                name: "asset_aud");

            migrationBuilder.DropTable(
                name: "asset_lcks");

            migrationBuilder.DropColumn(
                name: "payment_amount",
                table: "asset_checkouts");

            migrationBuilder.DropColumn(
                name: "payment_honored",
                table: "asset_checkouts");

            migrationBuilder.DropColumn(
                name: "payment_honored_at_utc",
                table: "asset_checkouts");

            migrationBuilder.DropColumn(
                name: "payment_required",
                table: "asset_checkouts");

            migrationBuilder.DropColumn(
                name: "payment_verified_by_id",
                table: "asset_checkouts");
        }
    }
}
