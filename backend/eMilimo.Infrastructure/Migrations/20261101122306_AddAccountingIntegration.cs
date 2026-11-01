using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounting_integration_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    product_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    company_identifier = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    integration_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    home_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    integration_setting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_integration_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_integration_profiles_integration_settings_integr",
                        column: x => x.integration_setting_id,
                        principalTable: "integration_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "accounting_account_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_integration_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    source_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    debit_account_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    credit_account_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    cash_book_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    entry_type_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    project_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tax_type_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("pk_accounting_account_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_account_mappings_accounting_integration_profiles",
                        column: x => x.accounting_integration_profile_id,
                        principalTable: "accounting_integration_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_integration_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    source_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_batch_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exported_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_batches", x => x.id);
                    table.CheckConstraint("ck_accounting_batch_period", "period_end >= period_start");
                    table.CheckConstraint("ck_accounting_batch_totals", "debit_total >= 0 AND credit_total >= 0");
                    table.ForeignKey(
                        name: "fk_accounting_batches_accounting_integration_profiles_accounti",
                        column: x => x.accounting_integration_profile_id,
                        principalTable: "accounting_integration_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounting_integration_profile_atm",
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
                    table.PrimaryKey("pk_accounting_integration_profile_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_integration_profile_atm_accounting_integration_p",
                        column: x => x.entity_id,
                        principalTable: "accounting_integration_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_integration_profile_aud",
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
                    table.PrimaryKey("pk_accounting_integration_profile_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_integration_profile_aud_accounting_integration_p",
                        column: x => x.entity_id,
                        principalTable: "accounting_integration_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_integration_profile_lck",
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
                    table.PrimaryKey("pk_accounting_integration_profile_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_integration_profile_lck_accounting_integration_p",
                        column: x => x.entity_id,
                        principalTable: "accounting_integration_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accounting_integration_profile_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounting_account_mapping_atm",
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
                    table.PrimaryKey("pk_accounting_account_mapping_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_account_mapping_atm_accounting_account_mappings_",
                        column: x => x.entity_id,
                        principalTable: "accounting_account_mappings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_account_mapping_aud",
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
                    table.PrimaryKey("pk_accounting_account_mapping_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_account_mapping_aud_accounting_account_mappings_",
                        column: x => x.entity_id,
                        principalTable: "accounting_account_mappings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_account_mapping_lck",
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
                    table.PrimaryKey("pk_accounting_account_mapping_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_account_mapping_lck_accounting_account_mappings_",
                        column: x => x.entity_id,
                        principalTable: "accounting_account_mappings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accounting_account_mapping_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_atm",
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
                    table.PrimaryKey("pk_accounting_batch_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_batch_atm_accounting_batches_entity_id",
                        column: x => x.entity_id,
                        principalTable: "accounting_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_aud",
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
                    table.PrimaryKey("pk_accounting_batch_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_batch_aud_accounting_batches_entity_id",
                        column: x => x.entity_id,
                        principalTable: "accounting_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_lck",
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
                    table.PrimaryKey("pk_accounting_batch_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_batch_lck_accounting_batches_entity_id",
                        column: x => x.entity_id,
                        principalTable: "accounting_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accounting_batch_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    account_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    project_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tax_type_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    original_currency_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(28,12)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_batch_lines", x => x.id);
                    table.CheckConstraint("ck_accounting_batch_line_one_side", "(debit > 0 AND credit = 0) OR (credit > 0 AND debit = 0)");
                    table.ForeignKey(
                        name: "fk_accounting_batch_lines_accounting_batches_accounting_batch_",
                        column: x => x.accounting_batch_id,
                        principalTable: "accounting_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_line_atm",
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
                    table.PrimaryKey("pk_accounting_batch_line_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_batch_line_atm_accounting_batch_lines_entity_id",
                        column: x => x.entity_id,
                        principalTable: "accounting_batch_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_line_aud",
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
                    table.PrimaryKey("pk_accounting_batch_line_aud", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_batch_line_aud_accounting_batch_lines_entity_id",
                        column: x => x.entity_id,
                        principalTable: "accounting_batch_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_batch_line_lck",
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
                    table.PrimaryKey("pk_accounting_batch_line_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_batch_line_lck_accounting_batch_lines_entity_id",
                        column: x => x.entity_id,
                        principalTable: "accounting_batch_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accounting_batch_line_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_account_mapping_atm_entity_id",
                table: "accounting_account_mapping_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_account_mapping_aud_entity_id_performed_at_utc",
                table: "accounting_account_mapping_aud",
                columns: new[] { "entity_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_account_mapping_lck_entity_id",
                table: "accounting_account_mapping_lck",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_account_mapping_lck_locked_by_id",
                table: "accounting_account_mapping_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_account_mappings_accounting_integration_profile_",
                table: "accounting_account_mappings",
                columns: new[] { "accounting_integration_profile_id", "mapping_type", "source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_atm_entity_id",
                table: "accounting_batch_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_aud_entity_id_performed_at_utc",
                table: "accounting_batch_aud",
                columns: new[] { "entity_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_lck_entity_id",
                table: "accounting_batch_lck",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_lck_locked_by_id",
                table: "accounting_batch_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_line_atm_entity_id",
                table: "accounting_batch_line_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_line_aud_entity_id_performed_at_utc",
                table: "accounting_batch_line_aud",
                columns: new[] { "entity_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_line_lck_entity_id",
                table: "accounting_batch_line_lck",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_line_lck_locked_by_id",
                table: "accounting_batch_line_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_lines_accounting_batch_id_line_number",
                table: "accounting_batch_lines",
                columns: new[] { "accounting_batch_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batch_lines_source_type_source_id",
                table: "accounting_batch_lines",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batches_accounting_integration_profile_id_source",
                table: "accounting_batches",
                columns: new[] { "accounting_integration_profile_id", "source_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batches_batch_number",
                table: "accounting_batches",
                column: "batch_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batches_status_created_date",
                table: "accounting_batches",
                columns: new[] { "status", "created_date" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_integration_profile_atm_entity_id",
                table: "accounting_integration_profile_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_integration_profile_aud_entity_id_performed_at_u",
                table: "accounting_integration_profile_aud",
                columns: new[] { "entity_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_integration_profile_lck_entity_id",
                table: "accounting_integration_profile_lck",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_integration_profile_lck_locked_by_id",
                table: "accounting_integration_profile_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_integration_profiles_integration_setting_id",
                table: "accounting_integration_profiles",
                column: "integration_setting_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_integration_profiles_provider_key_company_identi",
                table: "accounting_integration_profiles",
                columns: new[] { "provider_key", "company_identifier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_account_mapping_atm");

            migrationBuilder.DropTable(
                name: "accounting_account_mapping_aud");

            migrationBuilder.DropTable(
                name: "accounting_account_mapping_lck");

            migrationBuilder.DropTable(
                name: "accounting_batch_atm");

            migrationBuilder.DropTable(
                name: "accounting_batch_aud");

            migrationBuilder.DropTable(
                name: "accounting_batch_lck");

            migrationBuilder.DropTable(
                name: "accounting_batch_line_atm");

            migrationBuilder.DropTable(
                name: "accounting_batch_line_aud");

            migrationBuilder.DropTable(
                name: "accounting_batch_line_lck");

            migrationBuilder.DropTable(
                name: "accounting_integration_profile_atm");

            migrationBuilder.DropTable(
                name: "accounting_integration_profile_aud");

            migrationBuilder.DropTable(
                name: "accounting_integration_profile_lck");

            migrationBuilder.DropTable(
                name: "accounting_account_mappings");

            migrationBuilder.DropTable(
                name: "accounting_batch_lines");

            migrationBuilder.DropTable(
                name: "accounting_batches");

            migrationBuilder.DropTable(
                name: "accounting_integration_profiles");
        }
    }
}
