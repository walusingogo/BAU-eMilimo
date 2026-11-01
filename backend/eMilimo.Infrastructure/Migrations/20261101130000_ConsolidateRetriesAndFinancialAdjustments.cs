using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateRetriesAndFinancialAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sanitized_details_json",
                table: "digital_payment_events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            // Preserve retry/recovery evidence in the existing immutable payment event stream.
            migrationBuilder.Sql(
                """
                INSERT INTO digital_payment_events
                    (id, digital_payment_transaction_id, event_type, previous_status, new_status,
                     source, correlation_id, protected_details, sanitized_details_json, occurred_at_utc,
                     created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
                SELECT r.id, r.digital_payment_transaction_id, 'RECOVERY_ATTEMPT', r.previous_status,
                       COALESCE(r.result_status, r.previous_status), r.retry_type,
                       'retry:' || r.attempt_number::text, NULL,
                       jsonb_build_object(
                           'reason', r.reason,
                           'attemptNumber', r.attempt_number,
                           'resultStatus', r.result_status,
                           'resultMessage', r.result_message,
                           'initiatedById', r.initiated_by_id),
                       COALESCE(r.attempted_at_utc, r.created_date), r.created_date, r.updated_date,
                       r.last_processed_time_stamp, r.created_by_id, r.updated_by_id, r.version
                FROM digital_payment_retries r
                WHERE NOT EXISTS (SELECT 1 FROM digital_payment_events e WHERE e.id = r.id);
                """);

            migrationBuilder.DropTable(name: "digital_payment_retries");

            // Provider refunds are a different lifecycle. Refuse to silently discard any real refund evidence.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM digital_payment_reversals
                        WHERE provider_refund_initiated = TRUE
                           OR provider_refund_transaction_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot consolidate reversals: provider refund evidence exists and must first be migrated to a payment refund workflow.';
                    END IF;
                END $$;
                """);

            migrationBuilder.RenameTable(name: "digital_payment_reversals", newName: "financial_adjustments");
            migrationBuilder.RenameColumn(name: "related_reversal_id", table: "financial_adjustments", newName: "related_adjustment_id");
            migrationBuilder.RenameColumn(name: "reversal_status", table: "financial_adjustments", newName: "status");
            migrationBuilder.RenameColumn(name: "reversed_at_utc", table: "financial_adjustments", newName: "completed_at_utc");
            migrationBuilder.RenameColumn(name: "reversed_by_id", table: "financial_adjustments", newName: "completed_by_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE financial_adjustments RENAME CONSTRAINT pk_digital_payment_reversals TO pk_financial_adjustments;
                ALTER INDEX IF EXISTS ix_digital_payment_reversals_digital_payment_transaction_id
                    RENAME TO ix_financial_adjustments_digital_payment_transaction_id;
                ALTER INDEX IF EXISTS ix_digital_payment_reversals_related_reversal_id
                    RENAME TO ix_financial_adjustments_related_adjustment_id;
                ALTER INDEX IF EXISTS ix_digital_payment_reversals_reversed_by_id
                    RENAME TO ix_financial_adjustments_completed_by_id;
                """);

            migrationBuilder.AddColumn<string>(
                name: "adjustment_type", table: "financial_adjustments",
                type: "character varying(40)", maxLength: 40, nullable: false,
                defaultValue: "ACCOUNTING_REVERSAL");

            migrationBuilder.DropColumn(name: "provider_refund_initiated", table: "financial_adjustments");
            migrationBuilder.DropColumn(name: "provider_refund_transaction_id", table: "financial_adjustments");

            migrationBuilder.Sql(
                """
                ALTER TABLE financial_adjustments
                    DROP CONSTRAINT IF EXISTS fk_digital_payment_reversals_digital_payment_transactions;
                ALTER TABLE financial_adjustments
                    DROP CONSTRAINT IF EXISTS fk_digital_payment_reversals_related_reversals;
                ALTER TABLE financial_adjustments
                    DROP CONSTRAINT IF EXISTS fk_digital_payment_reversals_users_reversed_by;

                ALTER TABLE financial_adjustments
                    ADD CONSTRAINT fk_financial_adjustments_digital_payment_transactions
                    FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions(id) ON DELETE RESTRICT;
                ALTER TABLE financial_adjustments
                    ADD CONSTRAINT fk_financial_adjustments_related_adjustment
                    FOREIGN KEY (related_adjustment_id) REFERENCES financial_adjustments(id) ON DELETE SET NULL;
                ALTER TABLE financial_adjustments
                    ADD CONSTRAINT fk_financial_adjustments_users_completed_by
                    FOREIGN KEY (completed_by_id) REFERENCES users(id) ON DELETE SET NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_financial_adjustment_type", table: "financial_adjustments",
                sql: "adjustment_type IN ('ACCOUNTING_REVERSAL')");
            migrationBuilder.AddCheckConstraint(
                name: "ck_financial_adjustment_status", table: "financial_adjustments",
                sql: "status IN ('REQUESTED','APPROVED','REJECTED')");

            migrationBuilder.CreateIndex(
                name: "ix_financial_adjustments_digital_payment_transaction_id_status",
                table: "financial_adjustments", columns: new[] { "digital_payment_transaction_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digital_payment_retries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    attempted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    result_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    result_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    retry_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_payment_retries", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_payment_retries_digital_payment_transactions",
                        column: x => x.digital_payment_transaction_id,
                        principalTable: "digital_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digital_payment_retries_users_initiated_by",
                        column: x => x.initiated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_retries_digital_payment_transaction_id",
                table: "digital_payment_retries",
                column: "digital_payment_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_retries_initiated_by_id",
                table: "digital_payment_retries",
                column: "initiated_by_id");

            migrationBuilder.Sql(
                """
                INSERT INTO digital_payment_retries
                    (id, digital_payment_transaction_id, retry_type, previous_status, reason,
                     attempt_number, attempted_at_utc, result_status, result_message, initiated_by_id,
                     created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
                SELECT e.id, e.digital_payment_transaction_id, e.source, COALESCE(e.previous_status, e.new_status),
                       e.sanitized_details_json ->> 'reason',
                       COALESCE((e.sanitized_details_json ->> 'attemptNumber')::integer, 1), e.occurred_at_utc,
                       e.sanitized_details_json ->> 'resultStatus', e.sanitized_details_json ->> 'resultMessage',
                       NULLIF(e.sanitized_details_json ->> 'initiatedById', '')::uuid,
                       e.created_date, e.updated_date, e.last_processed_time_stamp,
                       e.created_by_id, e.updated_by_id, e.version
                FROM digital_payment_events e
                WHERE e.event_type = 'RECOVERY_ATTEMPT' AND e.correlation_id LIKE 'retry:%';

                DELETE FROM digital_payment_events
                WHERE event_type = 'RECOVERY_ATTEMPT' AND correlation_id LIKE 'retry:%';
                """);

            migrationBuilder.DropColumn(name: "sanitized_details_json", table: "digital_payment_events");

            migrationBuilder.DropIndex(
                name: "ix_financial_adjustments_digital_payment_transaction_id_status",
                table: "financial_adjustments");
            migrationBuilder.DropCheckConstraint(name: "ck_financial_adjustment_type", table: "financial_adjustments");
            migrationBuilder.DropCheckConstraint(name: "ck_financial_adjustment_status", table: "financial_adjustments");

            migrationBuilder.Sql(
                """
                ALTER TABLE financial_adjustments DROP CONSTRAINT IF EXISTS fk_financial_adjustments_digital_payment_transactions;
                ALTER TABLE financial_adjustments DROP CONSTRAINT IF EXISTS fk_financial_adjustments_related_adjustment;
                ALTER TABLE financial_adjustments DROP CONSTRAINT IF EXISTS fk_financial_adjustments_users_completed_by;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "provider_refund_initiated", table: "financial_adjustments",
                type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(
                name: "provider_refund_transaction_id", table: "financial_adjustments",
                type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.DropColumn(name: "adjustment_type", table: "financial_adjustments");

            migrationBuilder.RenameColumn(name: "related_adjustment_id", table: "financial_adjustments", newName: "related_reversal_id");
            migrationBuilder.RenameColumn(name: "status", table: "financial_adjustments", newName: "reversal_status");
            migrationBuilder.RenameColumn(name: "completed_at_utc", table: "financial_adjustments", newName: "reversed_at_utc");
            migrationBuilder.RenameColumn(name: "completed_by_id", table: "financial_adjustments", newName: "reversed_by_id");
            migrationBuilder.RenameTable(name: "financial_adjustments", newName: "digital_payment_reversals");

            migrationBuilder.Sql(
                """
                ALTER TABLE digital_payment_reversals RENAME CONSTRAINT pk_financial_adjustments TO pk_digital_payment_reversals;
                ALTER INDEX IF EXISTS ix_financial_adjustments_digital_payment_transaction_id
                    RENAME TO ix_digital_payment_reversals_digital_payment_transaction_id;
                ALTER INDEX IF EXISTS ix_financial_adjustments_related_adjustment_id
                    RENAME TO ix_digital_payment_reversals_related_reversal_id;
                ALTER INDEX IF EXISTS ix_financial_adjustments_completed_by_id
                    RENAME TO ix_digital_payment_reversals_reversed_by_id;

                ALTER TABLE digital_payment_reversals
                    ADD CONSTRAINT fk_digital_payment_reversals_digital_payment_transactions
                    FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions(id) ON DELETE CASCADE;
                ALTER TABLE digital_payment_reversals
                    ADD CONSTRAINT fk_digital_payment_reversals_related_reversals
                    FOREIGN KEY (related_reversal_id) REFERENCES digital_payment_reversals(id) ON DELETE SET NULL;
                ALTER TABLE digital_payment_reversals
                    ADD CONSTRAINT fk_digital_payment_reversals_users_reversed_by
                    FOREIGN KEY (reversed_by_id) REFERENCES users(id) ON DELETE SET NULL;
                """);
        }
    }
}
