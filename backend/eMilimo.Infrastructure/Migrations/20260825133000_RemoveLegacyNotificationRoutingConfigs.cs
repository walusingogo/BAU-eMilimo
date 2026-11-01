using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825133000_RemoveLegacyNotificationRoutingConfigs")]
public sealed class RemoveLegacyNotificationRoutingConfigs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM system_configs
            WHERE lower(config_key) IN (
                'notification_asset_request_cc_roles',
                'notification_asset_request_to_roles',
                'notification_contribution_cc_roles',
                'notification_contribution_to_roles',
                'notification_requisition_cc_roles',
                'notification_requisition_to_roles'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // These legacy overrides intentionally are not restored. Notification
        // recipients are derived from process permissions and workflow state.
    }
}
