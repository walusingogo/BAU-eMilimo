using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetHireRevenueAndFcyProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE fcy_management_settings
                SET rate_source_mode = 'PROVIDER_WITH_MANUAL_FALLBACK',
                    provider_code = 'ALL_RATES_TODAY_BOZ',
                    updated_date = NOW(),
                    version = version + 1
                WHERE rate_source_mode = 'MANUAL_ONLY' AND provider_code IS NULL;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "asset_checkout_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_contributions_asset_checkout_id",
                table: "contributions",
                column: "asset_checkout_id",
                unique: true,
                filter: "asset_checkout_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_asset_checkouts_asset_checkout_id",
                table: "contributions",
                column: "asset_checkout_id",
                principalTable: "asset_checkouts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE fcy_management_settings
                SET rate_source_mode = 'MANUAL_ONLY', provider_code = NULL, updated_date = NOW(), version = version + 1
                WHERE provider_code = 'ALL_RATES_TODAY_BOZ';
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_contributions_asset_checkouts_asset_checkout_id",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contributions_asset_checkout_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "asset_checkout_id",
                table: "contributions");
        }
    }
}
