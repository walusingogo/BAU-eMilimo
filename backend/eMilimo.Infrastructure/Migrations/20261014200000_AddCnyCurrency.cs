using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014200000_AddCnyCurrency")]
public partial class AddCnyCurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO currencies
                (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
            VALUES
                ('10000000-0000-0000-0000-000000000006', 'CNY', 'Chinese Yuan', '¥', 2, FALSE, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
            ON CONFLICT (code) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM currencies
            WHERE id = '10000000-0000-0000-0000-000000000006'
              AND code = 'CNY'
              AND NOT EXISTS (SELECT 1 FROM exchange_rates WHERE from_currency_code = 'CNY' OR to_currency_code = 'CNY');
            """);
    }
}
