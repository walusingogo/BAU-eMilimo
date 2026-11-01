using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260809093000_SeparateAssetCheckoutAttachments")]
public partial class SeparateAssetCheckoutAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "asset_checkouts_atms",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                attachment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                file_name = table.Column<string>(type: "text", nullable: false),
                file_path = table.Column<string>(type: "text", nullable: false),
                mime_type = table.Column<string>(type: "text", nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
                uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asset_checkouts_atms", x => x.id);
                table.ForeignKey("fk_asset_checkouts_atms_asset_checkouts_entity_id", x => x.entity_id,
                    "asset_checkouts", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_asset_checkouts_atms_entity_id", "asset_checkouts_atms", "entity_id");
        migrationBuilder.Sql("""
            INSERT INTO asset_checkouts_atms
                (id, attachment_type, entity_id, file_name, file_path, mime_type, size, uploaded_by_id, uploaded_at,
                 description, created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
            SELECT id, attachment_type, asset_checkout_id, file_name, file_path, mime_type, size, uploaded_by_id, uploaded_at,
                   description, created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version
            FROM asset_atms WHERE asset_checkout_id IS NOT NULL;
            DELETE FROM asset_atms WHERE asset_checkout_id IS NOT NULL;
            """);
        migrationBuilder.DropForeignKey("fk_asset_atms_asset_checkouts_asset_checkout_id", "asset_atms");
        migrationBuilder.DropIndex("ix_asset_atms_asset_checkout_id", "asset_atms");
        migrationBuilder.DropColumn("asset_checkout_id", "asset_atms");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "asset_checkout_id", table: "asset_atms", type: "uuid", nullable: true);
        migrationBuilder.Sql("""
            INSERT INTO asset_atms
                (id, asset_checkout_id, attachment_type, entity_id, file_name, file_path, mime_type, size, uploaded_by_id,
                 uploaded_at, description, created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
            SELECT a.id, a.entity_id, a.attachment_type, c.asset_id, a.file_name, a.file_path, a.mime_type, a.size, a.uploaded_by_id,
                   a.uploaded_at, a.description, a.created_date, a.updated_date, a.last_processed_time_stamp, a.created_by_id, a.updated_by_id, a.version
            FROM asset_checkouts_atms a JOIN asset_checkouts c ON c.id = a.entity_id;
            """);
        migrationBuilder.CreateIndex("ix_asset_atms_asset_checkout_id", "asset_atms", "asset_checkout_id");
        migrationBuilder.AddForeignKey("fk_asset_atms_asset_checkouts_asset_checkout_id", "asset_atms", "asset_checkout_id",
            "asset_checkouts", principalColumn: "id", onDelete: ReferentialAction.Cascade);
        migrationBuilder.DropTable("asset_checkouts_atms");
    }
}
