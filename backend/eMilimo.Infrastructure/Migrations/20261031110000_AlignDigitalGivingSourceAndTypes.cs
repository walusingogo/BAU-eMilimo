using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261031110000_AlignDigitalGivingSourceAndTypes")]
public partial class AlignDigitalGivingSourceAndTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO contribution_sources
                (id, name, description, is_active, created_date, updated_date, version)
            SELECT
                '9d4333a0-c4fa-4cb8-8e22-10f41b508e87'::uuid,
                'Digital Giving',
                'Verified contributions received through the public Digital Giving channel.',
                TRUE,
                NOW(),
                NOW(),
                1
            WHERE NOT EXISTS (
                SELECT 1 FROM contribution_sources WHERE upper(name) = 'DIGITAL GIVING');

            UPDATE digital_giving_configurations
            SET contribution_source_id = (
                SELECT id FROM contribution_sources
                WHERE upper(name) = 'DIGITAL GIVING' AND is_active
                ORDER BY created_date
                LIMIT 1);

            UPDATE contribution_types AS ct
            SET is_enabled_for_digital_giving = TRUE,
                allow_section_allocation = ct.allow_section_allocation OR EXISTS (
                    SELECT 1 FROM digital_giving_type_settings AS s
                    WHERE s.contribution_type_id = ct.id
                      AND s.is_publicly_available
                      AND s.section_id IS NOT NULL),
                allow_group_allocation = ct.allow_group_allocation OR EXISTS (
                    SELECT 1 FROM digital_giving_type_settings AS s
                    WHERE s.contribution_type_id = ct.id
                      AND s.is_publicly_available
                      AND s.group_id IS NOT NULL),
                allow_committee_allocation = ct.allow_committee_allocation OR EXISTS (
                    SELECT 1 FROM digital_giving_type_settings AS s
                    WHERE s.contribution_type_id = ct.id
                      AND s.is_publicly_available
                      AND s.committee_id IS NOT NULL)
            WHERE EXISTS (
                SELECT 1 FROM digital_giving_type_settings AS s
                WHERE s.contribution_type_id = ct.id AND s.is_publicly_available);

            INSERT INTO payment_method_definitions
                (id, code, name, category, requires_reference, requires_bank_account,
                 requires_attachment, supports_foreign_currency, is_active,
                 display_order, created_date, updated_date, version)
            SELECT
                '9191acdd-3163-4ef7-b354-2b25b3792c73'::uuid,
                'ZAMTEL_MONEY',
                'Zamtel Money',
                'DIGITAL',
                TRUE, FALSE, FALSE, FALSE, FALSE,
                33, NOW(), NOW(), 1
            WHERE NOT EXISTS (
                SELECT 1 FROM payment_method_definitions WHERE code = 'ZAMTEL_MONEY');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The source can be referenced by posted financial records and is retained.
    }
}
