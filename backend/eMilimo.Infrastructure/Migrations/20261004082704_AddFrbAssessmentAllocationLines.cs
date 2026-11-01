using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFrbAssessmentAllocationLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "beneficiary",
                table: "frb_assessment_rules",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.CreateTable(
                name: "frb_assessment_allocation_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    frb_assessment_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficiary = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    is_local_retained = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frb_assessment_allocation_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_frb_assessment_allocation_lines_frb_assessment_rules_frb_as",
                        column: x => x.frb_assessment_rule_id,
                        principalTable: "frb_assessment_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "frb_assessment_allocation_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    frb_assessment_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_allocation_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    beneficiary = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    calculated_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_local_retained = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frb_assessment_allocation_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_frb_assessment_allocation_snapshots_frb_assessment_snapshot",
                        column: x => x.frb_assessment_snapshot_id,
                        principalTable: "frb_assessment_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO frb_assessment_allocation_lines
                    (id, frb_assessment_rule_id, beneficiary, rate, fixed_amount, is_local_retained,
                     display_order, notes, created_date, updated_date, created_by_id, updated_by_id, version)
                SELECT gen_random_uuid(), id, beneficiary, rate, fixed_amount, FALSE,
                       0, notes, created_date, updated_date, created_by_id, updated_by_id, 1
                FROM frb_assessment_rules
                WHERE beneficiary IS NOT NULL AND btrim(beneficiary) <> '';

                INSERT INTO frb_assessment_allocation_snapshots
                    (id, frb_assessment_snapshot_id, beneficiary, rate, fixed_amount, calculated_amount,
                     is_local_retained, display_order, created_date, updated_date, created_by_id, updated_by_id, version)
                SELECT gen_random_uuid(), id, beneficiary, rate, fixed_amount, calculated_amount,
                       FALSE, 0, created_date, updated_date, created_by_id, updated_by_id, 1
                FROM frb_assessment_snapshots
                WHERE beneficiary IS NOT NULL AND btrim(beneficiary) <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_frb_assessment_allocation_lines_frb_assessment_rule_id_bene",
                table: "frb_assessment_allocation_lines",
                columns: new[] { "frb_assessment_rule_id", "beneficiary" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_frb_assessment_allocation_snapshots_frb_assessment_snapshot",
                table: "frb_assessment_allocation_snapshots",
                columns: new[] { "frb_assessment_snapshot_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "frb_assessment_allocation_lines");

            migrationBuilder.DropTable(
                name: "frb_assessment_allocation_snapshots");

            migrationBuilder.AlterColumn<string>(
                name: "beneficiary",
                table: "frb_assessment_rules",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

        }
    }
}
