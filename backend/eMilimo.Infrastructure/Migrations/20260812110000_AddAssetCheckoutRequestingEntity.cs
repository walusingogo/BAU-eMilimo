using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260812110000_AddAssetCheckoutRequestingEntity")]
public partial class AddAssetCheckoutRequestingEntity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name:"group_id",table:"asset_checkouts",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"section_id",table:"asset_checkouts",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"committee_id",table:"asset_checkouts",type:"uuid",nullable:true);
        migrationBuilder.CreateIndex(name:"ix_asset_checkouts_group_id",table:"asset_checkouts",column:"group_id");
        migrationBuilder.CreateIndex(name:"ix_asset_checkouts_section_id",table:"asset_checkouts",column:"section_id");
        migrationBuilder.CreateIndex(name:"ix_asset_checkouts_committee_id",table:"asset_checkouts",column:"committee_id");
        migrationBuilder.AddForeignKey(name:"fk_asset_checkouts_groups_group_id",table:"asset_checkouts",column:"group_id",principalTable:"groups",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name:"fk_asset_checkouts_sections_section_id",table:"asset_checkouts",column:"section_id",principalTable:"sections",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name:"fk_asset_checkouts_committees_committee_id",table:"asset_checkouts",column:"committee_id",principalTable:"committees",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        migrationBuilder.AddCheckConstraint(name:"CK_AssetCheckout_OneEntity",table:"asset_checkouts",sql:"(CASE WHEN group_id IS NULL THEN 0 ELSE 1 END + CASE WHEN section_id IS NULL THEN 0 ELSE 1 END + CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END) <= 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name:"fk_asset_checkouts_groups_group_id",table:"asset_checkouts");
        migrationBuilder.DropForeignKey(name:"fk_asset_checkouts_sections_section_id",table:"asset_checkouts");
        migrationBuilder.DropForeignKey(name:"fk_asset_checkouts_committees_committee_id",table:"asset_checkouts");
        migrationBuilder.DropCheckConstraint(name:"CK_AssetCheckout_OneEntity",table:"asset_checkouts");
        migrationBuilder.DropColumn(name:"group_id",table:"asset_checkouts");
        migrationBuilder.DropColumn(name:"section_id",table:"asset_checkouts");
        migrationBuilder.DropColumn(name:"committee_id",table:"asset_checkouts");
    }
}
