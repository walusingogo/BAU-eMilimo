using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequisitionAndUserLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "requisition_lcks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lock_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requisition_lcks", x => x.id);
                    table.ForeignKey(
                        name: "fk_requisition_lcks_requisitions_requisition_id",
                        column: x => x.requisition_id,
                        principalTable: "requisitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requisition_lcks_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_lcks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lock_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_lcks", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_lcks_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_lcks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_requisition_lcks_entity_id",
                table: "requisition_lcks",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_requisition_lcks_locked_by_id",
                table: "requisition_lcks",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisition_lcks_requisition_id",
                table: "requisition_lcks",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lcks_entity_id",
                table: "user_lcks",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_lcks_locked_by_id",
                table: "user_lcks",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lcks_user_id",
                table: "user_lcks",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "requisition_lcks");

            migrationBuilder.DropTable(
                name: "user_lcks");
        }
    }
}
