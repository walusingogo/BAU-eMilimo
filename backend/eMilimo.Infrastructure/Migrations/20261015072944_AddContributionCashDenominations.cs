using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionCashDenominations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration can encounter an orphaned table when an earlier startup was
            // interrupted after executing the DDL but before EF recorded the migration.
            // PostgreSQL's IF NOT EXISTS makes the repair safe without deleting cash data.
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS contribution_denominations (
                    id uuid NOT NULL,
                    contribution_id uuid NOT NULL,
                    denomination numeric(18,4) NOT NULL,
                    quantity integer NOT NULL,
                    amount numeric(18,2) NOT NULL,
                    created_date timestamp with time zone NOT NULL,
                    updated_date timestamp with time zone NOT NULL,
                    last_processed_time_stamp timestamp with time zone NULL,
                    created_by_id uuid NULL,
                    updated_by_id uuid NULL,
                    version integer NOT NULL,
                    CONSTRAINT pk_contribution_denominations PRIMARY KEY (id),
                    CONSTRAINT ck_contribution_denominations_amount CHECK (amount > 0),
                    CONSTRAINT ck_contribution_denominations_denomination CHECK (denomination > 0),
                    CONSTRAINT ck_contribution_denominations_quantity CHECK (quantity > 0),
                    CONSTRAINT fk_contribution_denominations_contributions_contribution_id
                        FOREIGN KEY (contribution_id) REFERENCES contributions (id) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_contribution_denominations_contribution_id_denomination
                    ON contribution_denominations (contribution_id, denomination);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contribution_denominations");
        }
    }
}
