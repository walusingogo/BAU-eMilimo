using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMtnSandboxTestingCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "mtn_sandbox_test_execution_id",
                table: "payment_api_exchanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mtn_sandbox_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    msisdn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expected_transaction_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expected_http_response_code = table.Column<int>(type: "integer", nullable: true),
                    expected_mtn_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    test_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    external_reference_prefix = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payer_message = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    payee_note = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    expected_processing_behaviour = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    maximum_polling_attempts = table.Column<int>(type: "integer", nullable: false),
                    polling_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    source_classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system_default = table.Column<bool>(type: "boolean", nullable: false),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mtn_sandbox_scenarios", x => x.id);
                    table.CheckConstraint("ck_mtn_sandbox_scenario_amount", "test_amount > 0");
                    table.CheckConstraint("ck_mtn_sandbox_scenario_configuration_version", "configuration_version > 0");
                    table.CheckConstraint("ck_mtn_sandbox_scenario_polling", "maximum_polling_attempts > 0 AND polling_interval_seconds > 0");
                });

            migrationBuilder.CreateTable(
                name: "mtn_sandbox_sample_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contribution_type_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    test_msisdn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_reference_format = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payer_message = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    payee_note = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    mtn_sandbox_scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_default = table.Column<bool>(type: "boolean", nullable: false),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mtn_sandbox_sample_transactions", x => x.id);
                    table.CheckConstraint("ck_mtn_sandbox_sample_amount", "amount > 0");
                    table.CheckConstraint("ck_mtn_sandbox_sample_configuration_version", "configuration_version > 0");
                    table.ForeignKey(
                        name: "fk_mtn_sandbox_sample_transactions_contribution_types_contribu",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_mtn_sandbox_sample_transactions_mtn_sandbox_scenarios_mtn_s",
                        column: x => x.mtn_sandbox_scenario_id,
                        principalTable: "mtn_sandbox_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mtn_sandbox_test_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_provider_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mtn_sandbox_scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mtn_sandbox_sample_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    external_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    test_msisdn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    expected_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expected_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expected_http_status_code = table.Column<int>(type: "integer", nullable: true),
                    actual_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actual_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actual_http_status_code = table.Column<int>(type: "integer", nullable: true),
                    verdict = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    execution_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_configuration_version = table.Column<int>(type: "integer", nullable: false),
                    polling_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    maximum_polling_attempts = table.Column<int>(type: "integer", nullable: false),
                    polling_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    next_poll_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sanitized_request_preview = table.Column<string>(type: "jsonb", nullable: false),
                    timeline_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mtn_sandbox_test_executions", x => x.id);
                    table.CheckConstraint("ck_mtn_sandbox_execution_amount", "amount > 0");
                    table.CheckConstraint("ck_mtn_sandbox_execution_polling", "polling_attempt_count >= 0 AND maximum_polling_attempts > 0 AND polling_interval_seconds > 0");
                    table.ForeignKey(
                        name: "fk_mtn_sandbox_test_executions_mtn_sandbox_sample_transactions",
                        column: x => x.mtn_sandbox_sample_transaction_id,
                        principalTable: "mtn_sandbox_sample_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_mtn_sandbox_test_executions_mtn_sandbox_scenarios_mtn_sandb",
                        column: x => x.mtn_sandbox_scenario_id,
                        principalTable: "mtn_sandbox_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mtn_sandbox_test_executions_payment_provider_configurations",
                        column: x => x.payment_provider_configuration_id,
                        principalTable: "payment_provider_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_api_exchanges_mtn_sandbox_test_execution_id",
                table: "payment_api_exchanges",
                column: "mtn_sandbox_test_execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_sample_transactions_contribution_type_id",
                table: "mtn_sandbox_sample_transactions",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_sample_transactions_mtn_sandbox_scenario_id_is_",
                table: "mtn_sandbox_sample_transactions",
                columns: new[] { "mtn_sandbox_scenario_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_sample_transactions_name",
                table: "mtn_sandbox_sample_transactions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_scenarios_is_active_name",
                table: "mtn_sandbox_scenarios",
                columns: new[] { "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_scenarios_scenario_code",
                table: "mtn_sandbox_scenarios",
                column: "scenario_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_execution_status_next_poll_at_u",
                table: "mtn_sandbox_test_executions",
                columns: new[] { "execution_status", "next_poll_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_external_reference",
                table: "mtn_sandbox_test_executions",
                column: "external_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_mtn_sandbox_sample_transaction_",
                table: "mtn_sandbox_test_executions",
                column: "mtn_sandbox_sample_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_mtn_sandbox_scenario_id_started",
                table: "mtn_sandbox_test_executions",
                columns: new[] { "mtn_sandbox_scenario_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_payment_provider_configuration_",
                table: "mtn_sandbox_test_executions",
                columns: new[] { "payment_provider_configuration_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_transaction_uuid",
                table: "mtn_sandbox_test_executions",
                column: "transaction_uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_api_exchanges_mtn_sandbox_test_executions_mtn_sandb",
                table: "payment_api_exchanges",
                column: "mtn_sandbox_test_execution_id",
                principalTable: "mtn_sandbox_test_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_api_exchanges_mtn_sandbox_test_executions_mtn_sandb",
                table: "payment_api_exchanges");

            migrationBuilder.DropTable(
                name: "mtn_sandbox_test_executions");

            migrationBuilder.DropTable(
                name: "mtn_sandbox_sample_transactions");

            migrationBuilder.DropTable(
                name: "mtn_sandbox_scenarios");

            migrationBuilder.DropIndex(
                name: "ix_payment_api_exchanges_mtn_sandbox_test_execution_id",
                table: "payment_api_exchanges");

            migrationBuilder.DropColumn(
                name: "mtn_sandbox_test_execution_id",
                table: "payment_api_exchanges");
        }
    }
}
