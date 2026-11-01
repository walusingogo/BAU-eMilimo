using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFunctionalAreasOfficeAppointmentsAndOrgScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "functional_area_id",
                table: "designations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_group_id",
                table: "committees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_section_id",
                table: "committees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "functional_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_functional_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "office_appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    functional_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_office_appointments", x => x.id);
                    table.CheckConstraint("ck_office_appointments_dates", "effective_to_utc IS NULL OR effective_to_utc > effective_from_utc");
                    table.CheckConstraint("ck_office_appointments_scope", "(scope_type = 'CONGREGATION' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'FUNCTIONAL_AREA' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'SECTION' AND section_id IS NOT NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'GROUP' AND section_id IS NULL AND group_id IS NOT NULL AND committee_id IS NULL) OR (scope_type = 'COMMITTEE' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NOT NULL) OR (scope_type = 'ENTITY' AND ((CASE WHEN section_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN group_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN committee_id IS NOT NULL THEN 1 ELSE 0 END)) = 1)");
                    table.ForeignKey(
                        name: "fk_office_appointments_committees_committee_id",
                        column: x => x.committee_id,
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_office_appointments_designations_designation_id",
                        column: x => x.designation_id,
                        principalTable: "designations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_office_appointments_functional_areas_functional_area_id",
                        column: x => x.functional_area_id,
                        principalTable: "functional_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_office_appointments_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_office_appointments_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_office_appointments_users_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_office_appointments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_groups_section_id",
                table: "groups",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_functional_area_id",
                table: "designations",
                column: "functional_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_committees_parent_group_id",
                table: "committees",
                column: "parent_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_committees_parent_section_id",
                table: "committees",
                column: "parent_section_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_committees_single_parent",
                table: "committees",
                sql: "NOT (parent_section_id IS NOT NULL AND parent_group_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_functional_areas_code",
                table: "functional_areas",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_assigned_by_id",
                table: "office_appointments",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_committee_id",
                table: "office_appointments",
                column: "committee_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_designation_id_functional_area_id_scope",
                table: "office_appointments",
                columns: new[] { "designation_id", "functional_area_id", "scope_type" });

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_functional_area_id",
                table: "office_appointments",
                column: "functional_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_group_id",
                table: "office_appointments",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_section_id",
                table: "office_appointments",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_appointments_user_id_is_active_effective_from_utc_ef",
                table: "office_appointments",
                columns: new[] { "user_id", "is_active", "effective_from_utc", "effective_to_utc" });

            migrationBuilder.AddForeignKey(
                name: "fk_committees_groups_parent_group_id",
                table: "committees",
                column: "parent_group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_committees_sections_parent_section_id",
                table: "committees",
                column: "parent_section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_designations_functional_areas_functional_area_id",
                table: "designations",
                column: "functional_area_id",
                principalTable: "functional_areas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_sections_section_id",
                table: "groups",
                column: "section_id",
                principalTable: "sections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                INSERT INTO functional_areas
                    (id, code, name, description, is_system, is_active, created_date, updated_date, version)
                VALUES
                    ('20000000-0000-0000-0000-000000000001','MINISTRY','Ministry',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000002','SECRETARIAT','Secretariat',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000003','ACCOUNTING','Accounting',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000004','TREASURY','Treasury',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000005','FINANCE','Finance',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000006','STEWARDSHIP','Stewardship',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000007','DWE','Doctrine, Worship and Evangelism',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000008','WCF','Women''s Christian Fellowship',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000009','MCF','Men''s Christian Fellowship',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000010','YOUTH','Youth Ministry',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000011','SUNDAY_SCHOOL','Sunday School',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000012','CHRISTIAN_TRAINING','Christian Training',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000013','MEMBERSHIP','Membership',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000014','CDSJC','Community Development and Social Justice',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000015','COMMUNICATIONS','Communications and Literature',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000016','DIACONAL','Diaconal',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000017','SECTION_ADMINISTRATION','Section Administration',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000018','CHURCH_OVERSIGHT','Church Oversight',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000019','AUDIT','Audit',NULL,TRUE,TRUE,NOW(),NOW(),1),
                    ('20000000-0000-0000-0000-000000000020','OTHER','Other',NULL,TRUE,TRUE,NOW(),NOW(),1)
                ON CONFLICT (code) DO NOTHING;

                UPDATE designations d SET functional_area_id = fa.id
                FROM functional_areas fa
                WHERE fa.code = d.functional_jurisdiction AND d.functional_area_id IS NULL;

                INSERT INTO logs (id, timestamp_utc, level, category, process, action, correlation_id, message, details)
                SELECT gen_random_uuid(), NOW(), 'Warning', 'Security', 'RBAC Migration', 'ManualReviewRequired',
                    gen_random_uuid()::text,
                    'Designation ' || d.code || ' used an unmapped functional jurisdiction and was assigned to OTHER.',
                    json_build_object('designationId', d.id, 'designationCode', d.code,
                                      'legacyFunctionalJurisdiction', d.functional_jurisdiction)::text
                FROM designations d WHERE d.functional_area_id IS NULL;

                UPDATE designations SET functional_area_id = (SELECT id FROM functional_areas WHERE code='OTHER')
                WHERE functional_area_id IS NULL;

                INSERT INTO designations
                    (id, code, name, required_role, functional_jurisdiction, functional_area_id, description,
                     capabilities_json, is_system, is_active, created_date, updated_date, version)
                SELECT gen_random_uuid(), seed.code, seed.name, seed.required_role, seed.area_code, fa.id,
                    seed.description, seed.capabilities, TRUE, TRUE, NOW(), NOW(), 1
                FROM (VALUES
                    ('CONVENER','Convener','SeniorRep','OTHER','Reusable convener office; scope and functional area belong to the appointment.','["FRB_VERIFY_STEWARDSHIP","FRB_ORGANIZATION_SCOPE"]'),
                    ('VICE_CONVENER','Vice Convener','SeniorRep','OTHER','Reusable vice-convener office.','[]'),
                    ('CHURCH_SECRETARY','Church Secretary','ChurchAuth','SECRETARIAT','Congregation-wide church secretariat office.','["REQUISITION_CHURCH_AUTHORITY"]'),
                    ('CHAIRPERSON','Chairperson','SeniorRep','OTHER','Reusable chairperson office.','[]'),
                    ('VICE_CHAIRPERSON','Vice Chairperson','SeniorRep','OTHER','Reusable vice-chairperson office.','[]'),
                    ('SECRETARY','Secretary','SeniorRep','OTHER','Reusable entity secretary office.','[]'),
                    ('VICE_SECRETARY','Vice Secretary','SeniorRep','OTHER','Reusable vice-secretary office.','[]'),
                    ('TEACHER','Teacher','GroupRep','OTHER','Reusable teaching office.','["REQUISITION_ENTITY_REVIEW"]'),
                    ('REPRESENTATIVE','Representative','SeniorRep','OTHER','Reusable representative office.','["REQUISITION_ENTITY_REVIEW"]'),
                    ('COMMITTEE_MEMBER','Committee Member','GroupRep','OTHER','Reusable committee member office.','[]'),
                    ('LEADER','Leader','GroupRep','OTHER','Reusable entity leader office.','[]'),
                    ('MEMBER','Member','GroupRep','OTHER','Reusable entity member office.','[]'),
                    ('OTHER','Other','GroupRep','OTHER','Maintained custom office designation.','[]')
                ) AS seed(code,name,required_role,area_code,description,capabilities)
                JOIN functional_areas fa ON fa.code = seed.area_code
                WHERE NOT EXISTS (SELECT 1 FROM designations d WHERE d.code = seed.code);

                INSERT INTO office_appointments
                    (id, user_id, designation_id, functional_area_id, scope_type, section_id, group_id, committee_id,
                     effective_from_utc, effective_to_utc, is_active, assigned_by_id, created_date, updated_date, version)
                SELECT gen_random_uuid(), uda.user_id,
                    CASE d.code WHEN 'STEWARDSHIP_CONVENER' THEN COALESCE((SELECT id FROM designations WHERE code='CONVENER'), d.id)
                                WHEN 'SUNDAY_SCHOOL_TEACHER' THEN COALESCE((SELECT id FROM designations WHERE code='TEACHER'), d.id)
                                ELSE d.id END,
                    fa.id,
                    CASE WHEN d.code IN ('MINISTER','ACCOUNTANT','TREASURER') THEN 'CONGREGATION'
                         WHEN d.code = 'STEWARDSHIP_CONVENER' THEN 'FUNCTIONAL_AREA'
                         WHEN d.code = 'ELDER' AND u.section_id IS NOT NULL THEN 'SECTION'
                         WHEN d.code IN ('STEWARD','SUNDAY_SCHOOL_TEACHER') AND u.group_id IS NOT NULL THEN 'GROUP'
                         ELSE 'FUNCTIONAL_AREA' END,
                    CASE WHEN d.code = 'ELDER' THEN u.section_id ELSE NULL END,
                    CASE WHEN d.code IN ('STEWARD','SUNDAY_SCHOOL_TEACHER') THEN u.group_id ELSE NULL END,
                    NULL, uda.effective_from_utc, uda.effective_to_utc,
                    uda.effective_to_utc IS NULL OR uda.effective_to_utc > NOW(), uda.assigned_by_id,
                    uda.assigned_at_utc, NOW(), 1
                FROM user_designation_assignments uda
                JOIN users u ON u.id = uda.user_id
                JOIN designations d ON d.id = uda.designation_id
                JOIN functional_areas fa ON fa.id = d.functional_area_id
                WHERE NOT EXISTS (SELECT 1 FROM office_appointments oa WHERE oa.user_id = uda.user_id
                                  AND oa.designation_id = CASE d.code WHEN 'STEWARDSHIP_CONVENER' THEN COALESCE((SELECT id FROM designations WHERE code='CONVENER'), d.id)
                                                                   WHEN 'SUNDAY_SCHOOL_TEACHER' THEN COALESCE((SELECT id FROM designations WHERE code='TEACHER'), d.id)
                                                                   ELSE d.id END);

                INSERT INTO office_appointments
                    (id, user_id, designation_id, functional_area_id, scope_type, effective_from_utc, is_active,
                     created_date, updated_date, version)
                SELECT gen_random_uuid(), u.id, d.id, fa.id, 'CONGREGATION', NOW(), TRUE, NOW(), NOW(), 1
                FROM users u
                JOIN designations d ON d.code = CASE u.role
                    WHEN 'Treasurer' THEN 'TREASURER'
                    WHEN 'Minister' THEN 'MINISTER'
                    WHEN 'Accountant' THEN 'ACCOUNTANT'
                    ELSE 'CHURCH_SECRETARY' END
                JOIN functional_areas fa ON fa.code = CASE u.role
                    WHEN 'Treasurer' THEN 'TREASURY'
                    WHEN 'Minister' THEN 'MINISTRY'
                    WHEN 'Accountant' THEN 'ACCOUNTING'
                    ELSE 'SECRETARIAT' END
                WHERE u.role IN ('Treasurer','Secretary','Minister','Accountant')
                  AND NOT EXISTS (SELECT 1 FROM office_appointments oa WHERE oa.user_id=u.id AND oa.designation_id=d.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_committees_groups_parent_group_id",
                table: "committees");

            migrationBuilder.DropForeignKey(
                name: "fk_committees_sections_parent_section_id",
                table: "committees");

            migrationBuilder.DropForeignKey(
                name: "fk_designations_functional_areas_functional_area_id",
                table: "designations");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_sections_section_id",
                table: "groups");

            migrationBuilder.DropTable(
                name: "office_appointments");

            migrationBuilder.DropTable(
                name: "functional_areas");

            migrationBuilder.DropIndex(
                name: "ix_groups_section_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_designations_functional_area_id",
                table: "designations");

            migrationBuilder.DropIndex(
                name: "ix_committees_parent_group_id",
                table: "committees");

            migrationBuilder.DropIndex(
                name: "ix_committees_parent_section_id",
                table: "committees");

            migrationBuilder.DropCheckConstraint(
                name: "ck_committees_single_parent",
                table: "committees");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "functional_area_id",
                table: "designations");

            migrationBuilder.DropColumn(
                name: "parent_group_id",
                table: "committees");

            migrationBuilder.DropColumn(
                name: "parent_section_id",
                table: "committees");
        }
    }
}
