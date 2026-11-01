using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014203000_AddServiceCollectionSignaturesAndPosProof")]
public partial class AddServiceCollectionSignaturesAndPosProof : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "signature_data",
            table: "service_collection_participants",
            type: "text",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE payment_method_definitions
            SET requires_attachment = TRUE,
                updated_date = CURRENT_TIMESTAMP,
                version = version + 1
            WHERE code = 'POS_CARD' AND requires_attachment = FALSE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "signature_data", table: "service_collection_participants");
    }
}
