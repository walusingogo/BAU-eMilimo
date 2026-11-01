using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO designations
                    (id, code, name, required_role, functional_jurisdiction, description, is_system, is_active,
                     created_date, updated_date, version)
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 'STEWARDSHIP_CONVENER', 'Stewardship Convener', 'SeniorRep', 'STEWARDSHIP', 'Reviews stewardship items from every committee, section and group.', TRUE, TRUE, NOW(), NOW(), 1),
                    ('10000000-0000-0000-0000-000000000002', 'ELDER', 'Elder', 'SeniorRep', 'CHURCH_OVERSIGHT', 'Performs elder oversight duties across the organization.', TRUE, TRUE, NOW(), NOW(), 1),
                    ('10000000-0000-0000-0000-000000000003', 'STEWARD', 'Steward', 'GroupRep', 'STEWARDSHIP', 'Performs stewardship duties for assigned organizational membership.', TRUE, TRUE, NOW(), NOW(), 1),
                    ('10000000-0000-0000-0000-000000000004', 'SUNDAY_SCHOOL_TEACHER', 'Sunday School Teacher', 'GroupRep', 'SUNDAY_SCHOOL', 'Performs Sunday School duties for assigned organizational membership.', TRUE, TRUE, NOW(), NOW(), 1),
                    ('10000000-0000-0000-0000-000000000005', 'MINISTER', 'Minister', 'ChurchAuth', 'MINISTRY', 'Performs organization-wide ministerial duties.', TRUE, TRUE, NOW(), NOW(), 1),
                    ('10000000-0000-0000-0000-000000000006', 'TREASURER', 'Treasurer', 'ChurchAuth', 'TREASURY', 'Performs organization-wide treasury duties.', TRUE, TRUE, NOW(), NOW(), 1),
                    ('10000000-0000-0000-0000-000000000007', 'ACCOUNTANT', 'Accountant', 'ChurchAuth', 'ACCOUNTING', 'Performs organization-wide accounting duties.', TRUE, TRUE, NOW(), NOW(), 1)
                ON CONFLICT (code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM user_designation_assignments
                WHERE designation_id IN (
                    '10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002',
                    '10000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000004',
                    '10000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000006',
                    '10000000-0000-0000-0000-000000000007');
                DELETE FROM designations
                WHERE id IN (
                    '10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002',
                    '10000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000004',
                    '10000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000006',
                    '10000000-0000-0000-0000-000000000007');
                """);
        }
    }
}
