using System;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving")]
    /// <inheritdoc />
    public partial class ExtendContributionTypeAndAllocationForDigitalGiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to contribution_types
            migrationBuilder.AddColumn<bool>(
                name: "is_enabled_for_digital_giving",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "default_digital_destination_type",
                table: "contribution_types",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "require_destination_selection_for_digital_giving",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "digital_destination_eligibility_policy",
                table: "contribution_types",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AS_CONFIGURED");

            migrationBuilder.AddColumn<bool>(
                name: "allow_organization_wide_allocation",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_section_allocation",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_group_allocation",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_committee_allocation",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_amount",
                table: "contribution_types",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_amount",
                table: "contribution_types",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "public_display_order",
                table: "contribution_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create contribution_type_destination_policies table
            migrationBuilder.CreateTable(
                name: "contribution_type_destination_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contribution_type_destination_policies", x => x.id);
                    table.CheckConstraint("ck_destination_policy_single_destination", 
                        "(CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + " +
                        "(CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + " +
                        "(CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END) <= 1");
                    table.ForeignKey(
                        name: "fk_contribution_type_destination_policies_contribution_types",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contribution_type_destination_policies_sections",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contribution_type_destination_policies_groups",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contribution_type_destination_policies_committees",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create indexes for contribution_type_destination_policies
            migrationBuilder.CreateIndex(
                name: "ix_contribution_type_destination_policies_contribution_type_id",
                table: "contribution_type_destination_policies",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_type_destination_policies_section_id",
                table: "contribution_type_destination_policies",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_type_destination_policies_group_id",
                table: "contribution_type_destination_policies",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_type_destination_policies_committee_id",
                table: "contribution_type_destination_policies",
                column: "committee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contribution_type_destination_policies");

            migrationBuilder.DropColumn(
                name: "is_enabled_for_digital_giving",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "default_digital_destination_type",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "require_destination_selection_for_digital_giving",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "digital_destination_eligibility_policy",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "allow_organization_wide_allocation",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "allow_section_allocation",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "allow_group_allocation",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "allow_committee_allocation",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "minimum_amount",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "maximum_amount",
                table: "contribution_types");

            migrationBuilder.DropColumn(
                name: "public_display_order",
                table: "contribution_types");
        }
    }
}
