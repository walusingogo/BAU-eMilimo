using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetsInventoryFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO role_permissions
                    (id, role, permissions_json, created_date, updated_date, version)
                SELECT gen_random_uuid(), substring(config_key from 17), config_value,
                       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1
                FROM system_configs
                WHERE config_key LIKE 'RolePermissions:%'
                ON CONFLICT (role) DO UPDATE
                    SET permissions_json = EXCLUDED.permissions_json,
                        updated_date = CURRENT_TIMESTAMP,
                        version = role_permissions.version + 1;

                DELETE FROM system_configs WHERE config_key LIKE 'RolePermissions:%';
                """);

            migrationBuilder.CreateTable(
                name: "asset_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_change = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_movements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity_total = table.Column<int>(type: "integer", nullable: false),
                    quantity_available = table.Column<int>(type: "integer", nullable: false),
                    registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_checkouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    expected_out_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_return_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checked_out_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    returned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_note = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_checkouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_checkouts_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_checkouts_asset_id_status",
                table: "asset_checkouts",
                columns: new[] { "asset_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_movements_asset_id_performed_at_utc",
                table: "asset_movements",
                columns: new[] { "asset_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_asset_number",
                table: "assets",
                column: "asset_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_checkouts");

            migrationBuilder.DropTable(
                name: "asset_movements");

            migrationBuilder.DropTable(
                name: "assets");
        }
    }
}
