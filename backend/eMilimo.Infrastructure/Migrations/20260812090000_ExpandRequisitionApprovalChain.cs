using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260812090000_ExpandRequisitionApprovalChain")]
public partial class ExpandRequisitionApprovalChain : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("treasurer_approver_id", "requisitions", "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("accountant_approver_id", "requisitions", "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("church_authority_approver_id", "requisitions", "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("minister_approver_id", "requisitions", "uuid", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("treasurer_approver_id", "requisitions");
        migrationBuilder.DropColumn("accountant_approver_id", "requisitions");
        migrationBuilder.DropColumn("church_authority_approver_id", "requisitions");
        migrationBuilder.DropColumn("minister_approver_id", "requisitions");
    }
}
