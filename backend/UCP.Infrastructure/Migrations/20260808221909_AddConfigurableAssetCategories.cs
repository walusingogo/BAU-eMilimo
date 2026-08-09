using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableAssetCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "asset_category_id",
                table: "assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "asset_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_asset_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assets_asset_category_id",
                table: "assets",
                column: "asset_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_code",
                table: "asset_categories",
                column: "code",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO asset_categories
                    (id, code, name, description, is_active, created_date, updated_date, version)
                VALUES
                    ('10f202dd-4d92-4da2-9164-e6fce9316001', 'INVENTORY', 'Inventory', 'Counted items such as chairs and tents.', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1),
                    ('10f202dd-4d92-4da2-9164-e6fce9316002', 'VEHICLE', 'Vehicle', 'Fleet assets such as buses and utility vehicles.', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1),
                    ('10f202dd-4d92-4da2-9164-e6fce9316003', 'EQUIPMENT', 'Equipment', 'Reusable church equipment and machinery.', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO asset_categories
                    (id, code, name, is_active, created_date, updated_date, version)
                SELECT gen_random_uuid(), normalized.code, initcap(replace(lower(normalized.code), '_', ' ')), TRUE,
                       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1
                FROM (
                    SELECT DISTINCT upper(trim(a.category)) AS code
                    FROM assets a
                    WHERE a.category IS NOT NULL AND trim(a.category) <> ''
                ) normalized
                WHERE NOT EXISTS (SELECT 1 FROM asset_categories c WHERE c.code = normalized.code);

                UPDATE assets a
                SET asset_category_id = c.id
                FROM asset_categories c
                WHERE c.code = upper(a.category) AND a.asset_category_id IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_assets_asset_categories_asset_category_id",
                table: "assets",
                column: "asset_category_id",
                principalTable: "asset_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assets_asset_categories_asset_category_id",
                table: "assets");

            migrationBuilder.DropTable(
                name: "asset_categories");

            migrationBuilder.DropIndex(
                name: "ix_assets_asset_category_id",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "asset_category_id",
                table: "assets");
        }
    }
}
