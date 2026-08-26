using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseKeyRotationTrust : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "license_key_rotation_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    previous_key_id = table.Column<string>(type: "text", nullable: true),
                    new_key_id = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<string>(type: "text", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    package_hash = table.Column<string>(type: "text", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_license_key_rotation_audits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "license_trusted_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    key_id = table.Column<string>(type: "text", nullable: false),
                    public_key_pem = table.Column<string>(type: "text", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    rotation_sequence = table.Column<long>(type: "bigint", nullable: false),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    installed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    installed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rotation_package_hash = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_license_trusted_keys", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_license_key_rotation_audits_package_hash",
                table: "license_key_rotation_audits",
                column: "package_hash");

            migrationBuilder.CreateIndex(
                name: "ix_license_key_rotation_audits_product_code_sequence",
                table: "license_key_rotation_audits",
                columns: new[] { "product_code", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_license_trusted_keys_product_code_key_id",
                table: "license_trusted_keys",
                columns: new[] { "product_code", "key_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_license_trusted_keys_product_code_rotation_sequence",
                table: "license_trusted_keys",
                columns: new[] { "product_code", "rotation_sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "license_key_rotation_audits");

            migrationBuilder.DropTable(
                name: "license_trusted_keys");
        }
    }
}
