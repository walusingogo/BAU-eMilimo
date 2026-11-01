using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CentralizeFrbServiceCollectionInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM financial_record_book_service_collections frb_sc
                        JOIN service_collection_entries entry
                          ON entry.service_collection_id = frb_sc.service_collection_id
                        JOIN service_collection_entry_lines line
                          ON line.service_collection_entry_id = entry.id
                        WHERE line.contribution_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot centralize FRB Service Collection inputs because one or more selected lines have no central Contribution.';
                    END IF;
                END $$;

                INSERT INTO financial_record_book_contributions (
                    id,
                    financial_record_book_id,
                    contribution_id,
                    created_date,
                    updated_date,
                    created_by_id,
                    updated_by_id,
                    version,
                    amount_snapshot,
                    contribution_date_snapshot,
                    contribution_number_snapshot,
                    contribution_type_id_snapshot,
                    contribution_type_name_snapshot,
                    description_snapshot,
                    status_snapshot,
                    lines_snapshot_json)
                SELECT
                    md5(frb_sc.financial_record_book_id::text || line.contribution_id::text)::uuid,
                    frb_sc.financial_record_book_id,
                    line.contribution_id,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP,
                    frb.created_by_id,
                    frb.updated_by_id,
                    1,
                    contribution.amount,
                    contribution.contribution_date::date,
                    contribution.contribution_number,
                    contribution.contribution_type_id,
                    contribution_type.name,
                    contribution.description,
                    contribution.status,
                    NULL
                FROM financial_record_book_service_collections frb_sc
                JOIN financial_record_books frb ON frb.id = frb_sc.financial_record_book_id
                JOIN service_collection_entries entry ON entry.service_collection_id = frb_sc.service_collection_id
                JOIN service_collection_entry_lines line ON line.service_collection_entry_id = entry.id
                JOIN contributions contribution ON contribution.id = line.contribution_id
                JOIN contribution_types contribution_type ON contribution_type.id = contribution.contribution_type_id
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM financial_record_book_contributions existing
                    WHERE existing.financial_record_book_id = frb_sc.financial_record_book_id
                      AND existing.contribution_id = line.contribution_id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM financial_record_book_contributions link
                USING financial_record_book_service_collections frb_sc,
                      service_collection_entries entry,
                      service_collection_entry_lines line
                WHERE entry.service_collection_id = frb_sc.service_collection_id
                  AND line.service_collection_entry_id = entry.id
                  AND link.financial_record_book_id = frb_sc.financial_record_book_id
                  AND link.contribution_id = line.contribution_id
                  AND link.id = md5(frb_sc.financial_record_book_id::text || line.contribution_id::text)::uuid;
                """);
        }
    }
}
