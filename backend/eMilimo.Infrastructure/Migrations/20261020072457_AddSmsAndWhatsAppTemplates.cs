using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsAndWhatsAppTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_sms_enabled",
                table: "email_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_whats_app_enabled",
                table: "email_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sms_body",
                table: "email_templates",
                type: "text",
                maxLength: 1600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "whats_app_body",
                table: "email_templates",
                type: "text",
                maxLength: 4096,
                nullable: false,
                defaultValue: "");

            // Preserve companion notifications for existing customised email
            // templates. Credential-related templates intentionally remain
            // email-only; all ordinary workflow templates receive safe defaults.
            migrationBuilder.Sql("""
                UPDATE email_templates
                SET sms_body = CASE template_key
                    WHEN 'account-activated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been activated. You can now sign in.'
                    WHEN 'account-deactivated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been deactivated. Contact your administrator if this is unexpected.'
                    WHEN 'requisition-status' THEN 'Requisition {{reference}} is now {{status}}. {{note}}'
                    WHEN 'contribution-status' THEN 'Contribution {{reference}} is now {{status}}. {{note}}'
                    WHEN 'asset-request-status' THEN 'Asset request {{reference}} for {{quantity}} x {{asset_name}} is now {{status}}. {{note}}'
                    WHEN 'approval-required' THEN E'Approval required\n\n{{process_name}}: {{reference}}\n{{summary}}\n\nAvailable actions: {{actions}}\n\nOpen request:\n{{approval_url}}'
                    ELSE '' END,
                    whats_app_body = CASE template_key
                    WHEN 'account-activated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been activated. You can now sign in.'
                    WHEN 'account-deactivated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been deactivated. Contact your administrator if this is unexpected.'
                    WHEN 'requisition-status' THEN 'Requisition {{reference}} is now {{status}}. {{note}}'
                    WHEN 'contribution-status' THEN 'Contribution {{reference}} is now {{status}}. {{note}}'
                    WHEN 'asset-request-status' THEN 'Asset request {{reference}} for {{quantity}} x {{asset_name}} is now {{status}}. {{note}}'
                    WHEN 'approval-required' THEN E'Approval required\n\n{{process_name}}: {{reference}}\n{{summary}}\n\nAvailable actions: {{actions}}\n\nOpen request:\n{{approval_url}}'
                    ELSE '' END,
                    is_sms_enabled = template_key NOT IN ('password-reset', 'temporary-password'),
                    is_whats_app_enabled = template_key NOT IN ('password-reset', 'temporary-password');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_sms_enabled",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "is_whats_app_enabled",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "sms_body",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "whats_app_body",
                table: "email_templates");
        }
    }
}
