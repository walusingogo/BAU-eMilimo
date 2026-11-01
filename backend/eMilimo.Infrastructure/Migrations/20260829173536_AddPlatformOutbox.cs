using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829173536_AddPlatformOutbox")]
public partial class AddPlatformOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "platform");
        migrationBuilder.CreateTable(
            name: "outbox_messages", schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                message_type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                headers_json = table.Column<string>(type: "jsonb", nullable: true),
                correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                available_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                last_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                last_failure = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                lock_token = table.Column<Guid>(type: "uuid", nullable: true),
                locked_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_outbox_messages", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_outbox_messages_correlation_id", schema: "platform", table: "outbox_messages", column: "correlation_id");
        migrationBuilder.CreateIndex(name: "ix_outbox_messages_locked_until_utc_available_at_utc", schema: "platform", table: "outbox_messages", columns: new[] { "locked_until_utc", "available_at_utc" });
        migrationBuilder.CreateIndex(name: "ix_outbox_messages_processed_at_utc_available_at_utc", schema: "platform", table: "outbox_messages", columns: new[] { "processed_at_utc", "available_at_utc" });
        migrationBuilder.CreateIndex(name: "ix_outbox_messages_tenant_id_idempotency_key", schema: "platform", table: "outbox_messages", columns: new[] { "tenant_id", "idempotency_key" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "outbox_messages", schema: "platform");
}
