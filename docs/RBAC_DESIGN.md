# eMilimo RBAC and Workflow Authorization

## Model

eMilimo separates authorization into three layers:

1. **Role** — the user's authority tier and broad module access.
2. **Designation** — the functional appointment held by the user, such as Elder, Stewardship Convener, Minister, Treasurer, or Accountant.
3. **Scope** — the records the user may act on, derived from their group, section, committee, or organization-wide jurisdiction.

A user may see a module because of their role, but may perform a workflow action only when their active designation grants the required capability and the record is within their permitted scope. SuperAdmin remains the emergency platform-wide authority.

Minister, Treasurer, Accountant, and Secretary are offices represented by designations and effective-dated office appointments. They are not authority roles.

## Workflow capability matrix

| Capability | Process action | Typical designation / authority | Scope |
|---|---|---|---|
| `REQUISITION_ENTITY_REVIEW` | First-stage requisition review | Elder, Steward, Sunday School Teacher, or another maintained entity designation | Matching group, section, or committee jurisdiction |
| `REQUISITION_CHURCH_AUTHORITY` | Church-authority approval slot | ChurchAuth appointment with the capability | Appointment scope |
| `REQUISITION_TREASURER_APPROVAL` | Treasurer approval slot | Treasurer | Organization-wide |
| `REQUISITION_ACCOUNTANT_APPROVAL` | Accountant approval and money-ready action | Accountant designation under ChurchAuth | Organization-wide |
| `REQUISITION_MINISTER_APPROVAL` | Minister approval slot | Minister designation under ChurchAuth | Organization-wide |
| `CONTRIBUTION_REVIEW` | Confirm, reject, or return submitted contributions | Accountant designation or Treasurer | Existing contribution record scope |
| `FRB_MANAGE` | Create, edit, archive, and submit FRBs | Accountant designation or Treasurer | Existing FRB record scope |
| `FRB_VERIFY_STEWARDSHIP` | Stewardship verification | Stewardship Convener under SeniorRep | Organization-wide stewardship function |
| `FRB_VERIFY_TREASURER` | Treasurer verification | Treasurer | Organization-wide |
| `FRB_VERIFY_MINISTER` | Minister verification | Minister designation under ChurchAuth | Organization-wide |
| `FRB_FINALIZE` | Finalize a fully verified FRB | Treasurer | Organization-wide |
| `FRB_ORGANIZATION_SCOPE` | Read FRBs across entity boundaries | Stewardship Convener, Treasurer, Minister | Organization-wide |

System designations provide the default mapping. Administrators can maintain capability assignments in **System Settings → Designation Maintenance**. A designation's required role must match the user's current role, and both the designation and assignment must be active.

## Process review

### Requisitions

- Initiation remains a configurable role permission.
- Entity review is designation-controlled and still enforces entity membership and separation of duties.
- Treasurer, Accountant, Church Authority, and Minister approval slots use distinct capabilities.
- Marking money ready requires the Accountant capability.
- Collection is restricted to the original requisitor; financial authority comes from office appointments.

### Contributions

- Initiation remains a configurable role permission.
- Confirmation, rejection, and return require `CONTRIBUTION_REVIEW`.
- Existing record-level group/section/committee access remains in force.

### Financial Record Book

- Draft management, each verification office, finalization, and organization-wide visibility are separate capabilities.
- The UI derives the verification office from capability rather than a hard-coded role or designation code.
- Server-side checks remain authoritative for every transition.

### Assets and fleet

- Asset maintenance, request, approval, and reporting remain explicit role-permission matrix entries.
- Asset operations depend on module permissions and explicit office capabilities, not office names used as roles.
- Existing entity access and requester/approver separation rules remain in force.

### Reports, administration, and platform operations

- Reports remain read-only role permissions with record-level filtering.
- User, group, section, committee, settings, backups, authentication, communication, licensing, and platform administration remain role-controlled because they are administrative authorities rather than business workflow appointments.
- SuperAdmin-only safety controls remain role-bound.

## Enforcement rules

- Frontend visibility is advisory; every sensitive transition is checked again by the API.
- Role-permission rows cannot grant designation-controlled workflow actions. This prevents old database configuration from bypassing the capability model.
- Inactive users, inactive designations, expired assignments, and role/designation mismatches grant no capability.
- Notification recipients are resolved from the same capability source used for action authorization.
- New workflow steps must define a capability, map it to coarse UI/API permissions, enforce record scope in the service, and add tests for allowed and denied cases.

