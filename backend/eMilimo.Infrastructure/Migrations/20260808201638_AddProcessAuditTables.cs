using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contribution_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contribution_aud", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "requisition_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requisition_aud", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contribution_aud_contribution_id_performed_at_utc",
                table: "contribution_aud",
                columns: new[] { "contribution_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_requisition_aud_requisition_id_performed_at_utc",
                table: "requisition_aud",
                columns: new[] { "requisition_id", "performed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contribution_aud");

            migrationBuilder.DropTable(
                name: "requisition_aud");
        }
    }
}
