using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "current_session_id",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_session_id",
                table: "users");
        }
    }
}
