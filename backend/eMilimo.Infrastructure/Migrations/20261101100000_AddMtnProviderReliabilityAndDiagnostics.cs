using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMtnProviderReliabilityAndDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_provider_configurations_provider_key",
                table: "payment_provider_configurations");

            migrationBuilder.AddColumn<Guid>(
                name: "digital_payment_transaction_id",
                table: "payment_webhook_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "configuration_version",
                table: "payment_provider_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "is_validated",
                table: "payment_provider_configurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_validated_at_utc",
                table: "payment_provider_configurations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "posting_key",
                table: "fund_transactions",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_reconciliation_at_utc",
                table: "digital_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_reconciliation_at_utc",
                table: "digital_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "provider_configuration_version",
                table: "digital_payment_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "provider_environment_snapshot",
                table: "digital_payment_transactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SANDBOX");

            migrationBuilder.AddColumn<int>(
                name: "reconciliation_attempt_count",
                table: "digital_payment_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "reconciliation_status",
                table: "digital_payment_transactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NONE");

            migrationBuilder.CreateTable(
                name: "payment_api_exchanges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_provider_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    environment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    transaction_reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    sanitized_request_headers = table.Column<string>(type: "jsonb", nullable: false),
                    sanitized_request_payload = table.Column<string>(type: "jsonb", nullable: true),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    sanitized_response_headers = table.Column<string>(type: "jsonb", nullable: false),
                    sanitized_response_payload = table.Column<string>(type: "jsonb", nullable: true),
                    duration_milliseconds = table.Column<long>(type: "bigint", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    error_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_api_exchanges", x => x.id);
                    table.CheckConstraint("ck_payment_api_exchange_duration", "duration_milliseconds >= 0 AND attempt_number > 0");
                    table.ForeignKey(
                        name: "fk_payment_api_exchanges_digital_payment_transactions_digital_",
                        column: x => x.digital_payment_transaction_id,
                        principalTable: "digital_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_api_exchanges_payment_provider_configurations_payme",
                        column: x => x.payment_provider_configuration_id,
                        principalTable: "payment_provider_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_provider_configuration_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_provider_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    protected_configuration_snapshot = table.Column<string>(type: "text", nullable: false),
                    sanitized_configuration_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    changed_fields_json = table.Column<string>(type: "jsonb", nullable: false),
                    validation_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    validation_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    validated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_provider_configuration_versions", x => x.id);
                    table.CheckConstraint("ck_provider_configuration_version_positive", "configuration_version > 0");
                    table.ForeignKey(
                        name: "fk_payment_provider_configuration_versions_payment_provider_co",
                        column: x => x.payment_provider_configuration_id,
                        principalTable: "payment_provider_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE digital_payment_transactions AS payment
                SET provider_configuration_version = provider.configuration_version,
                    provider_environment_snapshot = provider.environment
                FROM payment_provider_configurations AS provider
                WHERE payment.payment_provider_configuration_id = provider.id;

                UPDATE digital_payment_transactions
                SET reconciliation_status = 'SCHEDULED',
                    next_reconciliation_at_utc = NOW()
                WHERE provider_transaction_id IS NOT NULL
                  AND status IN ('PENDING', 'PROCESSING', 'UNKNOWN');

                INSERT INTO payment_provider_configuration_versions
                    (id, payment_provider_configuration_id, configuration_version,
                     protected_configuration_snapshot, sanitized_configuration_snapshot,
                     changed_fields_json, validation_status, validation_message,
                     created_date, updated_date, created_by_id, updated_by_id, version)
                SELECT gen_random_uuid(), id, configuration_version,
                       protected_configuration, '{}'::jsonb,
                       '["MIGRATED_EXISTING_CONFIGURATION"]'::jsonb, 'NOT_TESTED',
                       'Existing provider configuration captured during migration.',
                       NOW(), NOW(), created_by_id, updated_by_id, 1
                FROM payment_provider_configurations;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_digital_payment_transaction_id",
                table: "payment_webhook_events",
                column: "digital_payment_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_configurations_provider_key_environment",
                table: "payment_provider_configurations",
                columns: new[] { "provider_key", "environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_posting_key",
                table: "fund_transactions",
                column: "posting_key",
                unique: true,
                filter: "posting_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_reconciliation_status_next_rec",
                table: "digital_payment_transactions",
                columns: new[] { "reconciliation_status", "next_reconciliation_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_api_exchanges_digital_payment_transaction_id_reques",
                table: "payment_api_exchanges",
                columns: new[] { "digital_payment_transaction_id", "requested_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_api_exchanges_environment_requested_at_utc",
                table: "payment_api_exchanges",
                columns: new[] { "environment", "requested_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_api_exchanges_http_status_code_requested_at_utc",
                table: "payment_api_exchanges",
                columns: new[] { "http_status_code", "requested_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_api_exchanges_payment_provider_configuration_id_req",
                table: "payment_api_exchanges",
                columns: new[] { "payment_provider_configuration_id", "requested_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_api_exchanges_transaction_reference",
                table: "payment_api_exchanges",
                column: "transaction_reference");

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_configuration_versions_payment_provider_co",
                table: "payment_provider_configuration_versions",
                columns: new[] { "payment_provider_configuration_id", "configuration_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_configuration_versions_payment_provider_co1",
                table: "payment_provider_configuration_versions",
                columns: new[] { "payment_provider_configuration_id", "created_date" });

            migrationBuilder.AddForeignKey(
                name: "fk_payment_webhook_events_digital_payment_transactions_digital",
                table: "payment_webhook_events",
                column: "digital_payment_transaction_id",
                principalTable: "digital_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_webhook_events_digital_payment_transactions_digital",
                table: "payment_webhook_events");

            migrationBuilder.DropTable(
                name: "payment_api_exchanges");

            migrationBuilder.DropTable(
                name: "payment_provider_configuration_versions");

            migrationBuilder.DropIndex(
                name: "ix_payment_webhook_events_digital_payment_transaction_id",
                table: "payment_webhook_events");

            migrationBuilder.DropIndex(
                name: "ix_payment_provider_configurations_provider_key_environment",
                table: "payment_provider_configurations");

            migrationBuilder.DropIndex(
                name: "ix_fund_transactions_posting_key",
                table: "fund_transactions");

            migrationBuilder.DropIndex(
                name: "ix_digital_payment_transactions_reconciliation_status_next_rec",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "digital_payment_transaction_id",
                table: "payment_webhook_events");

            migrationBuilder.DropColumn(
                name: "configuration_version",
                table: "payment_provider_configurations");

            migrationBuilder.DropColumn(
                name: "is_validated",
                table: "payment_provider_configurations");

            migrationBuilder.DropColumn(
                name: "last_validated_at_utc",
                table: "payment_provider_configurations");

            migrationBuilder.DropColumn(
                name: "posting_key",
                table: "fund_transactions");

            migrationBuilder.DropColumn(
                name: "last_reconciliation_at_utc",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "next_reconciliation_at_utc",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_configuration_version",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_environment_snapshot",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "reconciliation_attempt_count",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "reconciliation_status",
                table: "digital_payment_transactions");

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_configurations_provider_key",
                table: "payment_provider_configurations",
                column: "provider_key",
                unique: true);
        }
    }
}
