# Financial Record Book controlled pilot

## Architecture

The FRB module closes the loop on existing Contributions; it does not capture money. `CONFIRMED` and `ALLOCATED` Contributions are eligible. All totals, assessment bases, rounding, workflow transitions, double-count validation and finalisation are authoritative in `FinancialRecordBookService`.

Existing platform facilities are reused for users, roles/capabilities, Section access, profile signatures, Contribution Types, process/system logs, A4 preview/PDF/print and Excel/CSV export. New FRB tables are necessary because an FRB is an immutable financial record with effective-dated calculation snapshots and three independent certifications, none of which an existing entity represents.

## Lifecycle and controls

- `DRAFT` / `RETURNED`: reporting period, Notes and selected Contributions are editable.
- `AWAITING_VERIFICATION`: calculations and rule details are snapshotted; financial content is locked.
- Stewardship Convener (`SeniorRep`), Treasurer and Minister sign independently using their existing profile signatures. The maker cannot sign their own FRB.
- `FINALIZED`: all three verifications and full reconciliation are required. Financial content is immutable.
- Drafts do not reserve Contributions. At finalisation, `contributions.finalized_financial_record_book_id` is assigned inside a serializable transaction. A filtered unique index and foreign key are the final collision/double-count safeguards.

Assessment rules are resolved at the FRB period end. For versions with the same name, the latest effective version wins. Submission snapshots each Contribution's reference, date, type, description, amount and status, plus every rule's applicable types, method, rate/fixed amount, beneficiary, base and result. Later edits to source records or configuration therefore cannot alter historical FRBs. Currency uses `decimal` and midpoint-away-from-zero rounding to two decimal places.

## Reference numbering

`ReferenceNumberService` is the only allocator for new Contribution, FRB, Requisition and Asset references. PostgreSQL atomically upserts a counter keyed by `(reference_type, sequence_date)` and returns the incremented value. Numbers use `[PREFIX][minimum-three-digit serial][yyyyMMdd]`; values above 999 expand naturally. The business date uses `Platform:TimeZone` (default `Africa/Lusaka`). Existing references are not rewritten.

Prefix changes are append-only, effective-dated configurations. The latest applicable configuration controls new records; an inactive latest configuration stops allocation. A prefix cannot be reused for another reference type.

## Pilot assumptions

- A Section represents the congregation-level organizational owner. Organization-wide FRBs require global access.
- Existing roles identify the three required officer classes because eMilimo does not yet maintain named officer-position assignments separately from roles. Section assignment additionally scopes a Stewardship Convener.
- Percentage and fixed assessment methods are enabled. Manual financial totals are deliberately excluded from the pilot because they would weaken deterministic reconciliation.
- Controlled correction/reversal of a finalised FRB is intentionally not exposed in this pilot. Finalised evidence is retained; a future correction workflow must preserve the original record and require explicit authorization.

## Principal endpoints

- `GET/POST api/frb`, `GET/PUT api/frb/{id}`
- `GET api/frb/eligible-contributions`
- `POST api/frb/{id}/submit|verify|finalize`
- `GET api/frb/{id}/audit`
- `GET/POST/PUT api/frb-settings/assessment-rules`
- `GET/POST api/frb-settings/reference-numbering`

## Pilot verification

Before go-live, exercise real PostgreSQL concurrency with multiple Sections creating each reference type simultaneously, race two FRBs finalising the same Contribution, validate all three real officer profiles/signatures, compare A4 preview/PDF/print on supported browsers, and reconcile a representative weekly FRB against the detailed Excel export.
