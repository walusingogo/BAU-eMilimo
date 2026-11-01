using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261016120000_AddFrbAssessmentSelection")]
public partial class AddFrbAssessmentSelection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "assessment_rule_ids_json",
            table: "financial_record_books",
            type: "jsonb",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "assessment_rule_ids_json",
            table: "financial_record_books");
    }
}
