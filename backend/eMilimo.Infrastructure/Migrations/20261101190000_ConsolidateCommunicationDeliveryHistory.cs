using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateCommunicationDeliveryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                table: "communication_logs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "protected_cc_recipients",
                table: "communication_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "communication_logs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "triggered_by_user_id",
                table: "communication_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO communication_logs
                    (id, sent_at_utc, channel, provider, recipient, action, subject, status,
                     provider_message_id, failure_reason, protected_message, protected_cc_recipients,
                     retry_of_communication_log_id, triggered_by_user_id)
                SELECT
                    id, sent_at_utc, 'Email', 'SMTP', to_email, category, subject, status,
                    NULL, failure_reason, html_body, cc_emails, retry_of_email_log_id, triggered_by_user_id
                FROM email_logs;
                """);

            migrationBuilder.DropTable(
                name: "email_logs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cc_emails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    retry_of_email_log_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    to_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_retry_of_email_log_id",
                table: "email_logs",
                column: "retry_of_email_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_sent_at_utc",
                table: "email_logs",
                column: "sent_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_status",
                table: "email_logs",
                column: "status");

            migrationBuilder.Sql("""
                INSERT INTO email_logs
                    (id, sent_at_utc, to_email, subject, category, status, failure_reason,
                     html_body, cc_emails, retry_of_email_log_id, triggered_by_user_id)
                SELECT
                    id, sent_at_utc, recipient, COALESCE(subject, ''), action, status, failure_reason,
                    protected_message, protected_cc_recipients, retry_of_communication_log_id, triggered_by_user_id
                FROM communication_logs
                WHERE channel = 'Email';

                DELETE FROM communication_logs WHERE channel = 'Email';
                """);

            migrationBuilder.DropColumn(
                name: "protected_cc_recipients",
                table: "communication_logs");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "communication_logs");

            migrationBuilder.DropColumn(
                name: "triggered_by_user_id",
                table: "communication_logs");

            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                table: "communication_logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
