START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    ALTER TABLE payment_api_exchanges ADD mtn_sandbox_test_execution_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE TABLE mtn_sandbox_scenarios (
        id uuid NOT NULL,
        scenario_code character varying(80) NOT NULL,
        name character varying(120) NOT NULL,
        description character varying(500) NOT NULL,
        msisdn character varying(20) NOT NULL,
        expected_transaction_status character varying(30) NOT NULL,
        expected_http_response_code integer,
        expected_mtn_error_code character varying(100),
        test_amount numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        external_reference_prefix character varying(40) NOT NULL,
        payer_message character varying(160) NOT NULL,
        payee_note character varying(160) NOT NULL,
        expected_processing_behaviour character varying(50) NOT NULL,
        maximum_polling_attempts integer NOT NULL,
        polling_interval_seconds integer NOT NULL,
        is_active boolean NOT NULL,
        source_classification character varying(30) NOT NULL,
        source_reference character varying(500),
        is_system_default boolean NOT NULL,
        configuration_version integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_mtn_sandbox_scenarios PRIMARY KEY (id),
        CONSTRAINT ck_mtn_sandbox_scenario_amount CHECK (test_amount > 0),
        CONSTRAINT ck_mtn_sandbox_scenario_configuration_version CHECK (configuration_version > 0),
        CONSTRAINT ck_mtn_sandbox_scenario_polling CHECK (maximum_polling_attempts > 0 AND polling_interval_seconds > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE TABLE mtn_sandbox_sample_transactions (
        id uuid NOT NULL,
        name character varying(120) NOT NULL,
        contribution_type_id uuid,
        contribution_type_name character varying(120) NOT NULL,
        amount numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        test_msisdn character varying(20) NOT NULL,
        external_reference_format character varying(120) NOT NULL,
        payer_message character varying(160) NOT NULL,
        payee_note character varying(160) NOT NULL,
        mtn_sandbox_scenario_id uuid NOT NULL,
        is_active boolean NOT NULL,
        is_system_default boolean NOT NULL,
        configuration_version integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_mtn_sandbox_sample_transactions PRIMARY KEY (id),
        CONSTRAINT ck_mtn_sandbox_sample_amount CHECK (amount > 0),
        CONSTRAINT ck_mtn_sandbox_sample_configuration_version CHECK (configuration_version > 0),
        CONSTRAINT fk_mtn_sandbox_sample_transactions_contribution_types_contribu FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE SET NULL,
        CONSTRAINT fk_mtn_sandbox_sample_transactions_mtn_sandbox_scenarios_mtn_s FOREIGN KEY (mtn_sandbox_scenario_id) REFERENCES mtn_sandbox_scenarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE TABLE mtn_sandbox_test_executions (
        id uuid NOT NULL,
        payment_provider_configuration_id uuid NOT NULL,
        mtn_sandbox_scenario_id uuid NOT NULL,
        mtn_sandbox_sample_transaction_id uuid,
        transaction_uuid uuid NOT NULL,
        external_reference character varying(120) NOT NULL,
        test_msisdn character varying(20) NOT NULL,
        amount numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        expected_status character varying(30) NOT NULL,
        expected_error_code character varying(100),
        expected_http_status_code integer,
        actual_status character varying(30) NOT NULL,
        actual_error_code character varying(100),
        actual_http_status_code integer,
        verdict character varying(30) NOT NULL,
        execution_status character varying(30) NOT NULL,
        provider_configuration_version integer NOT NULL,
        polling_attempt_count integer NOT NULL,
        maximum_polling_attempts integer NOT NULL,
        polling_interval_seconds integer NOT NULL,
        next_poll_at_utc timestamp with time zone,
        started_at_utc timestamp with time zone NOT NULL,
        completed_at_utc timestamp with time zone,
        provider_message character varying(1000),
        correlation_id character varying(100),
        sanitized_request_preview jsonb NOT NULL,
        timeline_json jsonb NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_mtn_sandbox_test_executions PRIMARY KEY (id),
        CONSTRAINT ck_mtn_sandbox_execution_amount CHECK (amount > 0),
        CONSTRAINT ck_mtn_sandbox_execution_polling CHECK (polling_attempt_count >= 0 AND maximum_polling_attempts > 0 AND polling_interval_seconds > 0),
        CONSTRAINT fk_mtn_sandbox_test_executions_mtn_sandbox_sample_transactions FOREIGN KEY (mtn_sandbox_sample_transaction_id) REFERENCES mtn_sandbox_sample_transactions (id) ON DELETE SET NULL,
        CONSTRAINT fk_mtn_sandbox_test_executions_mtn_sandbox_scenarios_mtn_sandb FOREIGN KEY (mtn_sandbox_scenario_id) REFERENCES mtn_sandbox_scenarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_mtn_sandbox_test_executions_payment_provider_configurations FOREIGN KEY (payment_provider_configuration_id) REFERENCES payment_provider_configurations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_payment_api_exchanges_mtn_sandbox_test_execution_id ON payment_api_exchanges (mtn_sandbox_test_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_sample_transactions_contribution_type_id ON mtn_sandbox_sample_transactions (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_sample_transactions_mtn_sandbox_scenario_id_is_ ON mtn_sandbox_sample_transactions (mtn_sandbox_scenario_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE UNIQUE INDEX ix_mtn_sandbox_sample_transactions_name ON mtn_sandbox_sample_transactions (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_scenarios_is_active_name ON mtn_sandbox_scenarios (is_active, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE UNIQUE INDEX ix_mtn_sandbox_scenarios_scenario_code ON mtn_sandbox_scenarios (scenario_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_test_executions_execution_status_next_poll_at_u ON mtn_sandbox_test_executions (execution_status, next_poll_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE UNIQUE INDEX ix_mtn_sandbox_test_executions_external_reference ON mtn_sandbox_test_executions (external_reference);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_test_executions_mtn_sandbox_sample_transaction_ ON mtn_sandbox_test_executions (mtn_sandbox_sample_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_test_executions_mtn_sandbox_scenario_id_started ON mtn_sandbox_test_executions (mtn_sandbox_scenario_id, started_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE INDEX ix_mtn_sandbox_test_executions_payment_provider_configuration_ ON mtn_sandbox_test_executions (payment_provider_configuration_id, started_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    CREATE UNIQUE INDEX ix_mtn_sandbox_test_executions_transaction_uuid ON mtn_sandbox_test_executions (transaction_uuid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    ALTER TABLE payment_api_exchanges ADD CONSTRAINT fk_payment_api_exchanges_mtn_sandbox_test_executions_mtn_sandb FOREIGN KEY (mtn_sandbox_test_execution_id) REFERENCES mtn_sandbox_test_executions (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101110000_AddMtnSandboxTestingCatalog') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261101110000_AddMtnSandboxTestingCatalog', '8.0.10');
    END IF;
END $EF$;
COMMIT;

