using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFrbReferentialConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_frb_verifications_verified_by_id",
                table: "frb_verifications",
                column: "verified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_frb_assessment_snapshots_source_rule_id",
                table: "frb_assessment_snapshots",
                column: "source_rule_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contributions_financial_record_books_finalized_financial_re",
                table: "contributions",
                column: "finalized_financial_record_book_id",
                principalTable: "financial_record_books",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_frb_assessment_snapshots_frb_assessment_rules_source_rule_id",
                table: "frb_assessment_snapshots",
                column: "source_rule_id",
                principalTable: "frb_assessment_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_frb_aud_financial_record_books_financial_record_book_id",
                table: "frb_aud",
                column: "financial_record_book_id",
                principalTable: "financial_record_books",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_frb_verifications_users_verified_by_id",
                table: "frb_verifications",
                column: "verified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contributions_financial_record_books_finalized_financial_re",
                table: "contributions");

            migrationBuilder.DropForeignKey(
                name: "fk_frb_assessment_snapshots_frb_assessment_rules_source_rule_id",
                table: "frb_assessment_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_frb_aud_financial_record_books_financial_record_book_id",
                table: "frb_aud");

            migrationBuilder.DropForeignKey(
                name: "fk_frb_verifications_users_verified_by_id",
                table: "frb_verifications");

            migrationBuilder.DropIndex(
                name: "ix_frb_verifications_verified_by_id",
                table: "frb_verifications");

            migrationBuilder.DropIndex(
                name: "ix_frb_assessment_snapshots_source_rule_id",
                table: "frb_assessment_snapshots");
        }
    }
}
