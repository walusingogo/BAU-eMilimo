using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationEntitiesInFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "committee_id",
                table: "requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "fund_transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "committee_id",
                table: "fund_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "fund_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "contribution_allocations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "committee_id",
                table: "contribution_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "contribution_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_committee_id",
                table: "requisitions",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_section_id",
                table: "requisitions",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_committee_id",
                table: "fund_transactions",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_section_id",
                table: "fund_transactions",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_allocations_committee_id",
                table: "contribution_allocations",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_allocations_section_id",
                table: "contribution_allocations",
                column: "section_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_allocations_committees_committee_id",
                table: "contribution_allocations",
                column: "committee_id",
                principalTable: "committees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_allocations_sections_section_id",
                table: "contribution_allocations",
                column: "section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_transactions_committees_committee_id",
                table: "fund_transactions",
                column: "committee_id",
                principalTable: "committees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_transactions_sections_section_id",
                table: "fund_transactions",
                column: "section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_requisitions_committees_committee_id",
                table: "requisitions",
                column: "committee_id",
                principalTable: "committees",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_requisitions_sections_section_id",
                table: "requisitions",
                column: "section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contribution_allocations_committees_committee_id",
                table: "contribution_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_contribution_allocations_sections_section_id",
                table: "contribution_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_fund_transactions_committees_committee_id",
                table: "fund_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_fund_transactions_sections_section_id",
                table: "fund_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_requisitions_committees_committee_id",
                table: "requisitions");

            migrationBuilder.DropForeignKey(
                name: "fk_requisitions_sections_section_id",
                table: "requisitions");

            migrationBuilder.DropIndex(
                name: "ix_requisitions_committee_id",
                table: "requisitions");

            migrationBuilder.DropIndex(
                name: "ix_requisitions_section_id",
                table: "requisitions");

            migrationBuilder.DropIndex(
                name: "ix_fund_transactions_committee_id",
                table: "fund_transactions");

            migrationBuilder.DropIndex(
                name: "ix_fund_transactions_section_id",
                table: "fund_transactions");

            migrationBuilder.DropIndex(
                name: "ix_contribution_allocations_committee_id",
                table: "contribution_allocations");

            migrationBuilder.DropIndex(
                name: "ix_contribution_allocations_section_id",
                table: "contribution_allocations");

            migrationBuilder.DropColumn(
                name: "committee_id",
                table: "requisitions");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "requisitions");

            migrationBuilder.DropColumn(
                name: "committee_id",
                table: "fund_transactions");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "fund_transactions");

            migrationBuilder.DropColumn(
                name: "committee_id",
                table: "contribution_allocations");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "contribution_allocations");

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "fund_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "contribution_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
