using System;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261030140000_AddOrganizationOverviewEntities")]
    /// <inheritdoc />
    public partial class AddOrganizationOverviewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create organization_overviews table
            migrationBuilder.CreateTable(
                name: "organization_overviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_organization_wide_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_section_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_group_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_committee_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    active_section_count = table.Column<int>(type: "integer", nullable: false),
                    active_group_count = table.Column<int>(type: "integer", nullable: false),
                    active_committee_count = table.Column<int>(type: "integer", nullable: false),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_overviews", x => x.id);
                });

            // Create organization_contribution_type_summaries table
            migrationBuilder.CreateTable(
                name: "organization_contribution_type_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_overview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    contribution_count = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_contribution_type_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_contribution_type_summaries_contribution_types",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_contribution_type_summaries_organization_overviews",
                        column: x => x.organization_overview_id,
                        principalTable: "organization_overviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create section_financial_summaries table
            migrationBuilder.CreateTable(
                name: "section_financial_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direct_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    group_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    committee_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    active_group_count = table.Column<int>(type: "integer", nullable: false),
                    active_committee_count = table.Column<int>(type: "integer", nullable: false),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_section_financial_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_section_financial_summaries_sections",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create group_financial_summaries table
            migrationBuilder.CreateTable(
                name: "group_financial_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direct_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    committee_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    active_committee_count = table.Column<int>(type: "integer", nullable: false),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_financial_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_financial_summaries_groups",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create committee_financial_summaries table
            migrationBuilder.CreateTable(
                name: "committee_financial_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    contribution_count = table.Column<int>(type: "integer", nullable: false),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_committee_financial_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_committee_financial_summaries_committees",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_organization_contribution_type_summaries_contribution_type_id",
                table: "organization_contribution_type_summaries",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_contribution_type_summaries_organization_overview_id",
                table: "organization_contribution_type_summaries",
                column: "organization_overview_id");

            migrationBuilder.CreateIndex(
                name: "ix_section_financial_summaries_section_id",
                table: "section_financial_summaries",
                column: "section_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_financial_summaries_group_id",
                table: "group_financial_summaries",
                column: "group_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_committee_financial_summaries_committee_id",
                table: "committee_financial_summaries",
                column: "committee_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_contribution_type_summaries");

            migrationBuilder.DropTable(
                name: "section_financial_summaries");

            migrationBuilder.DropTable(
                name: "group_financial_summaries");

            migrationBuilder.DropTable(
                name: "committee_financial_summaries");

            migrationBuilder.DropTable(
                name: "organization_overviews");
        }
    }
}
