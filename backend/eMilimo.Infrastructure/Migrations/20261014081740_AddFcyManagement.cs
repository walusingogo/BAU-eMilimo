using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFcyManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    is_base_currency = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_currencies", x => x.id);
                    table.UniqueConstraint("ak_currencies_code", x => x.code);
                    table.CheckConstraint("ck_currencies_decimal_places", "decimal_places BETWEEN 0 AND 6");
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    to_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(28,12)", precision: 28, scale: 12, nullable: false),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_rates", x => x.id);
                    table.CheckConstraint("ck_exchange_rates_dates", "expires_at_utc IS NULL OR expires_at_utc > effective_at_utc");
                    table.CheckConstraint("ck_exchange_rates_pair", "from_currency_code <> to_currency_code");
                    table.CheckConstraint("ck_exchange_rates_positive", "rate > 0");
                    table.ForeignKey(
                        name: "fk_exchange_rates_currencies_from_currency_code",
                        column: x => x.from_currency_code,
                        principalTable: "currencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_rates_currencies_to_currency_code",
                        column: x => x.to_currency_code,
                        principalTable: "currencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_rates_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fcy_management_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    base_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    rate_source_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    provider_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    allow_manual_rate_override = table.Column<bool>(type: "boolean", nullable: false),
                    require_approval_for_manual_override = table.Column<bool>(type: "boolean", nullable: false),
                    allow_last_known_rate_fallback = table.Column<bool>(type: "boolean", nullable: false),
                    maximum_rate_age_hours = table.Column<int>(type: "integer", nullable: false),
                    override_tolerance_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fcy_management_settings", x => x.id);
                    table.CheckConstraint("ck_fcy_settings_override_tolerance", "override_tolerance_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_fcy_settings_rate_age", "maximum_rate_age_hours BETWEEN 1 AND 8760");
                    table.ForeignKey(
                        name: "fk_fcy_management_settings_currencies_base_currency_code",
                        column: x => x.base_currency_code,
                        principalTable: "currencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_currencies_code",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_approved_by_id",
                table: "exchange_rates",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_from_currency_code_to_currency_code_effectiv",
                table: "exchange_rates",
                columns: new[] { "from_currency_code", "to_currency_code", "effective_at_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_from_currency_code_to_currency_code_is_appro",
                table: "exchange_rates",
                columns: new[] { "from_currency_code", "to_currency_code", "is_approved", "effective_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_to_currency_code",
                table: "exchange_rates",
                column: "to_currency_code");

            migrationBuilder.CreateIndex(
                name: "ix_fcy_management_settings_base_currency_code",
                table: "fcy_management_settings",
                column: "base_currency_code");

            var seededAt = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "currencies",
                columns: new[] { "id", "code", "name", "symbol", "decimal_places", "is_base_currency", "is_active", "created_date", "updated_date", "version" },
                values: new object[,]
                {
                    { Guid.Parse("10000000-0000-0000-0000-000000000001"), "ZMW", "Zambian Kwacha", "K", 2, true, true, seededAt, seededAt, 1 },
                    { Guid.Parse("10000000-0000-0000-0000-000000000002"), "USD", "United States Dollar", "$", 2, false, true, seededAt, seededAt, 1 },
                    { Guid.Parse("10000000-0000-0000-0000-000000000003"), "EUR", "Euro", "€", 2, false, true, seededAt, seededAt, 1 },
                    { Guid.Parse("10000000-0000-0000-0000-000000000004"), "GBP", "Pound Sterling", "£", 2, false, true, seededAt, seededAt, 1 },
                    { Guid.Parse("10000000-0000-0000-0000-000000000005"), "ZAR", "South African Rand", "R", 2, false, true, seededAt, seededAt, 1 }
                });
            migrationBuilder.InsertData(
                table: "fcy_management_settings",
                columns: new[] { "id", "is_enabled", "base_currency_code", "rate_source_mode", "provider_code", "allow_manual_rate_override", "require_approval_for_manual_override", "allow_last_known_rate_fallback", "maximum_rate_age_hours", "override_tolerance_percent", "created_date", "updated_date", "version" },
                values: new object[] { Guid.Parse("20000000-0000-0000-0000-000000000001"), false, "ZMW", "MANUAL_ONLY", null!, false, true, true, 24, 2m, seededAt, seededAt, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "fcy_management_settings");

            migrationBuilder.DropTable(
                name: "currencies");
        }
    }
}
