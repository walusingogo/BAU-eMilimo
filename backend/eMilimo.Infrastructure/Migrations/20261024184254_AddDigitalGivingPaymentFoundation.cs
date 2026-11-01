using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalGivingPaymentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digital_giving_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    public_slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    minimum_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    maximum_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    contribution_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    welcome_message = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_giving_configurations", x => x.id);
                    table.CheckConstraint("ck_digital_giving_configuration_maximum", "maximum_amount IS NULL OR maximum_amount >= minimum_amount");
                    table.CheckConstraint("ck_digital_giving_configuration_minimum", "minimum_amount > 0");
                    table.ForeignKey(
                        name: "fk_digital_giving_configurations_contribution_sources_contribu",
                        column: x => x.contribution_source_id,
                        principalTable: "contribution_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_provider_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    provider_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    environment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    protected_configuration = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_provider_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "digital_giving_type_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_giving_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_publicly_available = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_giving_type_settings", x => x.id);
                    table.CheckConstraint("ck_digital_giving_type_destination", "NOT is_publicly_available OR ((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) = 1");
                    table.ForeignKey(
                        name: "fk_digital_giving_type_settings_committees_committee_id",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_giving_type_settings_contribution_types_contributio",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_giving_type_settings_digital_giving_configurations_",
                        column: x => x.digital_giving_configuration_id,
                        principalTable: "digital_giving_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digital_giving_type_settings_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_giving_type_settings_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "digital_payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_reference = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    public_status_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    giver_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contributor_name_snapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    protected_payer_data = table.Column<string>(type: "text", nullable: true),
                    payment_method_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_provider_configuration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_transaction_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    settlement_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    provider_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_settlement_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    initiated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    settled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_payment_transactions", x => x.id);
                    table.CheckConstraint("ck_digital_payment_transaction_amounts", "gross_amount > 0 AND provider_fee >= 0 AND net_settlement_amount >= 0 AND provider_fee <= gross_amount");
                    table.ForeignKey(
                        name: "fk_digital_payment_transactions_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_payment_transactions_payment_method_definitions_pay",
                        column: x => x.payment_method_definition_id,
                        principalTable: "payment_method_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_payment_transactions_payment_provider_configuration",
                        column: x => x.payment_provider_configuration_id,
                        principalTable: "payment_provider_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_provider_routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_provider_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_payment_method_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_payment_provider_routes", x => x.id);
                    table.CheckConstraint("ck_payment_provider_route_priority", "priority >= 0");
                    table.ForeignKey(
                        name: "fk_payment_provider_routes_payment_method_definitions_payment_",
                        column: x => x.payment_method_definition_id,
                        principalTable: "payment_method_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_provider_routes_payment_provider_configurations_pay",
                        column: x => x.payment_provider_configuration_id,
                        principalTable: "payment_provider_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_provider_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    signature_valid = table.Column<bool>(type: "boolean", nullable: false),
                    processing_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    protected_payload = table.Column<string>(type: "text", nullable: true),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_attempts = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_webhook_events", x => x.id);
                    table.CheckConstraint("ck_payment_webhook_processing_attempts", "processing_attempts >= 0");
                    table.ForeignKey(
                        name: "fk_payment_webhook_events_payment_provider_configurations_paym",
                        column: x => x.payment_provider_configuration_id,
                        principalTable: "payment_provider_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "digital_payment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    protected_details = table.Column<string>(type: "text", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_payment_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_payment_events_digital_payment_transactions_digital",
                        column: x => x.digital_payment_transaction_id,
                        principalTable: "digital_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_payment_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contribution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_payment_lines", x => x.id);
                    table.CheckConstraint("ck_digital_payment_line_amount", "amount > 0");
                    table.CheckConstraint("ck_digital_payment_line_destination", "((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) = 1");
                    table.ForeignKey(
                        name: "fk_digital_payment_lines_committees_committee_id",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_payment_lines_contribution_types_contribution_type_",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_payment_lines_contributions_contribution_id",
                        column: x => x.contribution_id,
                        principalTable: "contributions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_payment_lines_digital_payment_transactions_digital_",
                        column: x => x.digital_payment_transaction_id,
                        principalTable: "digital_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digital_payment_lines_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_payment_lines_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_configurations_contribution_source_id",
                table: "digital_giving_configurations",
                column: "contribution_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_configurations_public_slug",
                table: "digital_giving_configurations",
                column: "public_slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_type_settings_committee_id",
                table: "digital_giving_type_settings",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_type_settings_contribution_type_id",
                table: "digital_giving_type_settings",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_type_settings_digital_giving_configuration_i",
                table: "digital_giving_type_settings",
                columns: new[] { "digital_giving_configuration_id", "contribution_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_type_settings_group_id",
                table: "digital_giving_type_settings",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_giving_type_settings_section_id",
                table: "digital_giving_type_settings",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_events_digital_payment_transaction_id_occur",
                table: "digital_payment_events",
                columns: new[] { "digital_payment_transaction_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_lines_committee_id",
                table: "digital_payment_lines",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_lines_contribution_id",
                table: "digital_payment_lines",
                column: "contribution_id",
                unique: true,
                filter: "contribution_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_lines_contribution_type_id",
                table: "digital_payment_lines",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_lines_digital_payment_transaction_id",
                table: "digital_payment_lines",
                column: "digital_payment_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_lines_group_id",
                table: "digital_payment_lines",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_lines_section_id",
                table: "digital_payment_lines",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_idempotency_key",
                table: "digital_payment_transactions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_member_id",
                table: "digital_payment_transactions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_payment_method_definition_id",
                table: "digital_payment_transactions",
                column: "payment_method_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_payment_provider_configuration",
                table: "digital_payment_transactions",
                columns: new[] { "payment_provider_configuration_id", "provider_transaction_id" },
                unique: true,
                filter: "provider_transaction_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_payment_reference",
                table: "digital_payment_transactions",
                column: "payment_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_public_status_token_hash",
                table: "digital_payment_transactions",
                column: "public_status_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_settlement_status_created_date",
                table: "digital_payment_transactions",
                columns: new[] { "settlement_status", "created_date" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_status_created_date",
                table: "digital_payment_transactions",
                columns: new[] { "status", "created_date" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_configurations_provider_key",
                table: "payment_provider_configurations",
                column: "provider_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_routes_payment_method_definition_id_paymen",
                table: "payment_provider_routes",
                columns: new[] { "payment_method_definition_id", "payment_provider_configuration_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_routes_payment_method_definition_id_priori",
                table: "payment_provider_routes",
                columns: new[] { "payment_method_definition_id", "priority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_routes_payment_provider_configuration_id",
                table: "payment_provider_routes",
                column: "payment_provider_configuration_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_payload_hash",
                table: "payment_webhook_events",
                column: "payload_hash");

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_payment_provider_configuration_id_ev",
                table: "payment_webhook_events",
                columns: new[] { "payment_provider_configuration_id", "event_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_processing_status_received_at_utc",
                table: "payment_webhook_events",
                columns: new[] { "processing_status", "received_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digital_giving_type_settings");

            migrationBuilder.DropTable(
                name: "digital_payment_events");

            migrationBuilder.DropTable(
                name: "digital_payment_lines");

            migrationBuilder.DropTable(
                name: "payment_provider_routes");

            migrationBuilder.DropTable(
                name: "payment_webhook_events");

            migrationBuilder.DropTable(
                name: "digital_giving_configurations");

            migrationBuilder.DropTable(
                name: "digital_payment_transactions");

            migrationBuilder.DropTable(
                name: "payment_provider_configurations");
        }
    }
}
