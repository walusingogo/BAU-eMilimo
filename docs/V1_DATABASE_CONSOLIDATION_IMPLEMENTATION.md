# eMilimo v1.0.0 Database Consolidation — Implementation Status

Last verified: 2026-10-28 (development environment only)

## Safety and recovery

- Development database: `emilimo_db` on the local PostgreSQL instance.
- Pre-change custom-format backup:
  `artifacts/backups/emilimo_db-before-v1-consolidation-20261028-123532.dump`.
- The backup catalogue was verified with `pg_restore --list`.
- No production database or production infrastructure was changed.
- No tables were truncated. The reconciliations were clean, so clearing data was unnecessary.

## Completed migrations

| Migration | Result |
| --- | --- |
| `20261101130000_ConsolidateRetriesAndFinancialAdjustments` | Retry state retained on the payment aggregate; accounting reversals consolidated as financial adjustments. |
| `20261101140000_ConsolidateIntegrationPersistence` | Provider configuration, API exchanges and webhook evidence moved to the provider-neutral integration schema. |
| `20261101150000_ConsolidateSandboxPresets` | Sandbox sample presets consolidated into the scenario catalogue. |
| `20261101160000_CentralizeServiceCollectionContributions` | Every approved Service Collection item links to a central Contribution. |
| `20261101170000_CentralizeFrbServiceCollectionInputs` | FRBs calculate from central Contributions; Service Collection links remain provenance/presentation records. |
| `20261101180000_RenameDigitalPaymentsToDigitalCollections` | Existing payment aggregate renamed in place to `digital_collections`, `digital_collection_items`, and `digital_collection_events`. |
| `20261101190000_ConsolidateCommunicationDeliveryHistory` | Email, SMS, and WhatsApp delivery attempts unified in `communication_logs`; legacy `email_logs` removed after backfill. |
| `20261101200000_AddDigitalCollectionCompanions` | Added dedicated ATM/LCK/AUD companions for Digital Collection headers and their financial items. |

## Reconciliation evidence

### Service Collections

- Approved Service Collections: 3.
- Service Collection items: 7.
- Central Contributions linked: 7.
- Contribution allocations: 7.
- Central fund-ledger postings: 7.
- Total reconciled amount: ZMW 579.93.
- Duplicate posting keys: 0.
- Direct legacy `SERVICE_COLLECTION` ledger postings: 0.
- Invalid allocation totals: 0.

### Digital Collections

The rename migration performs only PostgreSQL table, column, index, and constraint renames.
It does not drop/recreate the aggregate or copy financial rows.

- Headers before/after: 9 / 9.
- Gross amount before/after: ZMW 142.78 / ZMW 142.78.
- Items before/after: 9 / 9.
- Item amount before/after: ZMW 142.78 / ZMW 142.78.
- Linked Contributions before/after: 2 / 2.
- Lifecycle events before/after: 62 / 62.
- Obsolete `digital_payment_*` aggregate tables remaining: 0.

### Communication delivery history

- Legacy email attempts migrated: 164 (91 successful, 73 failed).
- SMS attempts preserved: 62 (14 successful, 48 failed).
- WhatsApp attempts preserved: 72 (22 successful, 50 failed).
- Unified delivery records: 298.
- Retry relationships preserved: 34.
- Legacy `email_logs` table remaining: 0.
- Existing SMTP history/retry API response field names are preserved for frontend compatibility.

## Verification

- Release solution build: succeeded, 0 warnings, 0 errors.
- Backend automated tests: 116 passed, 0 failed, 0 skipped.
- EF model drift check: no pending model changes.
- Consolidation deployment script:
  `artifacts/migrations/eMilimo-v1-consolidation-1600-to-2000-idempotent.sql`.

## Remaining bounded phases

1. Continue ATM/LCK/AUD coverage only for remaining business aggregates; Digital Collections and central Contributions are covered. Do not create companions for audit evidence or infrastructure-only rows.
2. Remove obsolete migration/build artifacts from the release package while retaining the authoritative migration chain and recovery backup.
3. Run installer/deployment regression certification against a clean v1 database and an upgraded development database.
4. Produce the final v1 baseline only after upgraded-data and clean-install parity is proven.
