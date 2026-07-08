using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSectionIdFromGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_sections_section_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_section_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_section_id",
                table: "groups",
                column: "section_id");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_sections_section_id",
                table: "groups",
                column: "section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
