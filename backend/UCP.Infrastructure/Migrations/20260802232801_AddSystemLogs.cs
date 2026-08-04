using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    process = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    duration_milliseconds = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    membership_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    exception = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_logs_process_timestamp_utc",
                table: "logs",
                columns: new[] { "process", "timestamp_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_logs_timestamp_utc",
                table: "logs",
                column: "timestamp_utc");

            migrationBuilder.CreateIndex(
                name: "ix_logs_user_id",
                table: "logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logs");
        }
    }
}
