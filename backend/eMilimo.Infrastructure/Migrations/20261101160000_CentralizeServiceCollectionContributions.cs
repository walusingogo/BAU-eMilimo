using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CentralizeServiceCollectionContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "contribution_id",
                table: "service_collection_entry_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE service_collection_entry_lines AS line
                SET contribution_id = contribution.id
                FROM contributions AS contribution
                WHERE contribution.service_collection_entry_line_id = line.id;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM contributions
                        WHERE service_collection_entry_line_id IS NOT NULL
                        GROUP BY service_collection_entry_line_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot centralize Service Collection contributions because duplicate line links exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_entry_lines_contribution_id",
                table: "service_collection_entry_lines",
                column: "contribution_id",
                unique: true,
                filter: "contribution_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_service_collection_entry_lines_contributions_contribution_id",
                table: "service_collection_entry_lines",
                column: "contribution_id",
                principalTable: "contributions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "fk_contributions_service_collection_entry_lines_service_collec",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contributions_service_collection_entry_line_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "service_collection_entry_line_id",
                table: "contributions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "service_collection_entry_line_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE contributions AS contribution
                SET service_collection_entry_line_id = line.id
                FROM service_collection_entry_lines AS line
                WHERE line.contribution_id = contribution.id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_contributions_service_collection_entry_line_id",
                table: "contributions",
                column: "service_collection_entry_line_id",
                unique: true,
                filter: "service_collection_entry_line_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_service_collection_entry_lines_service_collec",
                table: "contributions",
                column: "service_collection_entry_line_id",
                principalTable: "service_collection_entry_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "fk_service_collection_entry_lines_contributions_contribution_id",
                table: "service_collection_entry_lines");

            migrationBuilder.DropIndex(
                name: "ix_service_collection_entry_lines_contribution_id",
                table: "service_collection_entry_lines");

            migrationBuilder.DropColumn(
                name: "contribution_id",
                table: "service_collection_entry_lines");
        }
    }
}
