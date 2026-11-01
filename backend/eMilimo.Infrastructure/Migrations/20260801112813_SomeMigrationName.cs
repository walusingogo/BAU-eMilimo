using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SomeMigrationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    account_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("pk_bank_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contribution_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_contribution_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contribution_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contribution_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_number = table.Column<int>(type: "integer", nullable: false),
                    section_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_key = table.Column<string>(type: "text", nullable: false),
                    config_value = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contribution_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    percentage_allocation = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contribution_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contribution_atms",
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
                    table.PrimaryKey("pk_contribution_atms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contribution_lcks",
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
                    table.PrimaryKey("pk_contribution_lcks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    contribution_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_split = table.Column<bool>(type: "boolean", nullable: false),
                    received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    archive_reason = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contributions", x => x.id);
                    table.ForeignKey(
                        name: "fk_contributions_bank_accounts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_contributions_contribution_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "contribution_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contributions_contribution_types_contribution_type_id",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fund_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    budgeted_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    own_funds_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    committed_budgeted_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    committed_own_funds_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fund_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fund_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fund_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    previous_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    new_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reversal_reason = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fund_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    group_type = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    leader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_groups_groups_parent_group_id",
                        column: x => x.parent_group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    middle_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    profile_photo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    membership_id = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    password_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_expired = table.Column<bool>(type: "boolean", nullable: false),
                    password_attempts = table.Column<int>(type: "integer", nullable: false),
                    password_lock_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    locked = table.Column<bool>(type: "boolean", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signature_data = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "requisitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_number = table.Column<string>(type: "text", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requisitor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    money_ready_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    collected_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payee = table.Column<string>(type: "text", nullable: false),
                    payment_method = table.Column<string>(type: "text", nullable: false),
                    fund_source = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_words = table.Column<string>(type: "text", nullable: false),
                    payment_details = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    money_ready_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    collection_signature = table.Column<string>(type: "text", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archive_reason = table.Column<string>(type: "text", nullable: true),
                    first_senior_approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    second_senior_approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requisitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_requisitions_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_requisitions_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_requisitions_users_archived_by_id",
                        column: x => x.archived_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_requisitions_users_collected_by_id",
                        column: x => x.collected_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requisitions_users_money_ready_by_id",
                        column: x => x.money_ready_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requisitions_users_requisitor_id",
                        column: x => x.requisitor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "requisition_atms",
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
                    table.PrimaryKey("pk_requisition_atms", x => x.id);
                    table.ForeignKey(
                        name: "fk_requisition_atms_requisitions_entity_id",
                        column: x => x.entity_id,
                        principalTable: "requisitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_requisition_atms_requisitions_requisition_id",
                        column: x => x.requisition_id,
                        principalTable: "requisitions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "requisition_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requisition_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_requisition_items_requisitions_requisition_id",
                        column: x => x.requisition_id,
                        principalTable: "requisitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_account_number",
                table: "bank_accounts",
                column: "account_number");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_allocations_contribution_id",
                table: "contribution_allocations",
                column: "contribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_allocations_created_by_id",
                table: "contribution_allocations",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_allocations_group_id",
                table: "contribution_allocations",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_atms_entity_id",
                table: "contribution_atms",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_lcks_entity_id",
                table: "contribution_lcks",
                column: "entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contribution_lcks_locked_by_id",
                table: "contribution_lcks",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_types_code",
                table: "contribution_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contributions_bank_account_id",
                table: "contributions",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_confirmed_by",
                table: "contributions",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_contribution_date",
                table: "contributions",
                column: "contribution_date");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_contribution_number",
                table: "contributions",
                column: "contribution_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contributions_contribution_type_id",
                table: "contributions",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_created_by_id",
                table: "contributions",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_received_by",
                table: "contributions",
                column: "received_by");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_source_id",
                table: "contributions",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_status",
                table: "contributions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_updated_by_id",
                table: "contributions",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_fund_balances_group_id_fiscal_year",
                table: "fund_balances",
                columns: new[] { "group_id", "fiscal_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_created_by_id",
                table: "fund_transactions",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_created_date",
                table: "fund_transactions",
                column: "created_date");

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_group_id",
                table: "fund_transactions",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_fund_transactions_updated_by_id",
                table: "fund_transactions",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_group_name",
                table: "groups",
                column: "group_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_leader_id",
                table: "groups",
                column: "leader_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_parent_group_id",
                table: "groups",
                column: "parent_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisition_atms_entity_id",
                table: "requisition_atms",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisition_atms_requisition_id",
                table: "requisition_atms",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisition_items_requisition_id",
                table: "requisition_items",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_approved_by_id",
                table: "requisitions",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_archived_by_id",
                table: "requisitions",
                column: "archived_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_collected_by_id",
                table: "requisitions",
                column: "collected_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_group_id",
                table: "requisitions",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_money_ready_by_id",
                table: "requisitions",
                column: "money_ready_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_requisition_number",
                table: "requisitions",
                column: "requisition_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_requisitor_id",
                table: "requisitions",
                column: "requisitor_id");

            migrationBuilder.CreateIndex(
                name: "ix_requisitions_status",
                table: "requisitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sections_section_number",
                table: "sections",
                column: "section_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_configs_config_key",
                table: "system_configs",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_by_id",
                table: "users",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_group_id",
                table: "users",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_membership_id",
                table: "users",
                column: "membership_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_section_id",
                table: "users",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_updated_by_id",
                table: "users",
                column: "updated_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_allocations_contributions_contribution_id",
                table: "contribution_allocations",
                column: "contribution_id",
                principalTable: "contributions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_allocations_groups_group_id",
                table: "contribution_allocations",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_allocations_users_created_by_id",
                table: "contribution_allocations",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_atms_contributions_entity_id",
                table: "contribution_atms",
                column: "entity_id",
                principalTable: "contributions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_lcks_contributions_entity_id",
                table: "contribution_lcks",
                column: "entity_id",
                principalTable: "contributions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_lcks_users_locked_by_id",
                table: "contribution_lcks",
                column: "locked_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_users_confirmed_by",
                table: "contributions",
                column: "confirmed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_users_created_by_id",
                table: "contributions",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_users_received_by",
                table: "contributions",
                column: "received_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_users_updated_by_id",
                table: "contributions",
                column: "updated_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_balances_groups_group_id",
                table: "fund_balances",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_transactions_groups_group_id",
                table: "fund_transactions",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_transactions_users_created_by_id",
                table: "fund_transactions",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fund_transactions_users_updated_by_id",
                table: "fund_transactions",
                column: "updated_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_users_leader_id",
                table: "groups",
                column: "leader_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_groups_group_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "contribution_allocations");

            migrationBuilder.DropTable(
                name: "contribution_atms");

            migrationBuilder.DropTable(
                name: "contribution_lcks");

            migrationBuilder.DropTable(
                name: "fund_balances");

            migrationBuilder.DropTable(
                name: "fund_transactions");

            migrationBuilder.DropTable(
                name: "requisition_atms");

            migrationBuilder.DropTable(
                name: "requisition_items");

            migrationBuilder.DropTable(
                name: "system_configs");

            migrationBuilder.DropTable(
                name: "contributions");

            migrationBuilder.DropTable(
                name: "requisitions");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "contribution_sources");

            migrationBuilder.DropTable(
                name: "contribution_types");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "sections");
        }
    }
}
