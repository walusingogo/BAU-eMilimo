using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationDeliveryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipient = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    protected_message = table.Column<string>(type: "text", nullable: true),
                    retry_of_communication_log_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_communication_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_communication_logs_channel_status_sent_at_utc",
                table: "communication_logs",
                columns: new[] { "channel", "status", "sent_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_communication_logs_retry_of_communication_log_id",
                table: "communication_logs",
                column: "retry_of_communication_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_communication_logs_sent_at_utc",
                table: "communication_logs",
                column: "sent_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communication_logs");
        }
    }
}
