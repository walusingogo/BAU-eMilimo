using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryableEmailPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cc_emails",
                table: "email_logs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "html_body",
                table: "email_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "retry_of_email_log_id",
                table: "email_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_retry_of_email_log_id",
                table: "email_logs",
                column: "retry_of_email_log_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_logs_retry_of_email_log_id",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "cc_emails",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "html_body",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "retry_of_email_log_id",
                table: "email_logs");
        }
    }
}
