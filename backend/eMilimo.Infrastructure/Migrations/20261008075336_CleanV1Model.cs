using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanV1Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_designation_assignments");

            migrationBuilder.DropColumn(
                name: "beneficiary",
                table: "frb_assessment_snapshots");

            migrationBuilder.DropColumn(
                name: "fixed_amount",
                table: "frb_assessment_snapshots");

            migrationBuilder.DropColumn(
                name: "rate",
                table: "frb_assessment_snapshots");

            migrationBuilder.DropColumn(
                name: "beneficiary",
                table: "frb_assessment_rules");

            migrationBuilder.DropColumn(
                name: "fixed_amount",
                table: "frb_assessment_rules");

            migrationBuilder.DropColumn(
                name: "rate",
                table: "frb_assessment_rules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_designation_assignments",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_designation_assignments", x => new { x.user_id, x.designation_id });
                    table.ForeignKey("fk_user_designation_assignments_designations_designation_id", x => x.designation_id,
                        "designations", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("fk_user_designation_assignments_users_assigned_by_id", x => x.assigned_by_id,
                        "users", "id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("fk_user_designation_assignments_users_user_id", x => x.user_id,
                        "users", "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_designation_assignments_assigned_by_id",
                table: "user_designation_assignments",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_designation_assignments_designation_id_effective_to_utc",
                table: "user_designation_assignments",
                columns: new[] { "designation_id", "effective_to_utc" });

            migrationBuilder.AddColumn<string>(
                name: "beneficiary",
                table: "frb_assessment_snapshots",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "fixed_amount",
                table: "frb_assessment_snapshots",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate",
                table: "frb_assessment_snapshots",
                type: "numeric(9,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "beneficiary",
                table: "frb_assessment_rules",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "fixed_amount",
                table: "frb_assessment_rules",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate",
                table: "frb_assessment_rules",
                type: "numeric(9,4)",
                nullable: true);
        }
    }
}
