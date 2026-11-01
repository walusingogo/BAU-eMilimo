using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipAndServiceCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contributions_finalized_financial_record_book_id",
                table: "contributions");

            migrationBuilder.AddColumn<string>(
                name: "contributor_name_snapshot",
                table: "contributions",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "contributions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate_snapshot",
                table: "contributions",
                type: "numeric(28,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "member_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "membership_number_snapshot",
                table: "contributions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_type",
                table: "contributions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "original_currency_amount",
                table: "contributions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_method_definition_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reverses_contribution_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "service_collection_entry_line_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contribution_type_id",
                table: "contribution_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "member_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_aud", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_names = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    preferred_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sex = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    membership_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    joined_on = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archive_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    record_source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    synchronization_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_synchronized_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    normalized_search_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_members_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "membership_integration_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    use_local_fallback = table.Column<bool>(type: "boolean", nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    field_ownership_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_successful_sync_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_integration_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "membership_sync_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    read_count = table.Column<int>(type: "integer", nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    conflict_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    initiated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_sync_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_method_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requires_reference = table.Column<bool>(type: "boolean", nullable: false),
                    requires_bank_account = table.Column<bool>(type: "boolean", nullable: false),
                    requires_attachment = table.Column<bool>(type: "boolean", nullable: false),
                    supports_foreign_currency = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_payment_method_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collection_aud", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_service_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "member_external_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_member_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    external_membership_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_synchronized_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_external_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_external_identities_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "membership_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_membership_status_history_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_membership_status_history_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sequence_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attendance = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_events_committees_committee_id",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_events_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_events_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_events_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    service_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    purpose_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tender_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    variance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    variance_explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    returned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    returned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archive_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    finalized_financial_record_book_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collections", x => x.id);
                    table.CheckConstraint("ck_service_collections_scope", "((CASE WHEN section_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN group_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN committee_id IS NOT NULL THEN 1 ELSE 0 END)) <= 1");
                    table.ForeignKey(
                        name: "fk_service_collections_committees_committee_id",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collections_contribution_sources_contribution_sourc",
                        column: x => x.contribution_source_id,
                        principalTable: "contribution_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collections_financial_record_books_finalized_financ",
                        column: x => x.finalized_financial_record_book_id,
                        principalTable: "financial_record_books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collections_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collections_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collections_service_events_service_event_id",
                        column: x => x.service_event_id,
                        principalTable: "service_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "financial_record_book_service_collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_record_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_number_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purpose_total_snapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_record_book_service_collections", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_record_book_service_collections_financial_record_",
                        column: x => x.financial_record_book_id,
                        principalTable: "financial_record_books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_financial_record_book_service_collections_service_collectio",
                        column: x => x.service_collection_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_atm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("pk_service_collection_atm", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_collection_atm_service_collections_entity_id",
                        column: x => x.entity_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_denominations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    denomination = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(28,12)", nullable: false),
                    base_equivalent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    exchange_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_manual_rate_override = table.Column<bool>(type: "boolean", nullable: false),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collection_denominations", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_collection_denominations_currencies_currency_code",
                        column: x => x.currency_code,
                        principalTable: "currencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_denominations_exchange_rates_exchange_ra",
                        column: x => x.exchange_rate_id,
                        principalTable: "exchange_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_denominations_service_collections_servic",
                        column: x => x.service_collection_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    membership_number_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contributor_name_snapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    contributor_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collection_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_collection_entries_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_entries_service_collections_service_coll",
                        column: x => x.service_collection_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_lck",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    released_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_service_collection_lck", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_collection_lck_service_collections_entity_id",
                        column: x => x.entity_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_collection_lck_users_locked_by_id",
                        column: x => x.locked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collection_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_collection_participants_service_collections_service",
                        column: x => x.service_collection_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_collection_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_tenders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(28,12)", nullable: false),
                    base_equivalent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    exchange_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_manual_rate_override = table.Column<bool>(type: "boolean", nullable: false),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collection_tenders", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_collection_tenders_bank_accounts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_tenders_currencies_currency_code",
                        column: x => x.currency_code,
                        principalTable: "currencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_tenders_exchange_rates_exchange_rate_id",
                        column: x => x.exchange_rate_id,
                        principalTable: "exchange_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_tenders_payment_method_definitions_payme",
                        column: x => x.payment_method_definition_id,
                        principalTable: "payment_method_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_tenders_service_collections_service_coll",
                        column: x => x.service_collection_id,
                        principalTable: "service_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_collection_entry_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_collection_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_collection_entry_lines", x => x.id);
                    table.CheckConstraint("ck_service_collection_entry_lines_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_service_collection_entry_lines_contribution_types_contribut",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_collection_entry_lines_service_collection_entries_s",
                        column: x => x.service_collection_entry_id,
                        principalTable: "service_collection_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contributions_finalized_financial_record_book_id",
                table: "contributions",
                column: "finalized_financial_record_book_id",
                filter: "finalized_financial_record_book_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_member_id_contribution_date",
                table: "contributions",
                columns: new[] { "member_id", "contribution_date" });

            migrationBuilder.CreateIndex(
                name: "ix_contributions_payment_method_definition_id",
                table: "contributions",
                column: "payment_method_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_reverses_contribution_id",
                table: "contributions",
                column: "reverses_contribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_service_collection_entry_line_id",
                table: "contributions",
                column: "service_collection_entry_line_id",
                unique: true,
                filter: "service_collection_entry_line_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_contribution_allocations_contribution_type_id",
                table: "contribution_allocations",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_book_service_collections_financial_record_",
                table: "financial_record_book_service_collections",
                columns: new[] { "financial_record_book_id", "service_collection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_book_service_collections_service_collectio",
                table: "financial_record_book_service_collections",
                column: "service_collection_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_member_aud_member_id_performed_at_utc",
                table: "member_aud",
                columns: new[] { "member_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_member_external_identities_member_id",
                table: "member_external_identities",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_external_identities_provider_code_external_member_id",
                table: "member_external_identities",
                columns: new[] { "provider_code", "external_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_group_id",
                table: "members",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_membership_number",
                table: "members",
                column: "membership_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_normalized_phone_number",
                table: "members",
                column: "normalized_phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_members_normalized_search_name",
                table: "members",
                column: "normalized_search_name");

            migrationBuilder.CreateIndex(
                name: "ix_members_section_id",
                table: "members",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_status_is_archived",
                table: "members",
                columns: new[] { "status", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "ix_membership_integration_configurations_provider_code",
                table: "membership_integration_configurations",
                column: "provider_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_membership_status_history_changed_by_id",
                table: "membership_status_history",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_membership_status_history_member_id_effective_at_utc",
                table: "membership_status_history",
                columns: new[] { "member_id", "effective_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_membership_sync_logs_provider_code_started_at_utc",
                table: "membership_sync_logs",
                columns: new[] { "provider_code", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_method_definitions_code",
                table: "payment_method_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_atm_entity_id",
                table: "service_collection_atm",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_aud_service_collection_id_performed_at_u",
                table: "service_collection_aud",
                columns: new[] { "service_collection_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_denominations_currency_code",
                table: "service_collection_denominations",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_denominations_exchange_rate_id",
                table: "service_collection_denominations",
                column: "exchange_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_denominations_service_collection_id_curr",
                table: "service_collection_denominations",
                columns: new[] { "service_collection_id", "currency_code", "denomination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_entries_member_id",
                table: "service_collection_entries",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_entries_service_collection_id_member_id",
                table: "service_collection_entries",
                columns: new[] { "service_collection_id", "member_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_entry_lines_contribution_type_id",
                table: "service_collection_entry_lines",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_entry_lines_service_collection_entry_id_",
                table: "service_collection_entry_lines",
                columns: new[] { "service_collection_entry_id", "contribution_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_lck_entity_id_released_at_utc",
                table: "service_collection_lck",
                columns: new[] { "entity_id", "released_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_lck_locked_by_id",
                table: "service_collection_lck",
                column: "locked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_participants_service_collection_id_user_",
                table: "service_collection_participants",
                columns: new[] { "service_collection_id", "user_id", "responsibility" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_participants_user_id",
                table: "service_collection_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_tenders_bank_account_id",
                table: "service_collection_tenders",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_tenders_currency_code",
                table: "service_collection_tenders",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_tenders_exchange_rate_id",
                table: "service_collection_tenders",
                column: "exchange_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_tenders_payment_method_definition_id",
                table: "service_collection_tenders",
                column: "payment_method_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collection_tenders_service_collection_id",
                table: "service_collection_tenders",
                column: "service_collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_collection_number",
                table: "service_collections",
                column: "collection_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_committee_id",
                table: "service_collections",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_contribution_source_id",
                table: "service_collections",
                column: "contribution_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_finalized_financial_record_book_id",
                table: "service_collections",
                column: "finalized_financial_record_book_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_group_id",
                table: "service_collections",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_section_id",
                table: "service_collections",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_service_event_id",
                table: "service_collections",
                column: "service_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_collections_status_created_date",
                table: "service_collections",
                columns: new[] { "status", "created_date" });

            migrationBuilder.CreateIndex(
                name: "ix_service_events_committee_id",
                table: "service_events",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_events_group_id",
                table: "service_events",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_events_section_id",
                table: "service_events",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_events_service_type_id",
                table: "service_events",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_events_starts_at_utc_service_type_id",
                table: "service_events",
                columns: new[] { "starts_at_utc", "service_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_types_code",
                table: "service_types",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_contribution_allocations_contribution_types_contribution_ty",
                table: "contribution_allocations",
                column: "contribution_type_id",
                principalTable: "contribution_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_contributions_reverses_contribution_id",
                table: "contributions",
                column: "reverses_contribution_id",
                principalTable: "contributions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_members_member_id",
                table: "contributions",
                column: "member_id",
                principalTable: "members",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_payment_method_definitions_payment_method_def",
                table: "contributions",
                column: "payment_method_definition_id",
                principalTable: "payment_method_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_service_collection_entry_lines_service_collec",
                table: "contributions",
                column: "service_collection_entry_line_id",
                principalTable: "service_collection_entry_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contribution_allocations_contribution_types_contribution_ty",
                table: "contribution_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_contributions_contributions_reverses_contribution_id",
                table: "contributions");

            migrationBuilder.DropForeignKey(
                name: "fk_contributions_members_member_id",
                table: "contributions");

            migrationBuilder.DropForeignKey(
                name: "fk_contributions_payment_method_definitions_payment_method_def",
                table: "contributions");

            migrationBuilder.DropForeignKey(
                name: "fk_contributions_service_collection_entry_lines_service_collec",
                table: "contributions");

            migrationBuilder.DropTable(
                name: "financial_record_book_service_collections");

            migrationBuilder.DropTable(
                name: "member_aud");

            migrationBuilder.DropTable(
                name: "member_external_identities");

            migrationBuilder.DropTable(
                name: "membership_integration_configurations");

            migrationBuilder.DropTable(
                name: "membership_status_history");

            migrationBuilder.DropTable(
                name: "membership_sync_logs");

            migrationBuilder.DropTable(
                name: "service_collection_atm");

            migrationBuilder.DropTable(
                name: "service_collection_aud");

            migrationBuilder.DropTable(
                name: "service_collection_denominations");

            migrationBuilder.DropTable(
                name: "service_collection_entry_lines");

            migrationBuilder.DropTable(
                name: "service_collection_lck");

            migrationBuilder.DropTable(
                name: "service_collection_participants");

            migrationBuilder.DropTable(
                name: "service_collection_tenders");

            migrationBuilder.DropTable(
                name: "service_collection_entries");

            migrationBuilder.DropTable(
                name: "payment_method_definitions");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "service_collections");

            migrationBuilder.DropTable(
                name: "service_events");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DropIndex(
                name: "ix_contributions_finalized_financial_record_book_id",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contributions_member_id_contribution_date",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contributions_payment_method_definition_id",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contributions_reverses_contribution_id",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contributions_service_collection_entry_line_id",
                table: "contributions");

            migrationBuilder.DropIndex(
                name: "ix_contribution_allocations_contribution_type_id",
                table: "contribution_allocations");

            migrationBuilder.DropColumn(
                name: "contributor_name_snapshot",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "exchange_rate_snapshot",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "member_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "membership_number_snapshot",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "origin_type",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "original_currency_amount",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "payment_method_definition_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "reverses_contribution_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "service_collection_entry_line_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "contribution_type_id",
                table: "contribution_allocations");

            migrationBuilder.CreateIndex(
                name: "ix_contributions_finalized_financial_record_book_id",
                table: "contributions",
                column: "finalized_financial_record_book_id",
                unique: true,
                filter: "finalized_financial_record_book_id IS NOT NULL");
        }
    }
}
