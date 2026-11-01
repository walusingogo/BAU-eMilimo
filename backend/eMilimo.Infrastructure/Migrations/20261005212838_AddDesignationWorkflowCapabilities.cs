using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignationWorkflowCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "capabilities_json",
                table: "designations",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE designations SET capabilities_json = '["FRB_VERIFY_STEWARDSHIP","FRB_ORGANIZATION_SCOPE"]' WHERE code = 'STEWARDSHIP_CONVENER';
                UPDATE designations SET capabilities_json = '["REQUISITION_ENTITY_REVIEW"]' WHERE code IN ('ELDER', 'STEWARD', 'SUNDAY_SCHOOL_TEACHER');
                UPDATE designations SET capabilities_json = '["REQUISITION_MINISTER_APPROVAL","FRB_VERIFY_MINISTER","FRB_ORGANIZATION_SCOPE"]' WHERE code = 'MINISTER';
                UPDATE designations SET capabilities_json = '["REQUISITION_TREASURER_APPROVAL","CONTRIBUTION_REVIEW","FRB_MANAGE","FRB_VERIFY_TREASURER","FRB_FINALIZE","FRB_ORGANIZATION_SCOPE"]' WHERE code = 'TREASURER';
                UPDATE designations SET capabilities_json = '["REQUISITION_ACCOUNTANT_APPROVAL","CONTRIBUTION_REVIEW","FRB_MANAGE"]' WHERE code = 'ACCOUNTANT';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capabilities_json",
                table: "designations");
        }
    }
}
