using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetireMinisterAndAccountantRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO user_designation_assignments
                    (user_id, designation_id, is_primary, effective_from_utc, effective_to_utc, assigned_by_id, assigned_at_utc)
                SELECT id, '10000000-0000-0000-0000-000000000005', TRUE, NOW(), NULL, NULL, NOW()
                FROM users WHERE role = 'Minister'
                ON CONFLICT (user_id, designation_id) DO NOTHING;

                INSERT INTO user_designation_assignments
                    (user_id, designation_id, is_primary, effective_from_utc, effective_to_utc, assigned_by_id, assigned_at_utc)
                SELECT id, '10000000-0000-0000-0000-000000000007', TRUE, NOW(), NULL, NULL, NOW()
                FROM users WHERE role = 'Accountant'
                ON CONFLICT (user_id, designation_id) DO NOTHING;

                UPDATE users SET role = 'ChurchAuth', updated_date = NOW(), version = version + 1
                WHERE role IN ('Minister', 'Accountant');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users SET role = 'Minister', updated_date = NOW(), version = version + 1
                WHERE role = 'ChurchAuth' AND id IN (
                    SELECT user_id FROM user_designation_assignments
                    WHERE designation_id = '10000000-0000-0000-0000-000000000005' AND assigned_by_id IS NULL);

                UPDATE users SET role = 'Accountant', updated_date = NOW(), version = version + 1
                WHERE role = 'ChurchAuth' AND id IN (
                    SELECT user_id FROM user_designation_assignments
                    WHERE designation_id = '10000000-0000-0000-0000-000000000007' AND assigned_by_id IS NULL);

                DELETE FROM user_designation_assignments
                WHERE designation_id IN ('10000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000007')
                  AND assigned_by_id IS NULL;
                """);
        }
    }
}
