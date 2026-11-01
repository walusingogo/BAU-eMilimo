using System;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261030130000_ExtendDigitalPaymentAndAddRetryReversal")]
    /// <inheritdoc />
    public partial class ExtendDigitalPaymentAndAddRetryReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Extend digital_giving_type_settings
            migrationBuilder.AddColumn<bool>(
                name: "allow_destination_selection",
                table: "digital_giving_type_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "destination_eligibility_policy",
                table: "digital_giving_type_settings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AS_CONFIGURED");

            migrationBuilder.AddColumn<string>(
                name: "destination_type",
                table: "digital_giving_type_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ORGANIZATION");

            // A public contribution category may be organization-wide, in which
            // case all three entity foreign keys are intentionally null.
            migrationBuilder.Sql(
                """
                ALTER TABLE digital_giving_type_settings
                DROP CONSTRAINT IF EXISTS ck_digital_giving_type_destination;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_digital_giving_type_destination_updated",
                table: "digital_giving_type_settings",
                sql: "((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1");

            // Update digital_payment_lines constraint to allow organization-wide
            migrationBuilder.Sql(
                """
                ALTER TABLE digital_payment_lines 
                DROP CONSTRAINT IF EXISTS ck_digital_payment_line_destination;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_digital_payment_line_destination_updated",
                table: "digital_payment_lines",
                sql: "((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1");

            // Create digital_payment_retries table
            migrationBuilder.CreateTable(
                name: "digital_payment_retries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retry_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    attempted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    result_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    result_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    initiated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
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

            // Create digital_payment_reversals table
            migrationBuilder.CreateTable(
                name: "digital_payment_reversals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_reversal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reversal_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_refund_initiated = table.Column<bool>(type: "boolean", nullable: false),
                    provider_refund_transaction_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reversed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reversed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_payment_reversals", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_payment_reversals_digital_payment_transactions",
                        column: x => x.digital_payment_transaction_id,
                        principalTable: "digital_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digital_payment_reversals_related_reversals",
                        column: x => x.related_reversal_id,
                        principalTable: "digital_payment_reversals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_digital_payment_reversals_users_reversed_by",
                        column: x => x.reversed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_retries_digital_payment_transaction_id",
                table: "digital_payment_retries",
                column: "digital_payment_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_retries_initiated_by_id",
                table: "digital_payment_retries",
                column: "initiated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_reversals_digital_payment_transaction_id",
                table: "digital_payment_reversals",
                column: "digital_payment_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_reversals_related_reversal_id",
                table: "digital_payment_reversals",
                column: "related_reversal_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_reversals_reversed_by_id",
                table: "digital_payment_reversals",
                column: "reversed_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digital_payment_retries");

            migrationBuilder.DropTable(
                name: "digital_payment_reversals");

            migrationBuilder.DropCheckConstraint(
                name: "ck_digital_payment_line_destination_updated",
                table: "digital_payment_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_digital_giving_type_destination_updated",
                table: "digital_giving_type_settings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_digital_giving_type_destination",
                table: "digital_giving_type_settings",
                sql: "NOT is_publicly_available OR ((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_digital_payment_line_destination",
                table: "digital_payment_lines",
                sql: "((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) = 1");

            migrationBuilder.DropColumn(
                name: "allow_destination_selection",
                table: "digital_giving_type_settings");

            migrationBuilder.DropColumn(
                name: "destination_eligibility_policy",
                table: "digital_giving_type_settings");

            migrationBuilder.DropColumn(
                name: "destination_type",
                table: "digital_giving_type_settings");
        }
    }
}
