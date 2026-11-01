using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignServiceCollectionCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "children_attendance",
                table: "service_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "officiating_minister",
                table: "service_events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "teacher_attendance",
                table: "service_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_service_collection_column",
                table: "contribution_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Give existing installations useful worksheet defaults without
            // coupling service names to contribution-purpose names.
            migrationBuilder.Sql("""
                UPDATE contribution_types
                SET is_default_service_collection_column = TRUE
                WHERE upper(code) IN ('GENERAL', 'GENERAL_OFFERING', 'BUILDING', 'BUILDING_OFFERING');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "children_attendance",
                table: "service_events");

            migrationBuilder.DropColumn(
                name: "officiating_minister",
                table: "service_events");

            migrationBuilder.DropColumn(
                name: "teacher_attendance",
                table: "service_events");

            migrationBuilder.DropColumn(
                name: "is_default_service_collection_column",
                table: "contribution_types");
        }
    }
}
