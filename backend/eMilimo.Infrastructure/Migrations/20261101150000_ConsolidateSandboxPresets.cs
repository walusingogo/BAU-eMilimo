using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateSandboxPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sample_presets_json",
                table: "mtn_sandbox_scenarios",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "sample_preset_snapshot_json",
                table: "mtn_sandbox_test_executions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE mtn_sandbox_scenarios s
                SET sample_presets_json = COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'Id', p.id, 'Name', p.name, 'ContributionTypeId', p.contribution_type_id,
                        'ContributionTypeName', p.contribution_type_name, 'Amount', p.amount,
                        'Currency', p.currency, 'TestMsisdn', p.test_msisdn,
                        'ExternalReferenceFormat', p.external_reference_format,
                        'PayerMessage', p.payer_message, 'PayeeNote', p.payee_note,
                        'MtnSandboxScenarioId', p.mtn_sandbox_scenario_id,
                        'IsActive', p.is_active, 'IsSystemDefault', p.is_system_default,
                        'ConfigurationVersion', p.configuration_version,
                        'CreatedDate', p.created_date, 'UpdatedDate', p.updated_date) ORDER BY p.name)
                    FROM mtn_sandbox_sample_transactions p
                    WHERE p.mtn_sandbox_scenario_id = s.id), '[]'::jsonb);

                UPDATE mtn_sandbox_test_executions e
                SET sample_preset_snapshot_json = jsonb_build_object(
                        'Id', p.id, 'Name', p.name, 'ContributionTypeId', p.contribution_type_id,
                        'ContributionTypeName', p.contribution_type_name, 'Amount', p.amount,
                        'Currency', p.currency, 'TestMsisdn', p.test_msisdn,
                        'ExternalReferenceFormat', p.external_reference_format,
                        'PayerMessage', p.payer_message, 'PayeeNote', p.payee_note,
                        'MtnSandboxScenarioId', p.mtn_sandbox_scenario_id,
                        'IsActive', p.is_active, 'IsSystemDefault', p.is_system_default,
                        'ConfigurationVersion', p.configuration_version,
                        'CreatedDate', p.created_date, 'UpdatedDate', p.updated_date)
                FROM mtn_sandbox_sample_transactions p
                WHERE e.mtn_sandbox_sample_transaction_id = p.id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_mtn_sandbox_test_executions_mtn_sandbox_sample_transactions",
                table: "mtn_sandbox_test_executions");

            migrationBuilder.DropTable(
                name: "mtn_sandbox_sample_transactions");

            migrationBuilder.DropIndex(
                name: "ix_mtn_sandbox_test_executions_mtn_sandbox_sample_transaction_",
                table: "mtn_sandbox_test_executions");

            migrationBuilder.RenameColumn(
                name: "mtn_sandbox_sample_transaction_id",
                table: "mtn_sandbox_test_executions",
                newName: "sample_preset_id");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sample_preset_id",
                table: "mtn_sandbox_test_executions",
                newName: "mtn_sandbox_sample_transaction_id");

            migrationBuilder.CreateTable(
                name: "mtn_sandbox_sample_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mtn_sandbox_scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    contribution_type_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    external_reference_format = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_default = table.Column<bool>(type: "boolean", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payee_note = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    payer_message = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    test_msisdn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "ix_mtn_sandbox_test_executions_mtn_sandbox_sample_transaction_",
                table: "mtn_sandbox_test_executions",
                column: "mtn_sandbox_sample_transaction_id");

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

            migrationBuilder.Sql(
                """
                INSERT INTO mtn_sandbox_sample_transactions
                    (id, contribution_type_id, mtn_sandbox_scenario_id, amount, configuration_version,
                     contribution_type_name, created_by_id, created_date, currency, external_reference_format,
                     is_active, is_system_default, last_processed_time_stamp, name, payee_note, payer_message,
                     test_msisdn, updated_by_id, updated_date, version)
                SELECT (p->>'Id')::uuid, NULLIF(p->>'ContributionTypeId', '')::uuid,
                       (p->>'MtnSandboxScenarioId')::uuid, (p->>'Amount')::numeric,
                       (p->>'ConfigurationVersion')::integer, p->>'ContributionTypeName',
                       NULL, (p->>'CreatedDate')::timestamptz, p->>'Currency', p->>'ExternalReferenceFormat',
                       (p->>'IsActive')::boolean, (p->>'IsSystemDefault')::boolean, NULL,
                       p->>'Name', p->>'PayeeNote', p->>'PayerMessage', p->>'TestMsisdn',
                       NULL, (p->>'UpdatedDate')::timestamptz, 1
                FROM mtn_sandbox_scenarios s
                CROSS JOIN LATERAL jsonb_array_elements(s.sample_presets_json) p;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_mtn_sandbox_test_executions_mtn_sandbox_sample_transactions",
                table: "mtn_sandbox_test_executions",
                column: "mtn_sandbox_sample_transaction_id",
                principalTable: "mtn_sandbox_sample_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(
                name: "sample_preset_snapshot_json",
                table: "mtn_sandbox_test_executions");

            migrationBuilder.DropColumn(
                name: "sample_presets_json",
                table: "mtn_sandbox_scenarios");
        }
    }
}
