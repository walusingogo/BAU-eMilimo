using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateGroupsSectionsCommittees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "fund_balances",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "committee_id",
                table: "fund_balances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "fund_balances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "committees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    committee_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_committees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_group_assignments",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_group_assignments", x => new { x.user_id, x.group_id });
                    table.ForeignKey(
                        name: "fk_user_group_assignments_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_group_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_committee_assignments",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_committee_assignments", x => new { x.user_id, x.committee_id });
                    table.ForeignKey(
                        name: "fk_user_committee_assignments_committees_committee_id",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_committee_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO committees (id, committee_name, description, is_active, created_date, updated_date,
                    last_processed_time_stamp, created_by_id, updated_by_id, version)
                SELECT id, group_name, description, is_active, created_date, updated_date,
                    last_processed_time_stamp, created_by_id, updated_by_id, version
                FROM groups g
                WHERE g.group_type = 'Committee'
                  AND NOT EXISTS (
                      SELECT 1 FROM committees c
                      WHERE c.id = g.id OR LOWER(c.committee_name) = LOWER(g.group_name)
                  )
                ON CONFLICT DO NOTHING;

                INSERT INTO sections (id, section_number, section_name, description, is_active, created_date,
                    updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
                SELECT g.id, (COALESCE((SELECT MAX(section_number) FROM sections), 0) + ROW_NUMBER() OVER (ORDER BY g.group_name))::integer,
                    g.group_name, g.description, g.is_active, g.created_date, g.updated_date,
                    g.last_processed_time_stamp, g.created_by_id, g.updated_by_id, g.version
                FROM groups g WHERE g.group_type = 'Section'
                  AND NOT EXISTS (SELECT 1 FROM sections s WHERE LOWER(s.section_name) = LOWER(g.group_name));

                INSERT INTO user_group_assignments (user_id, group_id, assigned_date)
                SELECT u.id, u.group_id, NOW() FROM users u JOIN groups g ON g.id = u.group_id
                WHERE g.group_type = 'ChurchGroup' ON CONFLICT DO NOTHING;

                INSERT INTO user_committee_assignments (user_id, committee_id, assigned_date)
                SELECT u.id, u.group_id, NOW() FROM users u JOIN groups g ON g.id = u.group_id
                WHERE g.group_type = 'Committee' ON CONFLICT DO NOTHING;

                UPDATE users SET group_id = NULL
                WHERE group_id IN (SELECT id FROM groups WHERE group_type IN ('Section', 'Committee'));
                UPDATE groups SET group_type = 'LegacySection', is_active = FALSE WHERE group_type = 'Section';
                UPDATE groups SET group_type = 'LegacyCommittee', is_active = FALSE WHERE group_type = 'Committee';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_fund_balances_committee_id_fiscal_year",
                table: "fund_balances",
                columns: new[] { "committee_id", "fiscal_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fund_balances_section_id_fiscal_year",
                table: "fund_balances",
                columns: new[] { "section_id", "fiscal_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_committees_committee_name",
                table: "committees",
                column: "committee_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_committee_assignments_committee_id",
                table: "user_committee_assignments",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_group_assignments_group_id",
                table: "user_group_assignments",
                column: "group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fund_balances_committees_committee_id",
                table: "fund_balances",
                column: "committee_id",
                principalTable: "committees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_balances_sections_section_id",
                table: "fund_balances",
                column: "section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE groups SET group_type = 'Section' WHERE group_type = 'LegacySection';
                UPDATE groups SET group_type = 'Committee' WHERE group_type = 'LegacyCommittee';
                """);
            migrationBuilder.DropForeignKey(
                name: "fk_fund_balances_committees_committee_id",
                table: "fund_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_fund_balances_sections_section_id",
                table: "fund_balances");

            migrationBuilder.DropTable(
                name: "user_committee_assignments");

            migrationBuilder.DropTable(
                name: "user_group_assignments");

            migrationBuilder.DropTable(
                name: "committees");

            migrationBuilder.DropIndex(
                name: "ix_fund_balances_committee_id_fiscal_year",
                table: "fund_balances");

            migrationBuilder.DropIndex(
                name: "ix_fund_balances_section_id_fiscal_year",
                table: "fund_balances");

            migrationBuilder.DropColumn(
                name: "committee_id",
                table: "fund_balances");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "fund_balances");

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "fund_balances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
