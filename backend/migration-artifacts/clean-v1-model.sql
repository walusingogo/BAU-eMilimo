START TRANSACTION;

DROP TABLE user_designation_assignments;

ALTER TABLE frb_assessment_snapshots DROP COLUMN beneficiary;

ALTER TABLE frb_assessment_snapshots DROP COLUMN fixed_amount;

ALTER TABLE frb_assessment_snapshots DROP COLUMN rate;

ALTER TABLE frb_assessment_rules DROP COLUMN beneficiary;

ALTER TABLE frb_assessment_rules DROP COLUMN fixed_amount;

ALTER TABLE frb_assessment_rules DROP COLUMN rate;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261008075336_CleanV1Model', '8.0.10');

COMMIT;

