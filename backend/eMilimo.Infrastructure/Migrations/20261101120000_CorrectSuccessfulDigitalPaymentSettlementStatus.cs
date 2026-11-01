using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261101120000_CorrectSuccessfulDigitalPaymentSettlementStatus")]
public partial class CorrectSuccessfulDigitalPaymentSettlementStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE digital_payment_transactions AS payment
            SET settlement_status = 'SETTLED',
                updated_date = NOW(),
                version = version + 1
            WHERE payment.status = 'SUCCEEDED'
              AND payment.settlement_status = 'PENDING'
              AND EXISTS (
                  SELECT 1
                  FROM digital_payment_lines AS line
                  WHERE line.digital_payment_transaction_id = payment.id)
              AND NOT EXISTS (
                  SELECT 1
                  FROM digital_payment_lines AS line
                  WHERE line.digital_payment_transaction_id = payment.id
                    AND line.contribution_id IS NULL);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is a corrective data migration. A successful, fully posted payment
        // must not be returned to PENDING when rolling application code back.
    }
}
