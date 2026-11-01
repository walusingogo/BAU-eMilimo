using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalGivingOrchestration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "contribution_source_id",
                table: "digital_payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "protected_public_status_token",
                table: "digital_payment_transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_fingerprint",
                table: "digital_payment_transactions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "member_verification_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lookup_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_fingerprint_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    otp_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    otp_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    maximum_attempts = table.Column<int>(type: "integer", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    destination_hint = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_verification_challenges", x => x.id);
                    table.CheckConstraint("ck_member_verification_attempt_count", "attempt_count >= 0 AND maximum_attempts > 0");
                    table.CheckConstraint("ck_member_verification_otp_material", "length(otp_salt) >= 16 AND length(otp_hash) >= 32");
                    table.ForeignKey(
                        name: "fk_member_verification_challenges_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Phase 2 did not expose a public intent API, but this backfill keeps an
            // upgrade safe if development or pilot data was created directly.
            migrationBuilder.Sql(
                """
                UPDATE digital_payment_transactions AS payment
                SET contribution_source_id = configuration.contribution_source_id
                FROM (
                    SELECT contribution_source_id
                    FROM digital_giving_configurations
                    ORDER BY created_date
                    LIMIT 1
                ) AS configuration
                WHERE payment.contribution_source_id IS NULL;
                """);
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM digital_payment_transactions
                        WHERE contribution_source_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot migrate existing digital payments: configure a Digital Giving contribution source first.';
                    END IF;
                END $$;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "contribution_source_id",
                table: "digital_payment_transactions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_payment_transactions_contribution_source_id",
                table: "digital_payment_transactions",
                column: "contribution_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_verification_challenges_expires_at_utc",
                table: "member_verification_challenges",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_member_verification_challenges_lookup_hash_created_date",
                table: "member_verification_challenges",
                columns: new[] { "lookup_hash", "created_date" });

            migrationBuilder.CreateIndex(
                name: "ix_member_verification_challenges_member_id",
                table: "member_verification_challenges",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_verification_challenges_verification_token_hash",
                table: "member_verification_challenges",
                column: "verification_token_hash",
                filter: "verification_token_hash IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_digital_payment_transactions_contribution_sources_contribut",
                table: "digital_payment_transactions",
                column: "contribution_source_id",
                principalTable: "contribution_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_digital_payment_transactions_contribution_sources_contribut",
                table: "digital_payment_transactions");

            migrationBuilder.DropTable(
                name: "member_verification_challenges");

            migrationBuilder.DropIndex(
                name: "ix_digital_payment_transactions_contribution_source_id",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "contribution_source_id",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "protected_public_status_token",
                table: "digital_payment_transactions");

            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                table: "digital_payment_transactions");
        }
    }
}
