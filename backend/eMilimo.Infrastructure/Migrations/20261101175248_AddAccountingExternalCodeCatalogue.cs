using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingExternalCodeCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounting_external_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_integration_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    allows_debit = table.Column<bool>(type: "boolean", nullable: false),
                    allows_credit = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_external_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_external_codes_accounting_integration_profiles_a",
                        column: x => x.accounting_integration_profile_id,
                        principalTable: "accounting_integration_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_external_code_atm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_external_code_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_external_code_atm_accounting_external_codes_enti",
                        column: x => x.entity_id,
                        principalTable: "accounting_external_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_external_code_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_external_code_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_external_code_aud_accounting_external_codes_enti",
                        column: x => x.entity_id,
                        principalTable: "accounting_external_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_external_code_lck",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_accounting_external_code_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_external_code_lck_accounting_external_codes_enti",
                        column: x => x.entity_id,
                        principalTable: "accounting_external_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accounting_external_code_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_external_code_atm_entity_id",
                table: "accounting_external_code_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_external_code_aud_entity_id_performed_at_utc",
                table: "accounting_external_code_aud",
                columns: new[] { "entity_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_external_code_lck_entity_id",
                table: "accounting_external_code_lck",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_external_code_lck_locked_by_id",
                table: "accounting_external_code_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_external_codes_accounting_integration_profile_id",
                table: "accounting_external_codes",
                columns: new[] { "accounting_integration_profile_id", "code_type", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_external_codes_accounting_integration_profile_id1",
                table: "accounting_external_codes",
                columns: new[] { "accounting_integration_profile_id", "code_type", "is_active" });

            // Preserve every code already configured on an accounting mapping.
            // The deterministic UUID makes the backfill idempotent without
            // requiring a PostgreSQL UUID extension.
            migrationBuilder.Sql("""
                INSERT INTO accounting_external_codes
                    (id, accounting_integration_profile_id, code_type, code, name,
                     allows_debit, allows_credit, is_active, created_date, updated_date, version)
                SELECT md5(profile_id::text || ':' || code_type || ':' || code)::uuid,
                       profile_id, code_type, code, code,
                       BOOL_OR(allows_debit), BOOL_OR(allows_credit), TRUE, NOW(), NOW(), 1
                FROM (
                    SELECT DISTINCT accounting_integration_profile_id AS profile_id, 'GL_ACCOUNT' AS code_type,
                           UPPER(TRIM(debit_account_code)) AS code, TRUE AS allows_debit, FALSE AS allows_credit
                    FROM accounting_account_mappings WHERE NULLIF(TRIM(debit_account_code), '') IS NOT NULL
                    UNION ALL
                    SELECT DISTINCT accounting_integration_profile_id, 'GL_ACCOUNT',
                           UPPER(TRIM(credit_account_code)), FALSE, TRUE
                    FROM accounting_account_mappings WHERE NULLIF(TRIM(credit_account_code), '') IS NOT NULL
                    UNION ALL
                    SELECT DISTINCT accounting_integration_profile_id, 'CASH_BOOK',
                           UPPER(TRIM(cash_book_code)), TRUE, TRUE
                    FROM accounting_account_mappings WHERE NULLIF(TRIM(cash_book_code), '') IS NOT NULL
                    UNION ALL
                    SELECT DISTINCT accounting_integration_profile_id, 'ENTRY_TYPE',
                           UPPER(TRIM(entry_type_code)), TRUE, TRUE
                    FROM accounting_account_mappings WHERE NULLIF(TRIM(entry_type_code), '') IS NOT NULL
                    UNION ALL
                    SELECT DISTINCT accounting_integration_profile_id, 'PROJECT',
                           UPPER(TRIM(project_code)), TRUE, TRUE
                    FROM accounting_account_mappings WHERE NULLIF(TRIM(project_code), '') IS NOT NULL
                    UNION ALL
                    SELECT DISTINCT accounting_integration_profile_id, 'TAX_TYPE',
                           UPPER(TRIM(tax_type_code)), TRUE, TRUE
                    FROM accounting_account_mappings WHERE NULLIF(TRIM(tax_type_code), '') IS NOT NULL
                ) source
                GROUP BY profile_id, code_type, code
                ON CONFLICT (accounting_integration_profile_id, code_type, code) DO NOTHING;

                UPDATE accounting_external_codes target
                SET allows_debit = TRUE
                WHERE target.code_type = 'GL_ACCOUNT'
                  AND EXISTS (
                    SELECT 1 FROM accounting_account_mappings mapping
                    WHERE mapping.accounting_integration_profile_id = target.accounting_integration_profile_id
                      AND UPPER(TRIM(mapping.debit_account_code)) = target.code);

                UPDATE accounting_external_codes target
                SET allows_credit = TRUE
                WHERE target.code_type = 'GL_ACCOUNT'
                  AND EXISTS (
                    SELECT 1 FROM accounting_account_mappings mapping
                    WHERE mapping.accounting_integration_profile_id = target.accounting_integration_profile_id
                      AND UPPER(TRIM(mapping.credit_account_code)) = target.code);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_external_code_atm");

            migrationBuilder.DropTable(
                name: "accounting_external_code_aud");

            migrationBuilder.DropTable(
                name: "accounting_external_code_lck");

            migrationBuilder.DropTable(
                name: "accounting_external_codes");
        }
    }
}
