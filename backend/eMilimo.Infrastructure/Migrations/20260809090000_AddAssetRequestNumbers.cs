using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260809090000_AddAssetRequestNumbers")]
public partial class AddAssetRequestNumbers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "request_number",
            table: "asset_checkouts",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE asset_checkouts
            SET request_number = 'ARQ-' || TO_CHAR(created_date AT TIME ZONE 'UTC', 'YYYYMMDD') || '-' || UPPER(SUBSTRING(REPLACE(id::text, '-', '') FROM 1 FOR 8))
            WHERE request_number IS NULL OR request_number = '';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "request_number",
            table: "asset_checkouts",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(40)",
            oldMaxLength: 40,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_asset_checkouts_request_number",
            table: "asset_checkouts",
            column: "request_number",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_asset_checkouts_request_number", table: "asset_checkouts");
        migrationBuilder.DropColumn(name: "request_number", table: "asset_checkouts");
    }
}
