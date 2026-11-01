using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialRecordBooksAndReferenceNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "finalized_financial_record_book_id",
                table: "contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financial_record_books",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    frb_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    reviewer_comments = table.Column<string>(type: "text", nullable: true),
                    gross_contributions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    assessment_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_position = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    finalized_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalized_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_record_books", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_record_books_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "frb_assessment_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    beneficiary = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_frb_assessment_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "frb_aud",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_record_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frb_aud", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reference_number_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("pk_reference_number_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reference_number_sequences",
                columns: table => new
                {
                    reference_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sequence_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reference_number_sequences", x => new { x.reference_type, x.sequence_date });
                });

            migrationBuilder.CreateTable(
                name: "financial_record_book_contributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_record_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_record_book_contributions", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_record_book_contributions_contributions_contribut",
                        column: x => x.contribution_id,
                        principalTable: "contributions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_record_book_contributions_financial_record_books_",
                        column: x => x.financial_record_book_id,
                        principalTable: "financial_record_books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "frb_assessment_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_record_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    beneficiary = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    assessment_base = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    calculated_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    contribution_type_ids_json = table.Column<string>(type: "text", nullable: false),
                    contribution_type_names_json = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frb_assessment_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_frb_assessment_snapshots_financial_record_books_financial_r",
                        column: x => x.financial_record_book_id,
                        principalTable: "financial_record_books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "frb_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_record_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    verified_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    officer_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    signature_data = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    verified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frb_verifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_frb_verifications_financial_record_books_financial_record_b",
                        column: x => x.financial_record_book_id,
                        principalTable: "financial_record_books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "frb_assessment_rule_contribution_types",
                columns: table => new
                {
                    frb_assessment_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frb_assessment_rule_contribution_types", x => new { x.frb_assessment_rule_id, x.contribution_type_id });
                    table.ForeignKey(
                        name: "fk_frb_assessment_rule_contribution_types_contribution_types_c",
                        column: x => x.contribution_type_id,
                        principalTable: "contribution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_frb_assessment_rule_contribution_types_frb_assessment_rules",
                        column: x => x.frb_assessment_rule_id,
                        principalTable: "frb_assessment_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contributions_finalized_financial_record_book_id",
                table: "contributions",
                column: "finalized_financial_record_book_id",
                unique: true,
                filter: "finalized_financial_record_book_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_book_contributions_contribution_id",
                table: "financial_record_book_contributions",
                column: "contribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_book_contributions_financial_record_book_i",
                table: "financial_record_book_contributions",
                columns: new[] { "financial_record_book_id", "contribution_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_books_frb_number",
                table: "financial_record_books",
                column: "frb_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_books_section_id",
                table: "financial_record_books",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_record_books_status_period_start_period_end",
                table: "financial_record_books",
                columns: new[] { "status", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "ix_frb_assessment_rule_contribution_types_contribution_type_id",
                table: "frb_assessment_rule_contribution_types",
                column: "contribution_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_frb_assessment_rules_name_effective_from",
                table: "frb_assessment_rules",
                columns: new[] { "name", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_frb_assessment_snapshots_financial_record_book_id_source_ru",
                table: "frb_assessment_snapshots",
                columns: new[] { "financial_record_book_id", "source_rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_frb_aud_financial_record_book_id_performed_at_utc",
                table: "frb_aud",
                columns: new[] { "financial_record_book_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_frb_verifications_financial_record_book_id_officer_type",
                table: "frb_verifications",
                columns: new[] { "financial_record_book_id", "officer_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reference_number_configurations_reference_type_effective_fr",
                table: "reference_number_configurations",
                columns: new[] { "reference_type", "effective_from" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_record_book_contributions");

            migrationBuilder.DropTable(
                name: "frb_assessment_rule_contribution_types");

            migrationBuilder.DropTable(
                name: "frb_assessment_snapshots");

            migrationBuilder.DropTable(
                name: "frb_aud");

            migrationBuilder.DropTable(
                name: "frb_verifications");

            migrationBuilder.DropTable(
                name: "reference_number_configurations");

            migrationBuilder.DropTable(
                name: "reference_number_sequences");

            migrationBuilder.DropTable(
                name: "frb_assessment_rules");

            migrationBuilder.DropTable(
                name: "financial_record_books");

            migrationBuilder.DropIndex(
                name: "ix_contributions_finalized_financial_record_book_id",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "finalized_financial_record_book_id",
                table: "contributions");
        }
    }
}
