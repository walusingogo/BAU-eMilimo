using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateIntegrationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_provider_configuration_versions_payment_provider_co",
                table: "payment_provider_configuration_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_digital_payment_transactions_payment_provider_configuration",
                table: "digital_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_mtn_sandbox_test_executions_payment_provider_configurations",
                table: "mtn_sandbox_test_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_api_exchanges_digital_payment_transactions_digital_",
                table: "payment_api_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_api_exchanges_mtn_sandbox_test_executions_mtn_sandb",
                table: "payment_api_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_api_exchanges_payment_provider_configurations_payme",
                table: "payment_api_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_provider_routes_payment_provider_configurations_pay",
                table: "payment_provider_routes");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_webhook_events_digital_payment_transactions_digital",
                table: "payment_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_webhook_events_payment_provider_configurations_paym",
                table: "payment_webhook_events");

            migrationBuilder.RenameTable(
                name: "payment_provider_configuration_versions",
                newName: "integration_configuration_aud");

            migrationBuilder.RenameColumn(
                name: "payment_provider_configuration_id",
                table: "integration_configuration_aud",
                newName: "integration_configuration_id");

            migrationBuilder.RenameIndex(
                name: "ix_payment_provider_configuration_versions_payment_provider_co",
                table: "integration_configuration_aud",
                newName: "ix_integration_configuration_aud_integration_configuration_id_");

            migrationBuilder.RenameIndex(
                name: "ix_payment_provider_configuration_versions_payment_provider_co1",
                table: "integration_configuration_aud",
                newName: "ix_integration_configuration_aud_integration_configuration_id_1");

            migrationBuilder.AddColumn<string>(
                name: "action",
                table: "integration_configuration_aud",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "UPDATED");

            migrationBuilder.Sql(
                """
                ALTER TABLE integration_configuration_aud
                    RENAME CONSTRAINT pk_payment_provider_configuration_versions TO pk_integration_configuration_aud;
                """);

            migrationBuilder.DropPrimaryKey(
                name: "pk_payment_webhook_events",
                table: "payment_webhook_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_payment_provider_configurations",
                table: "payment_provider_configurations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_payment_api_exchanges",
                table: "payment_api_exchanges");

            migrationBuilder.RenameTable(
                name: "payment_webhook_events",
                newName: "integration_webhook_events");

            migrationBuilder.RenameTable(
                name: "payment_provider_configurations",
                newName: "integration_configurations");

            migrationBuilder.RenameTable(
                name: "payment_api_exchanges",
                newName: "integration_exchanges");

            migrationBuilder.RenameIndex(
                name: "ix_payment_webhook_events_processing_status_received_at_utc",
                table: "integration_webhook_events",
                newName: "ix_integration_webhook_events_processing_status_received_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_payment_webhook_events_payment_provider_configuration_id_ev",
                table: "integration_webhook_events",
                newName: "ix_integration_webhook_events_payment_provider_configuration_i");

            migrationBuilder.RenameIndex(
                name: "ix_payment_webhook_events_payload_hash",
                table: "integration_webhook_events",
                newName: "ix_integration_webhook_events_payload_hash");

            migrationBuilder.RenameIndex(
                name: "ix_payment_webhook_events_digital_payment_transaction_id",
                table: "integration_webhook_events",
                newName: "ix_integration_webhook_events_digital_payment_transaction_id");

            migrationBuilder.RenameIndex(
                name: "ix_payment_provider_configurations_provider_key_environment",
                table: "integration_configurations",
                newName: "ix_integration_configurations_provider_key_environment");

            migrationBuilder.RenameIndex(
                name: "ix_payment_api_exchanges_transaction_reference",
                table: "integration_exchanges",
                newName: "ix_integration_exchanges_transaction_reference");

            migrationBuilder.RenameIndex(
                name: "ix_payment_api_exchanges_payment_provider_configuration_id_req",
                table: "integration_exchanges",
                newName: "ix_integration_exchanges_payment_provider_configuration_id_req");

            migrationBuilder.RenameIndex(
                name: "ix_payment_api_exchanges_mtn_sandbox_test_execution_id",
                table: "integration_exchanges",
                newName: "ix_integration_exchanges_mtn_sandbox_test_execution_id");

            migrationBuilder.RenameIndex(
                name: "ix_payment_api_exchanges_http_status_code_requested_at_utc",
                table: "integration_exchanges",
                newName: "ix_integration_exchanges_http_status_code_requested_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_payment_api_exchanges_environment_requested_at_utc",
                table: "integration_exchanges",
                newName: "ix_integration_exchanges_environment_requested_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_payment_api_exchanges_digital_payment_transaction_id_reques",
                table: "integration_exchanges",
                newName: "ix_integration_exchanges_digital_payment_transaction_id_reques");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_webhook_events",
                table: "integration_webhook_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_configurations",
                table: "integration_configurations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_exchanges",
                table: "integration_exchanges",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_integration_configuration_aud_integration_configurations",
                table: "integration_configuration_aud",
                column: "integration_configuration_id",
                principalTable: "integration_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_digital_payment_transactions_integration_configurations_pay",
                table: "digital_payment_transactions",
                column: "payment_provider_configuration_id",
                principalTable: "integration_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_exchanges_digital_payment_transactions_digital_",
                table: "integration_exchanges",
                column: "digital_payment_transaction_id",
                principalTable: "digital_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_exchanges_integration_configurations_payment_pr",
                table: "integration_exchanges",
                column: "payment_provider_configuration_id",
                principalTable: "integration_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_exchanges_mtn_sandbox_test_executions_mtn_sandb",
                table: "integration_exchanges",
                column: "mtn_sandbox_test_execution_id",
                principalTable: "mtn_sandbox_test_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_webhook_events_digital_payment_transactions_dig",
                table: "integration_webhook_events",
                column: "digital_payment_transaction_id",
                principalTable: "digital_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_webhook_events_integration_configurations_payme",
                table: "integration_webhook_events",
                column: "payment_provider_configuration_id",
                principalTable: "integration_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_mtn_sandbox_test_executions_integration_configurations_paym",
                table: "mtn_sandbox_test_executions",
                column: "payment_provider_configuration_id",
                principalTable: "integration_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_provider_routes_integration_configurations_payment_",
                table: "payment_provider_routes",
                column: "payment_provider_configuration_id",
                principalTable: "integration_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_integration_configuration_aud_integration_configurations",
                table: "integration_configuration_aud");

            migrationBuilder.DropForeignKey(
                name: "fk_digital_payment_transactions_integration_configurations_pay",
                table: "digital_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_exchanges_digital_payment_transactions_digital_",
                table: "integration_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_exchanges_integration_configurations_payment_pr",
                table: "integration_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_exchanges_mtn_sandbox_test_executions_mtn_sandb",
                table: "integration_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_webhook_events_digital_payment_transactions_dig",
                table: "integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_webhook_events_integration_configurations_payme",
                table: "integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_mtn_sandbox_test_executions_integration_configurations_paym",
                table: "mtn_sandbox_test_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_provider_routes_integration_configurations_payment_",
                table: "payment_provider_routes");

            migrationBuilder.DropColumn(name: "action", table: "integration_configuration_aud");

            migrationBuilder.Sql(
                """
                ALTER TABLE integration_configuration_aud
                    RENAME CONSTRAINT pk_integration_configuration_aud TO pk_payment_provider_configuration_versions;
                """);

            migrationBuilder.RenameIndex(
                name: "ix_integration_configuration_aud_integration_configuration_id_",
                table: "integration_configuration_aud",
                newName: "ix_payment_provider_configuration_versions_payment_provider_co");

            migrationBuilder.RenameIndex(
                name: "ix_integration_configuration_aud_integration_configuration_id_1",
                table: "integration_configuration_aud",
                newName: "ix_payment_provider_configuration_versions_payment_provider_co1");

            migrationBuilder.RenameColumn(
                name: "integration_configuration_id",
                table: "integration_configuration_aud",
                newName: "payment_provider_configuration_id");

            migrationBuilder.RenameTable(
                name: "integration_configuration_aud",
                newName: "payment_provider_configuration_versions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_webhook_events",
                table: "integration_webhook_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_exchanges",
                table: "integration_exchanges");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_configurations",
                table: "integration_configurations");

            migrationBuilder.RenameTable(
                name: "integration_webhook_events",
                newName: "payment_webhook_events");

            migrationBuilder.RenameTable(
                name: "integration_exchanges",
                newName: "payment_api_exchanges");

            migrationBuilder.RenameTable(
                name: "integration_configurations",
                newName: "payment_provider_configurations");

            migrationBuilder.RenameIndex(
                name: "ix_integration_webhook_events_processing_status_received_at_utc",
                table: "payment_webhook_events",
                newName: "ix_payment_webhook_events_processing_status_received_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_integration_webhook_events_payment_provider_configuration_i",
                table: "payment_webhook_events",
                newName: "ix_payment_webhook_events_payment_provider_configuration_id_ev");

            migrationBuilder.RenameIndex(
                name: "ix_integration_webhook_events_payload_hash",
                table: "payment_webhook_events",
                newName: "ix_payment_webhook_events_payload_hash");

            migrationBuilder.RenameIndex(
                name: "ix_integration_webhook_events_digital_payment_transaction_id",
                table: "payment_webhook_events",
                newName: "ix_payment_webhook_events_digital_payment_transaction_id");

            migrationBuilder.RenameIndex(
                name: "ix_integration_exchanges_transaction_reference",
                table: "payment_api_exchanges",
                newName: "ix_payment_api_exchanges_transaction_reference");

            migrationBuilder.RenameIndex(
                name: "ix_integration_exchanges_payment_provider_configuration_id_req",
                table: "payment_api_exchanges",
                newName: "ix_payment_api_exchanges_payment_provider_configuration_id_req");

            migrationBuilder.RenameIndex(
                name: "ix_integration_exchanges_mtn_sandbox_test_execution_id",
                table: "payment_api_exchanges",
                newName: "ix_payment_api_exchanges_mtn_sandbox_test_execution_id");

            migrationBuilder.RenameIndex(
                name: "ix_integration_exchanges_http_status_code_requested_at_utc",
                table: "payment_api_exchanges",
                newName: "ix_payment_api_exchanges_http_status_code_requested_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_integration_exchanges_environment_requested_at_utc",
                table: "payment_api_exchanges",
                newName: "ix_payment_api_exchanges_environment_requested_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_integration_exchanges_digital_payment_transaction_id_reques",
                table: "payment_api_exchanges",
                newName: "ix_payment_api_exchanges_digital_payment_transaction_id_reques");

            migrationBuilder.RenameIndex(
                name: "ix_integration_configurations_provider_key_environment",
                table: "payment_provider_configurations",
                newName: "ix_payment_provider_configurations_provider_key_environment");

            migrationBuilder.AddPrimaryKey(
                name: "pk_payment_webhook_events",
                table: "payment_webhook_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_payment_api_exchanges",
                table: "payment_api_exchanges",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_payment_provider_configurations",
                table: "payment_provider_configurations",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_provider_configuration_versions_payment_provider_co",
                table: "payment_provider_configuration_versions",
                column: "payment_provider_configuration_id",
                principalTable: "payment_provider_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_digital_payment_transactions_payment_provider_configuration",
                table: "digital_payment_transactions",
                column: "payment_provider_configuration_id",
                principalTable: "payment_provider_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_mtn_sandbox_test_executions_payment_provider_configurations",
                table: "mtn_sandbox_test_executions",
                column: "payment_provider_configuration_id",
                principalTable: "payment_provider_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_api_exchanges_digital_payment_transactions_digital_",
                table: "payment_api_exchanges",
                column: "digital_payment_transaction_id",
                principalTable: "digital_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_api_exchanges_mtn_sandbox_test_executions_mtn_sandb",
                table: "payment_api_exchanges",
                column: "mtn_sandbox_test_execution_id",
                principalTable: "mtn_sandbox_test_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_api_exchanges_payment_provider_configurations_payme",
                table: "payment_api_exchanges",
                column: "payment_provider_configuration_id",
                principalTable: "payment_provider_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_provider_routes_payment_provider_configurations_pay",
                table: "payment_provider_routes",
                column: "payment_provider_configuration_id",
                principalTable: "payment_provider_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_webhook_events_digital_payment_transactions_digital",
                table: "payment_webhook_events",
                column: "digital_payment_transaction_id",
                principalTable: "digital_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_webhook_events_payment_provider_configurations_paym",
                table: "payment_webhook_events",
                column: "payment_provider_configuration_id",
                principalTable: "payment_provider_configurations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
