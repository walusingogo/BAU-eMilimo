using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

/// <summary>
/// Establishes the current EF model snapshot after the recovered Phase A-D
/// migrations. The schema changes themselves are owned by the preceding,
/// explicitly ordered migrations; this marker intentionally changes no data.
/// </summary>
public partial class SyncDigitalGivingAndOrganizationModelSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
