CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE bank_accounts (
        id uuid NOT NULL,
        bank_name character varying(100) NOT NULL,
        account_name character varying(255) NOT NULL,
        account_number character varying(50) NOT NULL,
        branch character varying(100),
        account_type character varying(50),
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_bank_accounts PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE contribution_sources (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description text,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_contribution_sources PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE contribution_types (
        id uuid NOT NULL,
        code character varying(20) NOT NULL,
        name character varying(100) NOT NULL,
        description text,
        is_active boolean NOT NULL,
        display_order integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_contribution_types PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE sections (
        id uuid NOT NULL,
        section_number integer NOT NULL,
        section_name text NOT NULL,
        description text,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_sections PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE system_configs (
        id uuid NOT NULL,
        config_key text NOT NULL,
        config_value text NOT NULL,
        description text,
        is_archived boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_system_configs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE contribution_allocations (
        id uuid NOT NULL,
        contribution_id uuid NOT NULL,
        group_id uuid NOT NULL,
        allocated_amount numeric(15,2) NOT NULL,
        percentage_allocation numeric(5,2),
        is_primary boolean NOT NULL,
        notes text,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_contribution_allocations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE contribution_atms (
        id uuid NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        file_name text NOT NULL,
        file_path text NOT NULL,
        mime_type text NOT NULL,
        size bigint NOT NULL,
        uploaded_by_id uuid,
        uploaded_at timestamp with time zone NOT NULL,
        description text,
        CONSTRAINT pk_contribution_atms PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE contribution_lcks (
        id uuid NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        locked_by_id uuid NOT NULL,
        locked_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        lock_reason text,
        CONSTRAINT pk_contribution_lcks PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE contributions (
        id uuid NOT NULL,
        contribution_number character varying(50) NOT NULL,
        contribution_type_id uuid NOT NULL,
        source_id uuid NOT NULL,
        amount numeric(15,2) NOT NULL,
        contribution_date timestamp with time zone NOT NULL,
        description text,
        reference_number character varying(100),
        payment_method character varying(50) NOT NULL,
        bank_account_id uuid,
        status character varying(20) NOT NULL,
        is_split boolean NOT NULL,
        received_by uuid,
        confirmed_by uuid,
        confirmed_at timestamp with time zone,
        is_archived boolean NOT NULL,
        archived_at timestamp with time zone,
        archived_by uuid,
        rejection_reason text,
        archive_reason text,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_contributions PRIMARY KEY (id),
        CONSTRAINT fk_contributions_bank_accounts_bank_account_id FOREIGN KEY (bank_account_id) REFERENCES bank_accounts (id),
        CONSTRAINT fk_contributions_contribution_sources_source_id FOREIGN KEY (source_id) REFERENCES contribution_sources (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contributions_contribution_types_contribution_type_id FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE fund_balances (
        id uuid NOT NULL,
        group_id uuid NOT NULL,
        fiscal_year integer NOT NULL,
        budgeted_balance numeric NOT NULL,
        own_funds_balance numeric NOT NULL,
        committed_budgeted_balance numeric NOT NULL,
        committed_own_funds_balance numeric NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_fund_balances PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE fund_transactions (
        id uuid NOT NULL,
        group_id uuid NOT NULL,
        transaction_type character varying(50) NOT NULL,
        fund_type character varying(50) NOT NULL,
        amount numeric(15,2) NOT NULL,
        previous_balance numeric(15,2) NOT NULL,
        new_balance numeric(15,2) NOT NULL,
        description text NOT NULL,
        source character varying(255),
        reference_type character varying(50),
        reference_id uuid,
        metadata jsonb,
        is_reversed boolean NOT NULL,
        reversed_by uuid,
        reversed_at timestamp with time zone,
        reversal_reason text,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_fund_transactions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE groups (
        id uuid NOT NULL,
        group_name text NOT NULL,
        description text,
        group_type text NOT NULL,
        is_active boolean NOT NULL,
        leader_id uuid,
        parent_group_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_groups PRIMARY KEY (id),
        CONSTRAINT fk_groups_groups_parent_group_id FOREIGN KEY (parent_group_id) REFERENCES groups (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        title text NOT NULL,
        first_name text NOT NULL,
        middle_name text,
        last_name text NOT NULL,
        gender text NOT NULL,
        profile_photo character varying(4000),
        email text NOT NULL,
        phone_number text NOT NULL,
        membership_id text NOT NULL,
        password_hash text NOT NULL,
        password_expiry_date timestamp with time zone,
        password_expired boolean NOT NULL,
        password_attempts integer NOT NULL,
        password_lock_date timestamp with time zone,
        locked boolean NOT NULL,
        role text NOT NULL,
        status text NOT NULL,
        section_id uuid,
        group_id uuid,
        signature_data character varying(4000) NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id),
        CONSTRAINT fk_users_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE SET NULL,
        CONSTRAINT fk_users_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE SET NULL,
        CONSTRAINT fk_users_users_created_by_id FOREIGN KEY (created_by_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_users_users_updated_by_id FOREIGN KEY (updated_by_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE requisitions (
        id uuid NOT NULL,
        requisition_number text NOT NULL,
        group_id uuid,
        requisitor_id uuid,
        approved_by_id uuid,
        money_ready_by_id uuid,
        collected_by_id uuid,
        archived_by_id uuid,
        payee text NOT NULL,
        payment_method text NOT NULL,
        fund_source text NOT NULL,
        amount numeric NOT NULL,
        amount_words text NOT NULL,
        payment_details text NOT NULL,
        status text NOT NULL,
        rejection_reason text,
        approved_at timestamp with time zone,
        money_ready_at timestamp with time zone,
        collected_at timestamp with time zone,
        collection_signature text,
        completed_at timestamp with time zone,
        is_archived boolean NOT NULL,
        archived_at timestamp with time zone,
        archive_reason text,
        first_senior_approver_id uuid,
        second_senior_approver_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL DEFAULT 1,
        CONSTRAINT pk_requisitions PRIMARY KEY (id),
        CONSTRAINT fk_requisitions_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE SET NULL,
        CONSTRAINT fk_requisitions_users_approved_by_id FOREIGN KEY (approved_by_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT fk_requisitions_users_archived_by_id FOREIGN KEY (archived_by_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT fk_requisitions_users_collected_by_id FOREIGN KEY (collected_by_id) REFERENCES users (id),
        CONSTRAINT fk_requisitions_users_money_ready_by_id FOREIGN KEY (money_ready_by_id) REFERENCES users (id),
        CONSTRAINT fk_requisitions_users_requisitor_id FOREIGN KEY (requisitor_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE requisition_atms (
        id uuid NOT NULL,
        requisition_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        file_name text NOT NULL,
        file_path text NOT NULL,
        mime_type text NOT NULL,
        size bigint NOT NULL,
        uploaded_by_id uuid,
        uploaded_at timestamp with time zone NOT NULL,
        description text,
        CONSTRAINT pk_requisition_atms PRIMARY KEY (id),
        CONSTRAINT fk_requisition_atms_requisitions_entity_id FOREIGN KEY (entity_id) REFERENCES requisitions (id) ON DELETE CASCADE,
        CONSTRAINT fk_requisition_atms_requisitions_requisition_id FOREIGN KEY (requisition_id) REFERENCES requisitions (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE TABLE requisition_items (
        id uuid NOT NULL,
        requisition_id uuid NOT NULL,
        description text NOT NULL,
        quantity integer NOT NULL,
        unit_price numeric NOT NULL,
        total numeric NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_requisition_items PRIMARY KEY (id),
        CONSTRAINT fk_requisition_items_requisitions_requisition_id FOREIGN KEY (requisition_id) REFERENCES requisitions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_bank_accounts_account_number ON bank_accounts (account_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contribution_allocations_contribution_id ON contribution_allocations (contribution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contribution_allocations_created_by_id ON contribution_allocations (created_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contribution_allocations_group_id ON contribution_allocations (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contribution_atms_entity_id ON contribution_atms (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_contribution_lcks_entity_id ON contribution_lcks (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contribution_lcks_locked_by_id ON contribution_lcks (locked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_contribution_types_code ON contribution_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_bank_account_id ON contributions (bank_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_confirmed_by ON contributions (confirmed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_contribution_date ON contributions (contribution_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_contributions_contribution_number ON contributions (contribution_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_contribution_type_id ON contributions (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_created_by_id ON contributions (created_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_received_by ON contributions (received_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_source_id ON contributions (source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_status ON contributions (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_contributions_updated_by_id ON contributions (updated_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_fund_balances_group_id_fiscal_year ON fund_balances (group_id, fiscal_year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_fund_transactions_created_by_id ON fund_transactions (created_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_fund_transactions_created_date ON fund_transactions (created_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_fund_transactions_group_id ON fund_transactions (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_fund_transactions_updated_by_id ON fund_transactions (updated_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_groups_group_name ON groups (group_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_groups_leader_id ON groups (leader_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_groups_parent_group_id ON groups (parent_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisition_atms_entity_id ON requisition_atms (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisition_atms_requisition_id ON requisition_atms (requisition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisition_items_requisition_id ON requisition_items (requisition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_approved_by_id ON requisitions (approved_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_archived_by_id ON requisitions (archived_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_collected_by_id ON requisitions (collected_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_group_id ON requisitions (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_money_ready_by_id ON requisitions (money_ready_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_requisitions_requisition_number ON requisitions (requisition_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_requisitor_id ON requisitions (requisitor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_requisitions_status ON requisitions (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_sections_section_number ON sections (section_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_system_configs_config_key ON system_configs (config_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_users_created_by_id ON users (created_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_users_group_id ON users (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE UNIQUE INDEX ix_users_membership_id ON users (membership_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_users_section_id ON users (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    CREATE INDEX ix_users_updated_by_id ON users (updated_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contribution_allocations ADD CONSTRAINT fk_contribution_allocations_contributions_contribution_id FOREIGN KEY (contribution_id) REFERENCES contributions (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contribution_allocations ADD CONSTRAINT fk_contribution_allocations_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contribution_allocations ADD CONSTRAINT fk_contribution_allocations_users_created_by_id FOREIGN KEY (created_by_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contribution_atms ADD CONSTRAINT fk_contribution_atms_contributions_entity_id FOREIGN KEY (entity_id) REFERENCES contributions (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contribution_lcks ADD CONSTRAINT fk_contribution_lcks_contributions_entity_id FOREIGN KEY (entity_id) REFERENCES contributions (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contribution_lcks ADD CONSTRAINT fk_contribution_lcks_users_locked_by_id FOREIGN KEY (locked_by_id) REFERENCES users (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_users_confirmed_by FOREIGN KEY (confirmed_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_users_created_by_id FOREIGN KEY (created_by_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_users_received_by FOREIGN KEY (received_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_users_updated_by_id FOREIGN KEY (updated_by_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE fund_balances ADD CONSTRAINT fk_fund_balances_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE fund_transactions ADD CONSTRAINT fk_fund_transactions_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE fund_transactions ADD CONSTRAINT fk_fund_transactions_users_created_by_id FOREIGN KEY (created_by_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE fund_transactions ADD CONSTRAINT fk_fund_transactions_users_updated_by_id FOREIGN KEY (updated_by_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    ALTER TABLE groups ADD CONSTRAINT fk_groups_users_leader_id FOREIGN KEY (leader_id) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801112813_SomeMigrationName') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260801112813_SomeMigrationName', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801114538_ProfilePhotoAndSignatureBase64Len') THEN
    ALTER TABLE users ALTER COLUMN signature_data TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801114538_ProfilePhotoAndSignatureBase64Len') THEN
    ALTER TABLE users ALTER COLUMN profile_photo TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260801114538_ProfilePhotoAndSignatureBase64Len') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260801114538_ProfilePhotoAndSignatureBase64Len', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    ALTER TABLE fund_balances ALTER COLUMN group_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    ALTER TABLE fund_balances ADD committee_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    ALTER TABLE fund_balances ADD section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE TABLE committees (
        id uuid NOT NULL,
        committee_name text NOT NULL,
        description text,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_committees PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE TABLE user_group_assignments (
        user_id uuid NOT NULL,
        group_id uuid NOT NULL,
        assigned_date timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_group_assignments PRIMARY KEY (user_id, group_id),
        CONSTRAINT fk_user_group_assignments_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_group_assignments_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE TABLE user_committee_assignments (
        user_id uuid NOT NULL,
        committee_id uuid NOT NULL,
        assigned_date timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_committee_assignments PRIMARY KEY (user_id, committee_id),
        CONSTRAINT fk_user_committee_assignments_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_committee_assignments_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    INSERT INTO committees (id, committee_name, description, is_active, created_date, updated_date,
        last_processed_time_stamp, created_by_id, updated_by_id, version)
    SELECT id, group_name, description, is_active, created_date, updated_date,
        last_processed_time_stamp, created_by_id, updated_by_id, version
    FROM groups g
    WHERE g.group_type = 'Committee'
      AND NOT EXISTS (
          SELECT 1 FROM committees c
          WHERE c.id = g.id OR LOWER(c.committee_name) = LOWER(g.group_name)
      )
    ON CONFLICT DO NOTHING;

    INSERT INTO sections (id, section_number, section_name, description, is_active, created_date,
        updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
    SELECT g.id, (COALESCE((SELECT MAX(section_number) FROM sections), 0) + ROW_NUMBER() OVER (ORDER BY g.group_name))::integer,
        g.group_name, g.description, g.is_active, g.created_date, g.updated_date,
        g.last_processed_time_stamp, g.created_by_id, g.updated_by_id, g.version
    FROM groups g WHERE g.group_type = 'Section'
      AND NOT EXISTS (SELECT 1 FROM sections s WHERE LOWER(s.section_name) = LOWER(g.group_name));

    INSERT INTO user_group_assignments (user_id, group_id, assigned_date)
    SELECT u.id, u.group_id, NOW() FROM users u JOIN groups g ON g.id = u.group_id
    WHERE g.group_type = 'ChurchGroup' ON CONFLICT DO NOTHING;

    INSERT INTO user_committee_assignments (user_id, committee_id, assigned_date)
    SELECT u.id, u.group_id, NOW() FROM users u JOIN groups g ON g.id = u.group_id
    WHERE g.group_type = 'Committee' ON CONFLICT DO NOTHING;

    UPDATE users SET group_id = NULL
    WHERE group_id IN (SELECT id FROM groups WHERE group_type IN ('Section', 'Committee'));
    UPDATE groups SET group_type = 'LegacySection', is_active = FALSE WHERE group_type = 'Section';
    UPDATE groups SET group_type = 'LegacyCommittee', is_active = FALSE WHERE group_type = 'Committee';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE UNIQUE INDEX ix_fund_balances_committee_id_fiscal_year ON fund_balances (committee_id, fiscal_year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE UNIQUE INDEX ix_fund_balances_section_id_fiscal_year ON fund_balances (section_id, fiscal_year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE UNIQUE INDEX ix_committees_committee_name ON committees (committee_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE INDEX ix_user_committee_assignments_committee_id ON user_committee_assignments (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    CREATE INDEX ix_user_group_assignments_group_id ON user_group_assignments (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    ALTER TABLE fund_balances ADD CONSTRAINT fk_fund_balances_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    ALTER TABLE fund_balances ADD CONSTRAINT fk_fund_balances_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802053716_SeparateGroupsSectionsCommittees') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260802053716_SeparateGroupsSectionsCommittees', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE requisitions ADD committee_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE requisitions ADD section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE fund_transactions ALTER COLUMN group_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE fund_transactions ADD committee_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE fund_transactions ADD section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE contribution_allocations ALTER COLUMN group_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE contribution_allocations ADD committee_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE contribution_allocations ADD section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    CREATE INDEX ix_requisitions_committee_id ON requisitions (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    CREATE INDEX ix_requisitions_section_id ON requisitions (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    CREATE INDEX ix_fund_transactions_committee_id ON fund_transactions (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    CREATE INDEX ix_fund_transactions_section_id ON fund_transactions (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    CREATE INDEX ix_contribution_allocations_committee_id ON contribution_allocations (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    CREATE INDEX ix_contribution_allocations_section_id ON contribution_allocations (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE contribution_allocations ADD CONSTRAINT fk_contribution_allocations_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE contribution_allocations ADD CONSTRAINT fk_contribution_allocations_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE fund_transactions ADD CONSTRAINT fk_fund_transactions_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE fund_transactions ADD CONSTRAINT fk_fund_transactions_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE requisitions ADD CONSTRAINT fk_requisitions_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    ALTER TABLE requisitions ADD CONSTRAINT fk_requisitions_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802055828_OrganizationEntitiesInFinance') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260802055828_OrganizationEntitiesInFinance', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802232801_AddSystemLogs') THEN
    CREATE TABLE logs (
        id uuid NOT NULL,
        timestamp_utc timestamp with time zone NOT NULL,
        level character varying(20) NOT NULL,
        category character varying(20) NOT NULL,
        process character varying(100) NOT NULL,
        action character varying(150) NOT NULL,
        http_method character varying(10),
        path character varying(500),
        status_code integer,
        duration_milliseconds bigint,
        user_id uuid,
        membership_id character varying(100),
        user_role character varying(100),
        ip_address character varying(100),
        correlation_id character varying(100) NOT NULL,
        message character varying(2000) NOT NULL,
        details text,
        exception text,
        CONSTRAINT pk_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802232801_AddSystemLogs') THEN
    CREATE INDEX ix_logs_process_timestamp_utc ON logs (process, timestamp_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802232801_AddSystemLogs') THEN
    CREATE INDEX ix_logs_timestamp_utc ON logs (timestamp_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802232801_AddSystemLogs') THEN
    CREATE INDEX ix_logs_user_id ON logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260802232801_AddSystemLogs') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260802232801_AddSystemLogs', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE TABLE requisition_lcks (
        id uuid NOT NULL,
        requisition_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        locked_by_id uuid NOT NULL,
        locked_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        lock_reason text,
        CONSTRAINT pk_requisition_lcks PRIMARY KEY (id),
        CONSTRAINT fk_requisition_lcks_requisitions_requisition_id FOREIGN KEY (requisition_id) REFERENCES requisitions (id),
        CONSTRAINT fk_requisition_lcks_users_locked_by_id FOREIGN KEY (locked_by_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE TABLE user_lcks (
        id uuid NOT NULL,
        user_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        locked_by_id uuid NOT NULL,
        locked_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        lock_reason text,
        CONSTRAINT pk_user_lcks PRIMARY KEY (id),
        CONSTRAINT fk_user_lcks_users_locked_by_id FOREIGN KEY (locked_by_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_lcks_users_user_id FOREIGN KEY (user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE UNIQUE INDEX ix_requisition_lcks_entity_id ON requisition_lcks (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE INDEX ix_requisition_lcks_locked_by_id ON requisition_lcks (locked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE INDEX ix_requisition_lcks_requisition_id ON requisition_lcks (requisition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE UNIQUE INDEX ix_user_lcks_entity_id ON user_lcks (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE INDEX ix_user_lcks_locked_by_id ON user_lcks (locked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    CREATE INDEX ix_user_lcks_user_id ON user_lcks (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804073720_AddRequisitionAndUserLocks') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260804073720_AddRequisitionAndUserLocks', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804104308_AddSessionValidation') THEN
    ALTER TABLE users ADD current_session_id text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804104308_AddSessionValidation') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260804104308_AddSessionValidation', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804131820_040820251517') THEN
    CREATE TABLE payment_methods (
        id uuid NOT NULL,
        code text NOT NULL,
        name text NOT NULL,
        description text,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_methods PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804131820_040820251517') THEN
    CREATE UNIQUE INDEX ix_payment_methods_code ON payment_methods (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804131820_040820251517') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260804131820_040820251517', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804133928_040820251538') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260804133928_040820251538', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260804141607_040820251615') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260804141607_040820251615', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    DROP TABLE IF EXISTS payment_methods;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    CREATE TABLE email_logs (
        id uuid NOT NULL,
        sent_at_utc timestamp with time zone NOT NULL,
        to_email character varying(255) NOT NULL,
        subject character varying(255) NOT NULL,
        category character varying(100) NOT NULL,
        status character varying(20) NOT NULL,
        failure_reason character varying(1000),
        triggered_by_user_id uuid,
        CONSTRAINT pk_email_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    CREATE TABLE role_permissions (
        id uuid NOT NULL,
        role character varying(50) NOT NULL,
        permissions_json text NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_role_permissions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    CREATE TABLE smtp_settings (
        id uuid NOT NULL,
        enabled boolean NOT NULL,
        host character varying(255) NOT NULL,
        port integer NOT NULL,
        enable_ssl boolean NOT NULL,
        username character varying(255) NOT NULL,
        password character varying(500) NOT NULL,
        from_email character varying(255) NOT NULL,
        from_name character varying(255) NOT NULL,
        timeout_milliseconds integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_smtp_settings PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    CREATE INDEX ix_email_logs_sent_at_utc ON email_logs (sent_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    CREATE INDEX ix_email_logs_status ON email_logs (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    CREATE UNIQUE INDEX ix_role_permissions_role ON role_permissions (role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808163639_080820261835') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260808163639_080820261835', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808201638_AddProcessAuditTables') THEN
    CREATE TABLE contribution_aud (
        id uuid NOT NULL,
        contribution_id uuid NOT NULL,
        action character varying(50) NOT NULL,
        changes_json text NOT NULL,
        performed_by_id uuid,
        performed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_contribution_aud PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808201638_AddProcessAuditTables') THEN
    CREATE TABLE requisition_aud (
        id uuid NOT NULL,
        requisition_id uuid NOT NULL,
        action character varying(50) NOT NULL,
        changes_json text NOT NULL,
        performed_by_id uuid,
        performed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_requisition_aud PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808201638_AddProcessAuditTables') THEN
    CREATE INDEX ix_contribution_aud_contribution_id_performed_at_utc ON contribution_aud (contribution_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808201638_AddProcessAuditTables') THEN
    CREATE INDEX ix_requisition_aud_requisition_id_performed_at_utc ON requisition_aud (requisition_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808201638_AddProcessAuditTables') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260808201638_AddProcessAuditTables', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    INSERT INTO role_permissions
        (id, role, permissions_json, created_date, updated_date, version)
    SELECT gen_random_uuid(), substring(config_key from 17), config_value,
           CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1
    FROM system_configs
    WHERE config_key LIKE 'RolePermissions:%'
    ON CONFLICT (role) DO UPDATE
        SET permissions_json = EXCLUDED.permissions_json,
            updated_date = CURRENT_TIMESTAMP,
            version = role_permissions.version + 1;

    DELETE FROM system_configs WHERE config_key LIKE 'RolePermissions:%';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    CREATE TABLE asset_movements (
        id uuid NOT NULL,
        asset_id uuid NOT NULL,
        checkout_id uuid,
        quantity_change integer NOT NULL,
        balance_after integer NOT NULL,
        action text NOT NULL,
        performed_by_id uuid NOT NULL,
        performed_at_utc timestamp with time zone NOT NULL,
        notes text,
        CONSTRAINT pk_asset_movements PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    CREATE TABLE assets (
        id uuid NOT NULL,
        asset_number character varying(40) NOT NULL,
        name character varying(150) NOT NULL,
        category character varying(30) NOT NULL,
        description text,
        quantity_total integer NOT NULL,
        quantity_available integer NOT NULL,
        registration_number character varying(50),
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_assets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    CREATE TABLE asset_checkouts (
        id uuid NOT NULL,
        asset_id uuid NOT NULL,
        requested_by_id uuid NOT NULL,
        driver_id uuid,
        approved_by_id uuid,
        quantity integer NOT NULL,
        status character varying(30) NOT NULL,
        purpose text NOT NULL,
        expected_out_at_utc timestamp with time zone NOT NULL,
        expected_return_at_utc timestamp with time zone NOT NULL,
        checked_out_at_utc timestamp with time zone,
        returned_at_utc timestamp with time zone,
        approval_note text,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_asset_checkouts PRIMARY KEY (id),
        CONSTRAINT fk_asset_checkouts_assets_asset_id FOREIGN KEY (asset_id) REFERENCES assets (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    CREATE INDEX ix_asset_checkouts_asset_id_status ON asset_checkouts (asset_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    CREATE INDEX ix_asset_movements_asset_id_performed_at_utc ON asset_movements (asset_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    CREATE UNIQUE INDEX ix_assets_asset_number ON assets (asset_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808215138_AddAssetsInventoryFleet') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260808215138_AddAssetsInventoryFleet', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    ALTER TABLE assets ADD asset_category_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    CREATE TABLE asset_categories (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        name character varying(100) NOT NULL,
        description text,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_asset_categories PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    CREATE INDEX ix_assets_asset_category_id ON assets (asset_category_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    CREATE UNIQUE INDEX ix_asset_categories_code ON asset_categories (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    INSERT INTO asset_categories
        (id, code, name, description, is_active, created_date, updated_date, version)
    VALUES
        ('10f202dd-4d92-4da2-9164-e6fce9316001', 'INVENTORY', 'Inventory', 'Counted items such as chairs and tents.', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1),
        ('10f202dd-4d92-4da2-9164-e6fce9316002', 'VEHICLE', 'Vehicle', 'Fleet assets such as buses and utility vehicles.', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1),
        ('10f202dd-4d92-4da2-9164-e6fce9316003', 'EQUIPMENT', 'Equipment', 'Reusable church equipment and machinery.', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
    ON CONFLICT (code) DO NOTHING;

    INSERT INTO asset_categories
        (id, code, name, is_active, created_date, updated_date, version)
    SELECT gen_random_uuid(), normalized.code, initcap(replace(lower(normalized.code), '_', ' ')), TRUE,
           CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1
    FROM (
        SELECT DISTINCT upper(trim(a.category)) AS code
        FROM assets a
        WHERE a.category IS NOT NULL AND trim(a.category) <> ''
    ) normalized
    WHERE NOT EXISTS (SELECT 1 FROM asset_categories c WHERE c.code = normalized.code);

    UPDATE assets a
    SET asset_category_id = c.id
    FROM asset_categories c
    WHERE c.code = upper(a.category) AND a.asset_category_id IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    ALTER TABLE assets ADD CONSTRAINT fk_assets_asset_categories_asset_category_id FOREIGN KEY (asset_category_id) REFERENCES asset_categories (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808221909_AddConfigurableAssetCategories') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260808221909_AddConfigurableAssetCategories', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    ALTER TABLE asset_checkouts ADD payment_amount numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    ALTER TABLE asset_checkouts ADD payment_honored boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    ALTER TABLE asset_checkouts ADD payment_honored_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    ALTER TABLE asset_checkouts ADD payment_required boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    ALTER TABLE asset_checkouts ADD payment_verified_by_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE TABLE asset_atms (
        id uuid NOT NULL,
        asset_checkout_id uuid,
        attachment_type character varying(30) NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        file_name text NOT NULL,
        file_path text NOT NULL,
        mime_type text NOT NULL,
        size bigint NOT NULL,
        uploaded_by_id uuid,
        uploaded_at timestamp with time zone NOT NULL,
        description text,
        CONSTRAINT pk_asset_atms PRIMARY KEY (id),
        CONSTRAINT fk_asset_atms_asset_checkouts_asset_checkout_id FOREIGN KEY (asset_checkout_id) REFERENCES asset_checkouts (id) ON DELETE CASCADE,
        CONSTRAINT fk_asset_atms_assets_entity_id FOREIGN KEY (entity_id) REFERENCES assets (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE TABLE asset_aud (
        id uuid NOT NULL,
        asset_id uuid NOT NULL,
        asset_checkout_id uuid,
        record_type character varying(30) NOT NULL,
        action character varying(50) NOT NULL,
        changes_json text NOT NULL,
        performed_by_id uuid,
        performed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_asset_aud PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE TABLE asset_lcks (
        id uuid NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        locked_by_id uuid NOT NULL,
        locked_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        lock_reason text,
        CONSTRAINT pk_asset_lcks PRIMARY KEY (id),
        CONSTRAINT fk_asset_lcks_assets_entity_id FOREIGN KEY (entity_id) REFERENCES assets (id) ON DELETE CASCADE,
        CONSTRAINT fk_asset_lcks_users_locked_by_id FOREIGN KEY (locked_by_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE INDEX ix_asset_atms_asset_checkout_id ON asset_atms (asset_checkout_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE INDEX ix_asset_atms_entity_id ON asset_atms (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE INDEX ix_asset_aud_asset_id_performed_at_utc ON asset_aud (asset_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE UNIQUE INDEX ix_asset_lcks_entity_id ON asset_lcks (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    CREATE INDEX ix_asset_lcks_locked_by_id ON asset_lcks (locked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223048_AddAssetCompanionRecordsAndPayments') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260808223048_AddAssetCompanionRecordsAndPayments', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260808223549_090820261235') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260808223549_090820261235', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809011742_AddMaintainedAssetDrivers') THEN
    CREATE TABLE asset_drivers (
        id uuid NOT NULL,
        name character varying(120) NOT NULL,
        email character varying(200) NOT NULL,
        phone_number character varying(30) NOT NULL,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_asset_drivers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809011742_AddMaintainedAssetDrivers') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260809011742_AddMaintainedAssetDrivers', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809090000_AddAssetRequestNumbers') THEN
    ALTER TABLE asset_checkouts ADD request_number character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809090000_AddAssetRequestNumbers') THEN
    UPDATE asset_checkouts
    SET request_number = 'ARQ-' || TO_CHAR(created_date AT TIME ZONE 'UTC', 'YYYYMMDD') || '-' || UPPER(SUBSTRING(REPLACE(id::text, '-', '') FROM 1 FOR 8))
    WHERE request_number IS NULL OR request_number = '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809090000_AddAssetRequestNumbers') THEN
    ALTER TABLE asset_checkouts ALTER COLUMN request_number TYPE character varying(40);
    ALTER TABLE asset_checkouts ALTER COLUMN request_number SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809090000_AddAssetRequestNumbers') THEN
    CREATE UNIQUE INDEX ix_asset_checkouts_request_number ON asset_checkouts (request_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809090000_AddAssetRequestNumbers') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260809090000_AddAssetRequestNumbers', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    CREATE TABLE asset_checkouts_atms (
        id uuid NOT NULL,
        attachment_type character varying(30) NOT NULL,
        entity_id uuid NOT NULL,
        file_name text NOT NULL,
        file_path text NOT NULL,
        mime_type text NOT NULL,
        size bigint NOT NULL,
        uploaded_by_id uuid,
        uploaded_at timestamp with time zone NOT NULL,
        description text,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_asset_checkouts_atms PRIMARY KEY (id),
        CONSTRAINT fk_asset_checkouts_atms_asset_checkouts_entity_id FOREIGN KEY (entity_id) REFERENCES asset_checkouts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    CREATE INDEX ix_asset_checkouts_atms_entity_id ON asset_checkouts_atms (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    INSERT INTO asset_checkouts_atms
        (id, attachment_type, entity_id, file_name, file_path, mime_type, size, uploaded_by_id, uploaded_at,
         description, created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version)
    SELECT id, attachment_type, asset_checkout_id, file_name, file_path, mime_type, size, uploaded_by_id, uploaded_at,
           description, created_date, updated_date, last_processed_time_stamp, created_by_id, updated_by_id, version
    FROM asset_atms WHERE asset_checkout_id IS NOT NULL;
    DELETE FROM asset_atms WHERE asset_checkout_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    ALTER TABLE asset_atms DROP CONSTRAINT fk_asset_atms_asset_checkouts_asset_checkout_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    DROP INDEX ix_asset_atms_asset_checkout_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    ALTER TABLE asset_atms DROP COLUMN asset_checkout_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809093000_SeparateAssetCheckoutAttachments') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260809093000_SeparateAssetCheckoutAttachments', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809100000_ExpandAssetRolePermissions') THEN
    UPDATE role_permissions
    SET permissions_json = (permissions_json::jsonb || '["assets.request"]'::jsonb)::text
    WHERE role <> 'Auditor'
      AND permissions_json::jsonb ? 'assets.view'
      AND NOT permissions_json::jsonb ? 'assets.request';

    UPDATE role_permissions
    SET permissions_json = (permissions_json::jsonb || '["assets.reports"]'::jsonb)::text
    WHERE permissions_json::jsonb ? 'reports.view'
      AND NOT permissions_json::jsonb ? 'assets.reports';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260809100000_ExpandAssetRolePermissions') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260809100000_ExpandAssetRolePermissions', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812090000_ExpandRequisitionApprovalChain') THEN
    ALTER TABLE requisitions ADD treasurer_approver_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812090000_ExpandRequisitionApprovalChain') THEN
    ALTER TABLE requisitions ADD accountant_approver_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812090000_ExpandRequisitionApprovalChain') THEN
    ALTER TABLE requisitions ADD church_authority_approver_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812090000_ExpandRequisitionApprovalChain') THEN
    ALTER TABLE requisitions ADD minister_approver_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812090000_ExpandRequisitionApprovalChain') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260812090000_ExpandRequisitionApprovalChain', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD group_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD committee_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    CREATE INDEX ix_asset_checkouts_group_id ON asset_checkouts (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    CREATE INDEX ix_asset_checkouts_section_id ON asset_checkouts (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    CREATE INDEX ix_asset_checkouts_committee_id ON asset_checkouts (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD CONSTRAINT fk_asset_checkouts_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD CONSTRAINT fk_asset_checkouts_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD CONSTRAINT fk_asset_checkouts_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    ALTER TABLE asset_checkouts ADD CONSTRAINT "CK_AssetCheckout_OneEntity" CHECK ((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END + CASE WHEN section_id IS NULL THEN 0 ELSE 1 END + CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END) <= 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260812110000_AddAssetCheckoutRequestingEntity') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260812110000_AddAssetCheckoutRequestingEntity', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816065131_AddProtectedSystemLicense') THEN
    CREATE TABLE system_licenses (
        id uuid NOT NULL,
        protected_payload text NOT NULL,
        installed_at_utc timestamp with time zone NOT NULL,
        installed_by_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_system_licenses PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816065131_AddProtectedSystemLicense') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260816065131_AddProtectedSystemLicense', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816175757_AddRetryableEmailPayload') THEN
    ALTER TABLE email_logs ADD cc_emails character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816175757_AddRetryableEmailPayload') THEN
    ALTER TABLE email_logs ADD html_body text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816175757_AddRetryableEmailPayload') THEN
    ALTER TABLE email_logs ADD retry_of_email_log_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816175757_AddRetryableEmailPayload') THEN
    CREATE INDEX ix_email_logs_retry_of_email_log_id ON email_logs (retry_of_email_log_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260816175757_AddRetryableEmailPayload') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260816175757_AddRetryableEmailPayload', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817084816_AddProtectedIntegrationSettings') THEN
    CREATE TABLE integration_settings (
        id uuid NOT NULL,
        integration_key text NOT NULL,
        protected_settings text NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_integration_settings PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817084816_AddProtectedIntegrationSettings') THEN
    CREATE UNIQUE INDEX ix_integration_settings_integration_key ON integration_settings (integration_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817084816_AddProtectedIntegrationSettings') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260817084816_AddProtectedIntegrationSettings', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817115855_AddCommunicationDeliveryHistory') THEN
    CREATE TABLE communication_logs (
        id uuid NOT NULL,
        sent_at_utc timestamp with time zone NOT NULL,
        channel character varying(20) NOT NULL,
        provider character varying(50) NOT NULL,
        recipient character varying(40) NOT NULL,
        action character varying(100) NOT NULL,
        status character varying(20) NOT NULL,
        provider_message_id character varying(100),
        failure_reason character varying(1000),
        protected_message text,
        retry_of_communication_log_id uuid,
        CONSTRAINT pk_communication_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817115855_AddCommunicationDeliveryHistory') THEN
    CREATE INDEX ix_communication_logs_channel_status_sent_at_utc ON communication_logs (channel, status, sent_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817115855_AddCommunicationDeliveryHistory') THEN
    CREATE INDEX ix_communication_logs_retry_of_communication_log_id ON communication_logs (retry_of_communication_log_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817115855_AddCommunicationDeliveryHistory') THEN
    CREATE INDEX ix_communication_logs_sent_at_utc ON communication_logs (sent_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260817115855_AddCommunicationDeliveryHistory') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260817115855_AddCommunicationDeliveryHistory', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    CREATE TABLE license_key_rotation_audits (
        id uuid NOT NULL,
        product_code text NOT NULL,
        sequence bigint NOT NULL,
        previous_key_id text,
        new_key_id text,
        action text NOT NULL,
        result text NOT NULL,
        failure_reason text,
        package_hash text NOT NULL,
        uploaded_by_id uuid,
        uploaded_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_license_key_rotation_audits PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    CREATE TABLE license_trusted_keys (
        id uuid NOT NULL,
        product_code text NOT NULL,
        key_id text NOT NULL,
        public_key_pem text NOT NULL,
        fingerprint text NOT NULL,
        status text NOT NULL,
        rotation_sequence bigint NOT NULL,
        valid_from_utc timestamp with time zone NOT NULL,
        revoked_at_utc timestamp with time zone,
        installed_at_utc timestamp with time zone NOT NULL,
        installed_by_id uuid,
        rotation_package_hash text NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_license_trusted_keys PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    CREATE INDEX ix_license_key_rotation_audits_package_hash ON license_key_rotation_audits (package_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    CREATE INDEX ix_license_key_rotation_audits_product_code_sequence ON license_key_rotation_audits (product_code, sequence);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    CREATE UNIQUE INDEX ix_license_trusted_keys_product_code_key_id ON license_trusted_keys (product_code, key_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    CREATE UNIQUE INDEX ix_license_trusted_keys_product_code_rotation_sequence ON license_trusted_keys (product_code, rotation_sequence);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260823205623_AddLicenseKeyRotationTrust') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260823205623_AddLicenseKeyRotationTrust', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260825133000_RemoveLegacyNotificationRoutingConfigs') THEN
    DELETE FROM system_configs
    WHERE lower(config_key) IN (
        'notification_asset_request_cc_roles',
        'notification_asset_request_to_roles',
        'notification_contribution_cc_roles',
        'notification_contribution_to_roles',
        'notification_requisition_cc_roles',
        'notification_requisition_to_roles'
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260825133000_RemoveLegacyNotificationRoutingConfigs') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260825133000_RemoveLegacyNotificationRoutingConfigs', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'platform') THEN
            CREATE SCHEMA platform;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
    CREATE TABLE platform.outbox_messages (
        id uuid NOT NULL,
        tenant_id uuid,
        message_type character varying(250) NOT NULL,
        payload_json jsonb NOT NULL,
        headers_json jsonb,
        correlation_id character varying(100),
        idempotency_key character varying(200),
        occurred_at_utc timestamp with time zone NOT NULL,
        available_at_utc timestamp with time zone NOT NULL,
        processed_at_utc timestamp with time zone,
        last_attempt_at_utc timestamp with time zone,
        attempt_count integer NOT NULL,
        last_failure character varying(2000),
        lock_token uuid,
        locked_until_utc timestamp with time zone,
        CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
    CREATE INDEX ix_outbox_messages_correlation_id ON platform.outbox_messages (correlation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
    CREATE INDEX ix_outbox_messages_locked_until_utc_available_at_utc ON platform.outbox_messages (locked_until_utc, available_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
    CREATE INDEX ix_outbox_messages_processed_at_utc_available_at_utc ON platform.outbox_messages (processed_at_utc, available_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
    CREATE UNIQUE INDEX ix_outbox_messages_tenant_id_idempotency_key ON platform.outbox_messages (tenant_id, idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260829173536_AddPlatformOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260829173536_AddPlatformOutbox', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    ALTER TABLE contributions ADD finalized_financial_record_book_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE financial_record_books (
        id uuid NOT NULL,
        frb_number character varying(50) NOT NULL,
        period_start date NOT NULL,
        period_end date NOT NULL,
        section_id uuid,
        status character varying(20) NOT NULL,
        notes text,
        reviewer_comments text,
        gross_contributions numeric(18,2) NOT NULL,
        assessment_total numeric(18,2) NOT NULL,
        net_position numeric(18,2) NOT NULL,
        submitted_at_utc timestamp with time zone,
        submitted_by_id uuid,
        finalized_at_utc timestamp with time zone,
        finalized_by_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_financial_record_books PRIMARY KEY (id),
        CONSTRAINT fk_financial_record_books_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE frb_assessment_rules (
        id uuid NOT NULL,
        name character varying(150) NOT NULL,
        description text,
        classification character varying(30) NOT NULL,
        calculation_method character varying(30) NOT NULL,
        beneficiary character varying(150) NOT NULL,
        rate numeric(9,4),
        fixed_amount numeric(18,2),
        effective_from date NOT NULL,
        effective_to date,
        is_active boolean NOT NULL,
        notes text,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_frb_assessment_rules PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE frb_aud (
        id uuid NOT NULL,
        financial_record_book_id uuid NOT NULL,
        action character varying(50) NOT NULL,
        changes_json text NOT NULL,
        performed_by_id uuid,
        performed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_frb_aud PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE reference_number_configurations (
        id uuid NOT NULL,
        reference_type character varying(30) NOT NULL,
        prefix character varying(10) NOT NULL,
        description character varying(200) NOT NULL,
        effective_from date NOT NULL,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_reference_number_configurations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE reference_number_sequences (
        reference_type character varying(30) NOT NULL,
        sequence_date date NOT NULL,
        last_value bigint NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_reference_number_sequences PRIMARY KEY (reference_type, sequence_date)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE financial_record_book_contributions (
        id uuid NOT NULL,
        financial_record_book_id uuid NOT NULL,
        contribution_id uuid NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_financial_record_book_contributions PRIMARY KEY (id),
        CONSTRAINT fk_financial_record_book_contributions_contributions_contribut FOREIGN KEY (contribution_id) REFERENCES contributions (id) ON DELETE RESTRICT,
        CONSTRAINT fk_financial_record_book_contributions_financial_record_books_ FOREIGN KEY (financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE frb_assessment_snapshots (
        id uuid NOT NULL,
        financial_record_book_id uuid NOT NULL,
        source_rule_id uuid NOT NULL,
        rule_name character varying(150) NOT NULL,
        classification character varying(30) NOT NULL,
        calculation_method character varying(30) NOT NULL,
        beneficiary character varying(150) NOT NULL,
        rate numeric(9,4),
        fixed_amount numeric(18,2),
        assessment_base numeric(18,2) NOT NULL,
        calculated_amount numeric(18,2) NOT NULL,
        contribution_type_ids_json text NOT NULL,
        contribution_type_names_json text NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_frb_assessment_snapshots PRIMARY KEY (id),
        CONSTRAINT fk_frb_assessment_snapshots_financial_record_books_financial_r FOREIGN KEY (financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE frb_verifications (
        id uuid NOT NULL,
        financial_record_book_id uuid NOT NULL,
        officer_type character varying(40) NOT NULL,
        verified_by_id uuid NOT NULL,
        officer_name character varying(160) NOT NULL,
        officer_role character varying(50) NOT NULL,
        signature_data text NOT NULL,
        comments text,
        verified_at_utc timestamp with time zone NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_frb_verifications PRIMARY KEY (id),
        CONSTRAINT fk_frb_verifications_financial_record_books_financial_record_b FOREIGN KEY (financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE TABLE frb_assessment_rule_contribution_types (
        frb_assessment_rule_id uuid NOT NULL,
        contribution_type_id uuid NOT NULL,
        CONSTRAINT pk_frb_assessment_rule_contribution_types PRIMARY KEY (frb_assessment_rule_id, contribution_type_id),
        CONSTRAINT fk_frb_assessment_rule_contribution_types_contribution_types_c FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE RESTRICT,
        CONSTRAINT fk_frb_assessment_rule_contribution_types_frb_assessment_rules FOREIGN KEY (frb_assessment_rule_id) REFERENCES frb_assessment_rules (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_contributions_finalized_financial_record_book_id ON contributions (finalized_financial_record_book_id) WHERE finalized_financial_record_book_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE INDEX ix_financial_record_book_contributions_contribution_id ON financial_record_book_contributions (contribution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_financial_record_book_contributions_financial_record_book_i ON financial_record_book_contributions (financial_record_book_id, contribution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_financial_record_books_frb_number ON financial_record_books (frb_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE INDEX ix_financial_record_books_section_id ON financial_record_books (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE INDEX ix_financial_record_books_status_period_start_period_end ON financial_record_books (status, period_start, period_end);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE INDEX ix_frb_assessment_rule_contribution_types_contribution_type_id ON frb_assessment_rule_contribution_types (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_frb_assessment_rules_name_effective_from ON frb_assessment_rules (name, effective_from);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_frb_assessment_snapshots_financial_record_book_id_source_ru ON frb_assessment_snapshots (financial_record_book_id, source_rule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE INDEX ix_frb_aud_financial_record_book_id_performed_at_utc ON frb_aud (financial_record_book_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_frb_verifications_financial_record_book_id_officer_type ON frb_verifications (financial_record_book_id, officer_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    CREATE UNIQUE INDEX ix_reference_number_configurations_reference_type_effective_fr ON reference_number_configurations (reference_type, effective_from);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004073139_AddFinancialRecordBooksAndReferenceNumbering') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261004073139_AddFinancialRecordBooksAndReferenceNumbering', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    CREATE INDEX ix_frb_verifications_verified_by_id ON frb_verifications (verified_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    CREATE INDEX ix_frb_assessment_snapshots_source_rule_id ON frb_assessment_snapshots (source_rule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_financial_record_books_finalized_financial_re FOREIGN KEY (finalized_financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    ALTER TABLE frb_assessment_snapshots ADD CONSTRAINT fk_frb_assessment_snapshots_frb_assessment_rules_source_rule_id FOREIGN KEY (source_rule_id) REFERENCES frb_assessment_rules (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    ALTER TABLE frb_aud ADD CONSTRAINT fk_frb_aud_financial_record_books_financial_record_book_id FOREIGN KEY (financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    ALTER TABLE frb_verifications ADD CONSTRAINT fk_frb_verifications_users_verified_by_id FOREIGN KEY (verified_by_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004074809_AddFrbReferentialConstraints') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261004074809_AddFrbReferentialConstraints', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD amount_snapshot numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD contribution_date_snapshot date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD contribution_number_snapshot character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD contribution_type_id_snapshot uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD contribution_type_name_snapshot character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD description_snapshot text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    ALTER TABLE financial_record_book_contributions ADD status_snapshot character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004075652_SnapshotFrbContributionValues') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261004075652_SnapshotFrbContributionValues', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    ALTER TABLE frb_assessment_rules ALTER COLUMN beneficiary DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    CREATE TABLE frb_assessment_allocation_lines (
        id uuid NOT NULL,
        frb_assessment_rule_id uuid NOT NULL,
        beneficiary character varying(150) NOT NULL,
        rate numeric(9,4),
        fixed_amount numeric(18,2),
        is_local_retained boolean NOT NULL,
        display_order integer NOT NULL,
        notes character varying(500),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_frb_assessment_allocation_lines PRIMARY KEY (id),
        CONSTRAINT fk_frb_assessment_allocation_lines_frb_assessment_rules_frb_as FOREIGN KEY (frb_assessment_rule_id) REFERENCES frb_assessment_rules (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    CREATE TABLE frb_assessment_allocation_snapshots (
        id uuid NOT NULL,
        frb_assessment_snapshot_id uuid NOT NULL,
        source_allocation_line_id uuid,
        beneficiary character varying(150) NOT NULL,
        rate numeric(9,4),
        fixed_amount numeric(18,2),
        calculated_amount numeric(18,2) NOT NULL,
        is_local_retained boolean NOT NULL,
        display_order integer NOT NULL,
        notes character varying(500),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_frb_assessment_allocation_snapshots PRIMARY KEY (id),
        CONSTRAINT fk_frb_assessment_allocation_snapshots_frb_assessment_snapshot FOREIGN KEY (frb_assessment_snapshot_id) REFERENCES frb_assessment_snapshots (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    INSERT INTO frb_assessment_allocation_lines
        (id, frb_assessment_rule_id, beneficiary, rate, fixed_amount, is_local_retained,
         display_order, notes, created_date, updated_date, created_by_id, updated_by_id, version)
    SELECT gen_random_uuid(), id, beneficiary, rate, fixed_amount, FALSE,
           0, notes, created_date, updated_date, created_by_id, updated_by_id, 1
    FROM frb_assessment_rules
    WHERE beneficiary IS NOT NULL AND btrim(beneficiary) <> '';

    INSERT INTO frb_assessment_allocation_snapshots
        (id, frb_assessment_snapshot_id, beneficiary, rate, fixed_amount, calculated_amount,
         is_local_retained, display_order, created_date, updated_date, created_by_id, updated_by_id, version)
    SELECT gen_random_uuid(), id, beneficiary, rate, fixed_amount, calculated_amount,
           FALSE, 0, created_date, updated_date, created_by_id, updated_by_id, 1
    FROM frb_assessment_snapshots
    WHERE beneficiary IS NOT NULL AND btrim(beneficiary) <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    CREATE UNIQUE INDEX ix_frb_assessment_allocation_lines_frb_assessment_rule_id_bene ON frb_assessment_allocation_lines (frb_assessment_rule_id, beneficiary);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    CREATE INDEX ix_frb_assessment_allocation_snapshots_frb_assessment_snapshot ON frb_assessment_allocation_snapshots (frb_assessment_snapshot_id, display_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004082704_AddFrbAssessmentAllocationLines') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261004082704_AddFrbAssessmentAllocationLines', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004225528_ExpandFrbStatusLength') THEN
    ALTER TABLE financial_record_books ALTER COLUMN status TYPE character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004225528_ExpandFrbStatusLength') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261004225528_ExpandFrbStatusLength', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    CREATE TABLE designations (
        id uuid NOT NULL,
        code character varying(80) NOT NULL,
        name character varying(120) NOT NULL,
        required_role character varying(30) NOT NULL,
        functional_jurisdiction character varying(80) NOT NULL,
        description character varying(500),
        is_system boolean NOT NULL,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_designations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    CREATE TABLE user_designation_assignments (
        user_id uuid NOT NULL,
        designation_id uuid NOT NULL,
        is_primary boolean NOT NULL,
        effective_from_utc timestamp with time zone NOT NULL,
        effective_to_utc timestamp with time zone,
        assigned_by_id uuid,
        assigned_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_designation_assignments PRIMARY KEY (user_id, designation_id),
        CONSTRAINT fk_user_designation_assignments_designations_designation_id FOREIGN KEY (designation_id) REFERENCES designations (id) ON DELETE RESTRICT,
        CONSTRAINT fk_user_designation_assignments_users_assigned_by_id FOREIGN KEY (assigned_by_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT fk_user_designation_assignments_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    CREATE UNIQUE INDEX ix_designations_code ON designations (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    CREATE INDEX ix_designations_required_role_is_active ON designations (required_role, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    CREATE INDEX ix_user_designation_assignments_assigned_by_id ON user_designation_assignments (assigned_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    CREATE INDEX ix_user_designation_assignments_designation_id_effective_to_utc ON user_designation_assignments (designation_id, effective_to_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261004232336_AddUserDesignations') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261004232336_AddUserDesignations', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005205415_SeedSystemDesignations') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005205415_SeedSystemDesignations') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261005205415_SeedSystemDesignations', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005211707_RetireMinisterAndAccountantRoles') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005211707_RetireMinisterAndAccountantRoles') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261005211707_RetireMinisterAndAccountantRoles', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005212838_AddDesignationWorkflowCapabilities') THEN
    ALTER TABLE designations ADD capabilities_json text NOT NULL DEFAULT '[]';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005212838_AddDesignationWorkflowCapabilities') THEN
    UPDATE designations SET capabilities_json = '["FRB_VERIFY_STEWARDSHIP","FRB_ORGANIZATION_SCOPE"]' WHERE code = 'STEWARDSHIP_CONVENER';
    UPDATE designations SET capabilities_json = '["REQUISITION_ENTITY_REVIEW"]' WHERE code IN ('ELDER', 'STEWARD', 'SUNDAY_SCHOOL_TEACHER');
    UPDATE designations SET capabilities_json = '["REQUISITION_MINISTER_APPROVAL","FRB_VERIFY_MINISTER","FRB_ORGANIZATION_SCOPE"]' WHERE code = 'MINISTER';
    UPDATE designations SET capabilities_json = '["REQUISITION_TREASURER_APPROVAL","CONTRIBUTION_REVIEW","FRB_MANAGE","FRB_VERIFY_TREASURER","FRB_FINALIZE","FRB_ORGANIZATION_SCOPE"]' WHERE code = 'TREASURER';
    UPDATE designations SET capabilities_json = '["REQUISITION_ACCOUNTANT_APPROVAL","CONTRIBUTION_REVIEW","FRB_MANAGE"]' WHERE code = 'ACCOUNTANT';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261005212838_AddDesignationWorkflowCapabilities') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261005212838_AddDesignationWorkflowCapabilities', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE groups ADD section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE designations ADD functional_area_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE committees ADD parent_group_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE committees ADD parent_section_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE TABLE functional_areas (
        id uuid NOT NULL,
        code character varying(80) NOT NULL,
        name character varying(120) NOT NULL,
        description character varying(500),
        is_system boolean NOT NULL,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_functional_areas PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE TABLE office_appointments (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        designation_id uuid NOT NULL,
        functional_area_id uuid NOT NULL,
        scope_type character varying(30) NOT NULL,
        section_id uuid,
        group_id uuid,
        committee_id uuid,
        effective_from_utc timestamp with time zone NOT NULL,
        effective_to_utc timestamp with time zone,
        is_active boolean NOT NULL,
        assigned_by_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_office_appointments PRIMARY KEY (id),
        CONSTRAINT ck_office_appointments_dates CHECK (effective_to_utc IS NULL OR effective_to_utc > effective_from_utc),
        CONSTRAINT ck_office_appointments_scope CHECK ((scope_type = 'CONGREGATION' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'FUNCTIONAL_AREA' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'SECTION' AND section_id IS NOT NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'GROUP' AND section_id IS NULL AND group_id IS NOT NULL AND committee_id IS NULL) OR (scope_type = 'COMMITTEE' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NOT NULL) OR (scope_type = 'ENTITY' AND ((CASE WHEN section_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN group_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN committee_id IS NOT NULL THEN 1 ELSE 0 END)) = 1)),
        CONSTRAINT fk_office_appointments_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT,
        CONSTRAINT fk_office_appointments_designations_designation_id FOREIGN KEY (designation_id) REFERENCES designations (id) ON DELETE RESTRICT,
        CONSTRAINT fk_office_appointments_functional_areas_functional_area_id FOREIGN KEY (functional_area_id) REFERENCES functional_areas (id) ON DELETE RESTRICT,
        CONSTRAINT fk_office_appointments_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
        CONSTRAINT fk_office_appointments_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT,
        CONSTRAINT fk_office_appointments_users_assigned_by_id FOREIGN KEY (assigned_by_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT fk_office_appointments_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_groups_section_id ON groups (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_designations_functional_area_id ON designations (functional_area_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_committees_parent_group_id ON committees (parent_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_committees_parent_section_id ON committees (parent_section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE committees ADD CONSTRAINT ck_committees_single_parent CHECK (NOT (parent_section_id IS NOT NULL AND parent_group_id IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE UNIQUE INDEX ix_functional_areas_code ON functional_areas (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_assigned_by_id ON office_appointments (assigned_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_committee_id ON office_appointments (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_designation_id_functional_area_id_scope ON office_appointments (designation_id, functional_area_id, scope_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_functional_area_id ON office_appointments (functional_area_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_group_id ON office_appointments (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_section_id ON office_appointments (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    CREATE INDEX ix_office_appointments_user_id_is_active_effective_from_utc_ef ON office_appointments (user_id, is_active, effective_from_utc, effective_to_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE committees ADD CONSTRAINT fk_committees_groups_parent_group_id FOREIGN KEY (parent_group_id) REFERENCES groups (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE committees ADD CONSTRAINT fk_committees_sections_parent_section_id FOREIGN KEY (parent_section_id) REFERENCES sections (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE designations ADD CONSTRAINT fk_designations_functional_areas_functional_area_id FOREIGN KEY (functional_area_id) REFERENCES functional_areas (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    ALTER TABLE groups ADD CONSTRAINT fk_groups_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261006050816_AddFunctionalAreasOfficeAppointmentsAndOrgScopes', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    DROP TABLE user_designation_assignments;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    ALTER TABLE frb_assessment_snapshots DROP COLUMN beneficiary;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    ALTER TABLE frb_assessment_snapshots DROP COLUMN fixed_amount;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    ALTER TABLE frb_assessment_snapshots DROP COLUMN rate;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    ALTER TABLE frb_assessment_rules DROP COLUMN beneficiary;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    ALTER TABLE frb_assessment_rules DROP COLUMN fixed_amount;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    ALTER TABLE frb_assessment_rules DROP COLUMN rate;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261008075336_CleanV1Model') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261008075336_CleanV1Model', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE TABLE currencies (
        id uuid NOT NULL,
        code character varying(3) NOT NULL,
        name character varying(100) NOT NULL,
        symbol character varying(10) NOT NULL,
        decimal_places integer NOT NULL,
        is_base_currency boolean NOT NULL,
        is_active boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_currencies PRIMARY KEY (id),
        CONSTRAINT ak_currencies_code UNIQUE (code),
        CONSTRAINT ck_currencies_decimal_places CHECK (decimal_places BETWEEN 0 AND 6)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE TABLE exchange_rates (
        id uuid NOT NULL,
        from_currency_code character varying(3) NOT NULL,
        to_currency_code character varying(3) NOT NULL,
        rate numeric(28,12) NOT NULL,
        effective_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone,
        source_kind character varying(20) NOT NULL,
        source_reference character varying(200) NOT NULL,
        reason character varying(1000),
        is_approved boolean NOT NULL,
        approved_by_id uuid,
        approved_at_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_exchange_rates PRIMARY KEY (id),
        CONSTRAINT ck_exchange_rates_dates CHECK (expires_at_utc IS NULL OR expires_at_utc > effective_at_utc),
        CONSTRAINT ck_exchange_rates_pair CHECK (from_currency_code <> to_currency_code),
        CONSTRAINT ck_exchange_rates_positive CHECK (rate > 0),
        CONSTRAINT fk_exchange_rates_currencies_from_currency_code FOREIGN KEY (from_currency_code) REFERENCES currencies (code) ON DELETE RESTRICT,
        CONSTRAINT fk_exchange_rates_currencies_to_currency_code FOREIGN KEY (to_currency_code) REFERENCES currencies (code) ON DELETE RESTRICT,
        CONSTRAINT fk_exchange_rates_users_approved_by_id FOREIGN KEY (approved_by_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE TABLE fcy_management_settings (
        id uuid NOT NULL,
        is_enabled boolean NOT NULL,
        base_currency_code character varying(3) NOT NULL,
        rate_source_mode character varying(40) NOT NULL,
        provider_code character varying(100),
        allow_manual_rate_override boolean NOT NULL,
        require_approval_for_manual_override boolean NOT NULL,
        allow_last_known_rate_fallback boolean NOT NULL,
        maximum_rate_age_hours integer NOT NULL,
        override_tolerance_percent numeric(9,4) NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_fcy_management_settings PRIMARY KEY (id),
        CONSTRAINT ck_fcy_settings_override_tolerance CHECK (override_tolerance_percent BETWEEN 0 AND 100),
        CONSTRAINT ck_fcy_settings_rate_age CHECK (maximum_rate_age_hours BETWEEN 1 AND 8760),
        CONSTRAINT fk_fcy_management_settings_currencies_base_currency_code FOREIGN KEY (base_currency_code) REFERENCES currencies (code) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE UNIQUE INDEX ix_currencies_code ON currencies (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE INDEX ix_exchange_rates_approved_by_id ON exchange_rates (approved_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE UNIQUE INDEX ix_exchange_rates_from_currency_code_to_currency_code_effectiv ON exchange_rates (from_currency_code, to_currency_code, effective_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE INDEX ix_exchange_rates_from_currency_code_to_currency_code_is_appro ON exchange_rates (from_currency_code, to_currency_code, is_approved, effective_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE INDEX ix_exchange_rates_to_currency_code ON exchange_rates (to_currency_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    CREATE INDEX ix_fcy_management_settings_base_currency_code ON fcy_management_settings (base_currency_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    INSERT INTO currencies (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
    VALUES ('10000000-0000-0000-0000-000000000001', 'ZMW', 'Zambian Kwacha', 'K', 2, TRUE, TRUE, TIMESTAMPTZ '2026-10-14T00:00:00Z', TIMESTAMPTZ '2026-10-14T00:00:00Z', 1);
    INSERT INTO currencies (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
    VALUES ('10000000-0000-0000-0000-000000000002', 'USD', 'United States Dollar', '$', 2, FALSE, TRUE, TIMESTAMPTZ '2026-10-14T00:00:00Z', TIMESTAMPTZ '2026-10-14T00:00:00Z', 1);
    INSERT INTO currencies (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
    VALUES ('10000000-0000-0000-0000-000000000003', 'EUR', 'Euro', '€', 2, FALSE, TRUE, TIMESTAMPTZ '2026-10-14T00:00:00Z', TIMESTAMPTZ '2026-10-14T00:00:00Z', 1);
    INSERT INTO currencies (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
    VALUES ('10000000-0000-0000-0000-000000000004', 'GBP', 'Pound Sterling', '£', 2, FALSE, TRUE, TIMESTAMPTZ '2026-10-14T00:00:00Z', TIMESTAMPTZ '2026-10-14T00:00:00Z', 1);
    INSERT INTO currencies (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
    VALUES ('10000000-0000-0000-0000-000000000005', 'ZAR', 'South African Rand', 'R', 2, FALSE, TRUE, TIMESTAMPTZ '2026-10-14T00:00:00Z', TIMESTAMPTZ '2026-10-14T00:00:00Z', 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    INSERT INTO fcy_management_settings (id, is_enabled, base_currency_code, rate_source_mode, provider_code, allow_manual_rate_override, require_approval_for_manual_override, allow_last_known_rate_fallback, maximum_rate_age_hours, override_tolerance_percent, created_date, updated_date, version)
    VALUES ('20000000-0000-0000-0000-000000000001', FALSE, 'ZMW', 'MANUAL_ONLY', NULL, FALSE, TRUE, TRUE, 24, 2.0, TIMESTAMPTZ '2026-10-14T00:00:00Z', TIMESTAMPTZ '2026-10-14T00:00:00Z', 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014081740_AddFcyManagement') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014081740_AddFcyManagement', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    DROP INDEX ix_contributions_finalized_financial_record_book_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD contributor_name_snapshot character varying(250);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD currency_code character varying(3) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD exchange_rate_snapshot numeric(28,12) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD member_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD membership_number_snapshot character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD origin_type character varying(30) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD original_currency_amount numeric(18,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD payment_method_definition_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD reverses_contribution_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD service_collection_entry_line_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contribution_allocations ADD contribution_type_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE member_aud (
        id uuid NOT NULL,
        member_id uuid NOT NULL,
        action character varying(50) NOT NULL,
        changes_json text NOT NULL,
        performed_by_id uuid,
        performed_at_utc timestamp with time zone NOT NULL,
        correlation_id character varying(100) NOT NULL,
        CONSTRAINT pk_member_aud PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE members (
        id uuid NOT NULL,
        membership_number character varying(50) NOT NULL,
        title character varying(20),
        first_name character varying(100) NOT NULL,
        middle_names character varying(100),
        last_name character varying(100) NOT NULL,
        preferred_name character varying(200),
        sex character varying(30),
        date_of_birth date,
        phone_number character varying(50),
        email character varying(255),
        address character varying(500),
        section_id uuid,
        group_id uuid,
        membership_type character varying(50) NOT NULL,
        status character varying(30) NOT NULL,
        joined_on date,
        is_active boolean NOT NULL,
        is_archived boolean NOT NULL,
        archived_at_utc timestamp with time zone,
        archived_by_id uuid,
        archive_reason character varying(500),
        record_source character varying(30) NOT NULL,
        synchronization_status character varying(30) NOT NULL,
        last_synchronized_at_utc timestamp with time zone,
        normalized_search_name character varying(200) NOT NULL,
        normalized_phone_number character varying(50),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_members PRIMARY KEY (id),
        CONSTRAINT fk_members_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE SET NULL,
        CONSTRAINT fk_members_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE membership_integration_configurations (
        id uuid NOT NULL,
        provider_code character varying(50) NOT NULL,
        endpoint character varying(300),
        is_enabled boolean NOT NULL,
        use_local_fallback boolean NOT NULL,
        timeout_seconds integer NOT NULL,
        field_ownership_mode character varying(30) NOT NULL,
        last_successful_sync_at_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_membership_integration_configurations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE membership_sync_logs (
        id uuid NOT NULL,
        provider_code character varying(50) NOT NULL,
        started_at_utc timestamp with time zone NOT NULL,
        completed_at_utc timestamp with time zone,
        status character varying(30) NOT NULL,
        read_count integer NOT NULL,
        created_count integer NOT NULL,
        updated_count integer NOT NULL,
        conflict_count integer NOT NULL,
        error text,
        initiated_by_id uuid,
        correlation_id character varying(100) NOT NULL,
        CONSTRAINT pk_membership_sync_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE payment_method_definitions (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        name character varying(100) NOT NULL,
        category character varying(30) NOT NULL,
        requires_reference boolean NOT NULL,
        requires_bank_account boolean NOT NULL,
        requires_attachment boolean NOT NULL,
        supports_foreign_currency boolean NOT NULL,
        is_active boolean NOT NULL,
        display_order integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_method_definitions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_aud (
        id uuid NOT NULL,
        service_collection_id uuid NOT NULL,
        action character varying(60) NOT NULL,
        changes_json text NOT NULL,
        performed_by_id uuid,
        performed_at_utc timestamp with time zone NOT NULL,
        correlation_id character varying(100) NOT NULL,
        CONSTRAINT pk_service_collection_aud PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_types (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        name character varying(120) NOT NULL,
        description character varying(500),
        is_active boolean NOT NULL,
        display_order integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_types PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE member_external_identities (
        id uuid NOT NULL,
        member_id uuid NOT NULL,
        provider_code character varying(50) NOT NULL,
        external_member_id character varying(200) NOT NULL,
        external_membership_number character varying(100),
        external_version character varying(200),
        last_synchronized_at_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_member_external_identities PRIMARY KEY (id),
        CONSTRAINT fk_member_external_identities_members_member_id FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE membership_status_history (
        id uuid NOT NULL,
        member_id uuid NOT NULL,
        previous_status character varying(30) NOT NULL,
        new_status character varying(30) NOT NULL,
        effective_at_utc timestamp with time zone NOT NULL,
        reason character varying(500) NOT NULL,
        changed_by_id uuid NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_membership_status_history PRIMARY KEY (id),
        CONSTRAINT fk_membership_status_history_members_member_id FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE CASCADE,
        CONSTRAINT fk_membership_status_history_users_changed_by_id FOREIGN KEY (changed_by_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_events (
        id uuid NOT NULL,
        service_type_id uuid NOT NULL,
        starts_at_utc timestamp with time zone NOT NULL,
        sequence_name character varying(120),
        description character varying(200),
        location character varying(200),
        section_id uuid,
        group_id uuid,
        committee_id uuid,
        attendance integer,
        status character varying(30) NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_events PRIMARY KEY (id),
        CONSTRAINT fk_service_events_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_events_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_events_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_events_service_types_service_type_id FOREIGN KEY (service_type_id) REFERENCES service_types (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collections (
        id uuid NOT NULL,
        collection_number character varying(50) NOT NULL,
        service_event_id uuid NOT NULL,
        contribution_source_id uuid NOT NULL,
        section_id uuid,
        group_id uuid,
        committee_id uuid,
        base_currency_code character varying(3) NOT NULL,
        status character varying(30) NOT NULL,
        purpose_total numeric(18,2) NOT NULL,
        tender_total numeric(18,2) NOT NULL,
        variance numeric(18,2) NOT NULL,
        notes character varying(2000),
        variance_explanation character varying(1000),
        submitted_at_utc timestamp with time zone,
        submitted_by_id uuid,
        approved_at_utc timestamp with time zone,
        approved_by_id uuid,
        returned_at_utc timestamp with time zone,
        returned_by_id uuid,
        review_comments character varying(1000),
        archived_at_utc timestamp with time zone,
        archived_by_id uuid,
        archive_reason character varying(500),
        finalized_financial_record_book_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_collections PRIMARY KEY (id),
        CONSTRAINT ck_service_collections_scope CHECK (((CASE WHEN section_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN group_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN committee_id IS NOT NULL THEN 1 ELSE 0 END)) <= 1),
        CONSTRAINT fk_service_collections_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collections_contribution_sources_contribution_sourc FOREIGN KEY (contribution_source_id) REFERENCES contribution_sources (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collections_financial_record_books_finalized_financ FOREIGN KEY (finalized_financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collections_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collections_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collections_service_events_service_event_id FOREIGN KEY (service_event_id) REFERENCES service_events (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE financial_record_book_service_collections (
        id uuid NOT NULL,
        financial_record_book_id uuid NOT NULL,
        service_collection_id uuid NOT NULL,
        collection_number_snapshot character varying(50) NOT NULL,
        purpose_total_snapshot numeric(18,2) NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_financial_record_book_service_collections PRIMARY KEY (id),
        CONSTRAINT fk_financial_record_book_service_collections_financial_record_ FOREIGN KEY (financial_record_book_id) REFERENCES financial_record_books (id) ON DELETE CASCADE,
        CONSTRAINT fk_financial_record_book_service_collections_service_collectio FOREIGN KEY (service_collection_id) REFERENCES service_collections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_atm (
        id uuid NOT NULL,
        document_type character varying(50) NOT NULL,
        content_hash character varying(128),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        file_name text NOT NULL,
        file_path text NOT NULL,
        mime_type text NOT NULL,
        size bigint NOT NULL,
        uploaded_by_id uuid,
        uploaded_at timestamp with time zone NOT NULL,
        description text,
        CONSTRAINT pk_service_collection_atm PRIMARY KEY (id),
        CONSTRAINT fk_service_collection_atm_service_collections_entity_id FOREIGN KEY (entity_id) REFERENCES service_collections (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_denominations (
        id uuid NOT NULL,
        service_collection_id uuid NOT NULL,
        currency_code character varying(3) NOT NULL,
        denomination numeric(18,4) NOT NULL,
        quantity integer NOT NULL,
        original_amount numeric(18,4) NOT NULL,
        exchange_rate numeric(28,12) NOT NULL,
        base_equivalent numeric(18,2) NOT NULL,
        exchange_rate_id uuid,
        is_manual_rate_override boolean NOT NULL,
        override_reason character varying(500),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_collection_denominations PRIMARY KEY (id),
        CONSTRAINT fk_service_collection_denominations_currencies_currency_code FOREIGN KEY (currency_code) REFERENCES currencies (code) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_denominations_exchange_rates_exchange_ra FOREIGN KEY (exchange_rate_id) REFERENCES exchange_rates (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_denominations_service_collections_servic FOREIGN KEY (service_collection_id) REFERENCES service_collections (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_entries (
        id uuid NOT NULL,
        service_collection_id uuid NOT NULL,
        party_type character varying(30) NOT NULL,
        member_id uuid,
        membership_number_snapshot character varying(100),
        contributor_name_snapshot character varying(250) NOT NULL,
        contributor_reference character varying(100),
        notes character varying(500),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_collection_entries PRIMARY KEY (id),
        CONSTRAINT fk_service_collection_entries_members_member_id FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_entries_service_collections_service_coll FOREIGN KEY (service_collection_id) REFERENCES service_collections (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_lck (
        id uuid NOT NULL,
        lock_type character varying(30) NOT NULL,
        released_at_utc timestamp with time zone,
        released_by_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        entity_id uuid NOT NULL,
        locked_by_id uuid NOT NULL,
        locked_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        lock_reason text,
        CONSTRAINT pk_service_collection_lck PRIMARY KEY (id),
        CONSTRAINT fk_service_collection_lck_service_collections_entity_id FOREIGN KEY (entity_id) REFERENCES service_collections (id) ON DELETE CASCADE,
        CONSTRAINT fk_service_collection_lck_users_locked_by_id FOREIGN KEY (locked_by_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_participants (
        id uuid NOT NULL,
        service_collection_id uuid NOT NULL,
        user_id uuid NOT NULL,
        responsibility character varying(50) NOT NULL,
        completed_at_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_collection_participants PRIMARY KEY (id),
        CONSTRAINT fk_service_collection_participants_service_collections_service FOREIGN KEY (service_collection_id) REFERENCES service_collections (id) ON DELETE CASCADE,
        CONSTRAINT fk_service_collection_participants_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_tenders (
        id uuid NOT NULL,
        service_collection_id uuid NOT NULL,
        payment_method_definition_id uuid NOT NULL,
        currency_code character varying(3) NOT NULL,
        original_amount numeric(18,4) NOT NULL,
        exchange_rate numeric(28,12) NOT NULL,
        base_equivalent numeric(18,2) NOT NULL,
        exchange_rate_id uuid,
        is_manual_rate_override boolean NOT NULL,
        override_reason character varying(500),
        reference character varying(200),
        bank_account_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_collection_tenders PRIMARY KEY (id),
        CONSTRAINT fk_service_collection_tenders_bank_accounts_bank_account_id FOREIGN KEY (bank_account_id) REFERENCES bank_accounts (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_tenders_currencies_currency_code FOREIGN KEY (currency_code) REFERENCES currencies (code) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_tenders_exchange_rates_exchange_rate_id FOREIGN KEY (exchange_rate_id) REFERENCES exchange_rates (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_tenders_payment_method_definitions_payme FOREIGN KEY (payment_method_definition_id) REFERENCES payment_method_definitions (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_tenders_service_collections_service_coll FOREIGN KEY (service_collection_id) REFERENCES service_collections (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE TABLE service_collection_entry_lines (
        id uuid NOT NULL,
        service_collection_entry_id uuid NOT NULL,
        contribution_type_id uuid NOT NULL,
        amount numeric(18,2) NOT NULL,
        notes character varying(500),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_service_collection_entry_lines PRIMARY KEY (id),
        CONSTRAINT ck_service_collection_entry_lines_amount CHECK (amount > 0),
        CONSTRAINT fk_service_collection_entry_lines_contribution_types_contribut FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE RESTRICT,
        CONSTRAINT fk_service_collection_entry_lines_service_collection_entries_s FOREIGN KEY (service_collection_entry_id) REFERENCES service_collection_entries (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_contributions_finalized_financial_record_book_id ON contributions (finalized_financial_record_book_id) WHERE finalized_financial_record_book_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_contributions_member_id_contribution_date ON contributions (member_id, contribution_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_contributions_payment_method_definition_id ON contributions (payment_method_definition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_contributions_reverses_contribution_id ON contributions (reverses_contribution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_contributions_service_collection_entry_line_id ON contributions (service_collection_entry_line_id) WHERE service_collection_entry_line_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_contribution_allocations_contribution_type_id ON contribution_allocations (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_financial_record_book_service_collections_financial_record_ ON financial_record_book_service_collections (financial_record_book_id, service_collection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_financial_record_book_service_collections_service_collectio ON financial_record_book_service_collections (service_collection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_member_aud_member_id_performed_at_utc ON member_aud (member_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_member_external_identities_member_id ON member_external_identities (member_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_member_external_identities_provider_code_external_member_id ON member_external_identities (provider_code, external_member_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_members_group_id ON members (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_members_membership_number ON members (membership_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_members_normalized_phone_number ON members (normalized_phone_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_members_normalized_search_name ON members (normalized_search_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_members_section_id ON members (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_members_status_is_archived ON members (status, is_archived);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_membership_integration_configurations_provider_code ON membership_integration_configurations (provider_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_membership_status_history_changed_by_id ON membership_status_history (changed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_membership_status_history_member_id_effective_at_utc ON membership_status_history (member_id, effective_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_membership_sync_logs_provider_code_started_at_utc ON membership_sync_logs (provider_code, started_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_payment_method_definitions_code ON payment_method_definitions (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_atm_entity_id ON service_collection_atm (entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_aud_service_collection_id_performed_at_u ON service_collection_aud (service_collection_id, performed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_denominations_currency_code ON service_collection_denominations (currency_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_denominations_exchange_rate_id ON service_collection_denominations (exchange_rate_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_service_collection_denominations_service_collection_id_curr ON service_collection_denominations (service_collection_id, currency_code, denomination);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_entries_member_id ON service_collection_entries (member_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_entries_service_collection_id_member_id ON service_collection_entries (service_collection_id, member_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_entry_lines_contribution_type_id ON service_collection_entry_lines (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_entry_lines_service_collection_entry_id_ ON service_collection_entry_lines (service_collection_entry_id, contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_lck_entity_id_released_at_utc ON service_collection_lck (entity_id, released_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_lck_locked_by_id ON service_collection_lck (locked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_service_collection_participants_service_collection_id_user_ ON service_collection_participants (service_collection_id, user_id, responsibility);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_participants_user_id ON service_collection_participants (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_tenders_bank_account_id ON service_collection_tenders (bank_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_tenders_currency_code ON service_collection_tenders (currency_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_tenders_exchange_rate_id ON service_collection_tenders (exchange_rate_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_tenders_payment_method_definition_id ON service_collection_tenders (payment_method_definition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collection_tenders_service_collection_id ON service_collection_tenders (service_collection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_service_collections_collection_number ON service_collections (collection_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_committee_id ON service_collections (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_contribution_source_id ON service_collections (contribution_source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_finalized_financial_record_book_id ON service_collections (finalized_financial_record_book_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_group_id ON service_collections (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_section_id ON service_collections (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_service_event_id ON service_collections (service_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_collections_status_created_date ON service_collections (status, created_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_events_committee_id ON service_events (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_events_group_id ON service_events (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_events_section_id ON service_events (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_events_service_type_id ON service_events (service_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE INDEX ix_service_events_starts_at_utc_service_type_id ON service_events (starts_at_utc, service_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    CREATE UNIQUE INDEX ix_service_types_code ON service_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contribution_allocations ADD CONSTRAINT fk_contribution_allocations_contribution_types_contribution_ty FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_contributions_reverses_contribution_id FOREIGN KEY (reverses_contribution_id) REFERENCES contributions (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_members_member_id FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_payment_method_definitions_payment_method_def FOREIGN KEY (payment_method_definition_id) REFERENCES payment_method_definitions (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_service_collection_entry_lines_service_collec FOREIGN KEY (service_collection_entry_line_id) REFERENCES service_collection_entry_lines (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014084313_AddMembershipAndServiceCollections') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014084313_AddMembershipAndServiceCollections', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014114740_AddAssetHireRevenueAndFcyProvider') THEN
    UPDATE fcy_management_settings
    SET rate_source_mode = 'PROVIDER_WITH_MANUAL_FALLBACK',
        provider_code = 'ALL_RATES_TODAY_BOZ',
        updated_date = NOW(),
        version = version + 1
    WHERE rate_source_mode = 'MANUAL_ONLY' AND provider_code IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014114740_AddAssetHireRevenueAndFcyProvider') THEN
    ALTER TABLE contributions ADD asset_checkout_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014114740_AddAssetHireRevenueAndFcyProvider') THEN
    CREATE UNIQUE INDEX ix_contributions_asset_checkout_id ON contributions (asset_checkout_id) WHERE asset_checkout_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014114740_AddAssetHireRevenueAndFcyProvider') THEN
    ALTER TABLE contributions ADD CONSTRAINT fk_contributions_asset_checkouts_asset_checkout_id FOREIGN KEY (asset_checkout_id) REFERENCES asset_checkouts (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014114740_AddAssetHireRevenueAndFcyProvider') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014114740_AddAssetHireRevenueAndFcyProvider', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014123000_ExpandMembershipAndCollectionRolePermissions') THEN
    UPDATE role_permissions
    SET permissions_json = (
            permissions_json::jsonb ||
            '["members.view","members.manage","members.financial-activity","membership.sync.manage","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit"]'::jsonb
        )::text,
        updated_date = CURRENT_TIMESTAMP,
        version = version + 1
    WHERE role = 'Admin';

    UPDATE role_permissions
    SET permissions_json = (
            permissions_json::jsonb ||
            '["members.view","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit"]'::jsonb
        )::text,
        updated_date = CURRENT_TIMESTAMP,
        version = version + 1
    WHERE role IN ('SeniorRep', 'GroupRep');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014123000_ExpandMembershipAndCollectionRolePermissions') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014123000_ExpandMembershipAndCollectionRolePermissions', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014190925_RedesignServiceCollectionCapture') THEN
    ALTER TABLE service_events ADD children_attendance integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014190925_RedesignServiceCollectionCapture') THEN
    ALTER TABLE service_events ADD officiating_minister character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014190925_RedesignServiceCollectionCapture') THEN
    ALTER TABLE service_events ADD teacher_attendance integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014190925_RedesignServiceCollectionCapture') THEN
    ALTER TABLE contribution_types ADD is_default_service_collection_column boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014190925_RedesignServiceCollectionCapture') THEN
    UPDATE contribution_types
    SET is_default_service_collection_column = TRUE
    WHERE upper(code) IN ('GENERAL', 'GENERAL_OFFERING', 'BUILDING', 'BUILDING_OFFERING');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014190925_RedesignServiceCollectionCapture') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014190925_RedesignServiceCollectionCapture', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014200000_AddCnyCurrency') THEN
    INSERT INTO currencies
        (id, code, name, symbol, decimal_places, is_base_currency, is_active, created_date, updated_date, version)
    VALUES
        ('10000000-0000-0000-0000-000000000006', 'CNY', 'Chinese Yuan', '¥', 2, FALSE, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
    ON CONFLICT (code) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014200000_AddCnyCurrency') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014200000_AddCnyCurrency', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014203000_AddServiceCollectionSignaturesAndPosProof') THEN
    ALTER TABLE service_collection_participants ADD signature_data text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014203000_AddServiceCollectionSignaturesAndPosProof') THEN
    UPDATE payment_method_definitions
    SET requires_attachment = TRUE,
        updated_date = CURRENT_TIMESTAMP,
        version = version + 1
    WHERE code = 'POS_CARD' AND requires_attachment = FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261014203000_AddServiceCollectionSignaturesAndPosProof') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261014203000_AddServiceCollectionSignaturesAndPosProof', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261015072944_AddContributionCashDenominations') THEN
    CREATE TABLE IF NOT EXISTS contribution_denominations (
        id uuid NOT NULL,
        contribution_id uuid NOT NULL,
        denomination numeric(18,4) NOT NULL,
        quantity integer NOT NULL,
        amount numeric(18,2) NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone NULL,
        created_by_id uuid NULL,
        updated_by_id uuid NULL,
        version integer NOT NULL,
        CONSTRAINT pk_contribution_denominations PRIMARY KEY (id),
        CONSTRAINT ck_contribution_denominations_amount CHECK (amount > 0),
        CONSTRAINT ck_contribution_denominations_denomination CHECK (denomination > 0),
        CONSTRAINT ck_contribution_denominations_quantity CHECK (quantity > 0),
        CONSTRAINT fk_contribution_denominations_contributions_contribution_id
            FOREIGN KEY (contribution_id) REFERENCES contributions (id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IF NOT EXISTS ix_contribution_denominations_contribution_id_denomination
        ON contribution_denominations (contribution_id, denomination);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261015072944_AddContributionCashDenominations') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261015072944_AddContributionCashDenominations', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261016080000_RepairContributionDenominationSchema') THEN
    ALTER TABLE contribution_denominations
    ADD COLUMN IF NOT EXISTS last_processed_time_stamp timestamp with time zone NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261016080000_RepairContributionDenominationSchema') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261016080000_RepairContributionDenominationSchema', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261016120000_AddFrbAssessmentSelection') THEN
    ALTER TABLE financial_record_books ADD assessment_rule_ids_json jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261016120000_AddFrbAssessmentSelection') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261016120000_AddFrbAssessmentSelection', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261017232235_IndependentFinancialModules') THEN
    ALTER TABLE financial_record_book_service_collections ADD lines_snapshot_json jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261017232235_IndependentFinancialModules') THEN
    ALTER TABLE financial_record_book_service_collections ADD service_date_snapshot timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261017232235_IndependentFinancialModules') THEN
    ALTER TABLE financial_record_book_service_collections ADD status_snapshot character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261017232235_IndependentFinancialModules') THEN
    ALTER TABLE financial_record_book_contributions ADD lines_snapshot_json jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261017232235_IndependentFinancialModules') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261017232235_IndependentFinancialModules', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020065616_AddEmailTemplates') THEN
    CREATE TABLE email_templates (
        id uuid NOT NULL,
        template_key character varying(100) NOT NULL,
        name character varying(150) NOT NULL,
        description character varying(500),
        is_enabled boolean NOT NULL,
        subject character varying(255) NOT NULL,
        html_body text NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_email_templates PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020065616_AddEmailTemplates') THEN
    CREATE UNIQUE INDEX ix_email_templates_template_key ON email_templates (template_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020065616_AddEmailTemplates') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261020065616_AddEmailTemplates', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020072457_AddSmsAndWhatsAppTemplates') THEN
    ALTER TABLE email_templates ADD is_sms_enabled boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020072457_AddSmsAndWhatsAppTemplates') THEN
    ALTER TABLE email_templates ADD is_whats_app_enabled boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020072457_AddSmsAndWhatsAppTemplates') THEN
    ALTER TABLE email_templates ADD sms_body text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020072457_AddSmsAndWhatsAppTemplates') THEN
    ALTER TABLE email_templates ADD whats_app_body text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020072457_AddSmsAndWhatsAppTemplates') THEN
    UPDATE email_templates
    SET sms_body = CASE template_key
        WHEN 'account-activated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been activated. You can now sign in.'
        WHEN 'account-deactivated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been deactivated. Contact your administrator if this is unexpected.'
        WHEN 'requisition-status' THEN 'Requisition {{reference}} is now {{status}}. {{note}}'
        WHEN 'contribution-status' THEN 'Contribution {{reference}} is now {{status}}. {{note}}'
        WHEN 'asset-request-status' THEN 'Asset request {{reference}} for {{quantity}} x {{asset_name}} is now {{status}}. {{note}}'
        WHEN 'approval-required' THEN E'Approval required\n\n{{process_name}}: {{reference}}\n{{summary}}\n\nAvailable actions: {{actions}}\n\nOpen request:\n{{approval_url}}'
        ELSE '' END,
        whats_app_body = CASE template_key
        WHEN 'account-activated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been activated. You can now sign in.'
        WHEN 'account-deactivated' THEN 'Hi {{recipient_name}}, your {{church_name}} account has been deactivated. Contact your administrator if this is unexpected.'
        WHEN 'requisition-status' THEN 'Requisition {{reference}} is now {{status}}. {{note}}'
        WHEN 'contribution-status' THEN 'Contribution {{reference}} is now {{status}}. {{note}}'
        WHEN 'asset-request-status' THEN 'Asset request {{reference}} for {{quantity}} x {{asset_name}} is now {{status}}. {{note}}'
        WHEN 'approval-required' THEN E'Approval required\n\n{{process_name}}: {{reference}}\n{{summary}}\n\nAvailable actions: {{actions}}\n\nOpen request:\n{{approval_url}}'
        ELSE '' END,
        is_sms_enabled = template_key NOT IN ('password-reset', 'temporary-password'),
        is_whats_app_enabled = template_key NOT IN ('password-reset', 'temporary-password');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261020072457_AddSmsAndWhatsAppTemplates') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261020072457_AddSmsAndWhatsAppTemplates', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE digital_giving_configurations (
        id uuid NOT NULL,
        is_enabled boolean NOT NULL,
        public_slug character varying(80) NOT NULL,
        currency_code character varying(3) NOT NULL,
        minimum_amount numeric(18,2) NOT NULL,
        maximum_amount numeric(18,2),
        contribution_source_id uuid NOT NULL,
        welcome_message character varying(250),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_giving_configurations PRIMARY KEY (id),
        CONSTRAINT ck_digital_giving_configuration_maximum CHECK (maximum_amount IS NULL OR maximum_amount >= minimum_amount),
        CONSTRAINT ck_digital_giving_configuration_minimum CHECK (minimum_amount > 0),
        CONSTRAINT fk_digital_giving_configurations_contribution_sources_contribu FOREIGN KEY (contribution_source_id) REFERENCES contribution_sources (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE payment_provider_configurations (
        id uuid NOT NULL,
        provider_key character varying(80) NOT NULL,
        display_name character varying(120) NOT NULL,
        provider_kind character varying(30) NOT NULL,
        environment character varying(30) NOT NULL,
        is_enabled boolean NOT NULL,
        protected_configuration text NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_provider_configurations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE digital_giving_type_settings (
        id uuid NOT NULL,
        digital_giving_configuration_id uuid NOT NULL,
        contribution_type_id uuid NOT NULL,
        is_publicly_available boolean NOT NULL,
        display_order integer NOT NULL,
        group_id uuid,
        section_id uuid,
        committee_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_giving_type_settings PRIMARY KEY (id),
        CONSTRAINT ck_digital_giving_type_destination CHECK (NOT is_publicly_available OR ((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) = 1),
        CONSTRAINT fk_digital_giving_type_settings_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_giving_type_settings_contribution_types_contributio FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_giving_type_settings_digital_giving_configurations_ FOREIGN KEY (digital_giving_configuration_id) REFERENCES digital_giving_configurations (id) ON DELETE CASCADE,
        CONSTRAINT fk_digital_giving_type_settings_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_giving_type_settings_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE digital_payment_transactions (
        id uuid NOT NULL,
        payment_reference character varying(60) NOT NULL,
        idempotency_key character varying(120) NOT NULL,
        public_status_token_hash character varying(128) NOT NULL,
        giver_type character varying(20) NOT NULL,
        member_id uuid,
        contributor_name_snapshot character varying(250),
        protected_payer_data text,
        payment_method_definition_id uuid NOT NULL,
        payment_provider_configuration_id uuid,
        provider_transaction_id character varying(200),
        status character varying(30) NOT NULL,
        settlement_status character varying(30) NOT NULL,
        currency_code character varying(3) NOT NULL,
        gross_amount numeric(18,2) NOT NULL,
        provider_fee numeric(18,2) NOT NULL,
        net_settlement_amount numeric(18,2) NOT NULL,
        initiated_at_utc timestamp with time zone,
        provider_completed_at_utc timestamp with time zone,
        settled_at_utc timestamp with time zone,
        expires_at_utc timestamp with time zone,
        failure_reason character varying(1000),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_payment_transactions PRIMARY KEY (id),
        CONSTRAINT ck_digital_payment_transaction_amounts CHECK (gross_amount > 0 AND provider_fee >= 0 AND net_settlement_amount >= 0 AND provider_fee <= gross_amount),
        CONSTRAINT fk_digital_payment_transactions_members_member_id FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_payment_transactions_payment_method_definitions_pay FOREIGN KEY (payment_method_definition_id) REFERENCES payment_method_definitions (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_payment_transactions_payment_provider_configuration FOREIGN KEY (payment_provider_configuration_id) REFERENCES payment_provider_configurations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE payment_provider_routes (
        id uuid NOT NULL,
        payment_method_definition_id uuid NOT NULL,
        payment_provider_configuration_id uuid NOT NULL,
        provider_payment_method_code character varying(80) NOT NULL,
        priority integer NOT NULL,
        is_enabled boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_provider_routes PRIMARY KEY (id),
        CONSTRAINT ck_payment_provider_route_priority CHECK (priority >= 0),
        CONSTRAINT fk_payment_provider_routes_payment_method_definitions_payment_ FOREIGN KEY (payment_method_definition_id) REFERENCES payment_method_definitions (id) ON DELETE RESTRICT,
        CONSTRAINT fk_payment_provider_routes_payment_provider_configurations_pay FOREIGN KEY (payment_provider_configuration_id) REFERENCES payment_provider_configurations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE payment_webhook_events (
        id uuid NOT NULL,
        payment_provider_configuration_id uuid NOT NULL,
        event_key character varying(200) NOT NULL,
        payload_hash character varying(128) NOT NULL,
        signature_valid boolean NOT NULL,
        processing_status character varying(30) NOT NULL,
        correlation_id character varying(100),
        failure_reason character varying(1000),
        protected_payload text,
        received_at_utc timestamp with time zone NOT NULL,
        processed_at_utc timestamp with time zone,
        processing_attempts integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_webhook_events PRIMARY KEY (id),
        CONSTRAINT ck_payment_webhook_processing_attempts CHECK (processing_attempts >= 0),
        CONSTRAINT fk_payment_webhook_events_payment_provider_configurations_paym FOREIGN KEY (payment_provider_configuration_id) REFERENCES payment_provider_configurations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE digital_payment_events (
        id uuid NOT NULL,
        digital_payment_transaction_id uuid NOT NULL,
        event_type character varying(50) NOT NULL,
        previous_status character varying(30),
        new_status character varying(30) NOT NULL,
        source character varying(80) NOT NULL,
        correlation_id character varying(100),
        protected_details text,
        occurred_at_utc timestamp with time zone NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_payment_events PRIMARY KEY (id),
        CONSTRAINT fk_digital_payment_events_digital_payment_transactions_digital FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE TABLE digital_payment_lines (
        id uuid NOT NULL,
        digital_payment_transaction_id uuid NOT NULL,
        contribution_type_id uuid NOT NULL,
        amount numeric(18,2) NOT NULL,
        group_id uuid,
        section_id uuid,
        committee_id uuid,
        contribution_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_payment_lines PRIMARY KEY (id),
        CONSTRAINT ck_digital_payment_line_amount CHECK (amount > 0),
        CONSTRAINT ck_digital_payment_line_destination CHECK (((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) = 1),
        CONSTRAINT fk_digital_payment_lines_committees_committee_id FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_payment_lines_contribution_types_contribution_type_ FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_payment_lines_contributions_contribution_id FOREIGN KEY (contribution_id) REFERENCES contributions (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_payment_lines_digital_payment_transactions_digital_ FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions (id) ON DELETE CASCADE,
        CONSTRAINT fk_digital_payment_lines_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
        CONSTRAINT fk_digital_payment_lines_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_giving_configurations_contribution_source_id ON digital_giving_configurations (contribution_source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_giving_configurations_public_slug ON digital_giving_configurations (public_slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_giving_type_settings_committee_id ON digital_giving_type_settings (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_giving_type_settings_contribution_type_id ON digital_giving_type_settings (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_giving_type_settings_digital_giving_configuration_i ON digital_giving_type_settings (digital_giving_configuration_id, contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_giving_type_settings_group_id ON digital_giving_type_settings (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_giving_type_settings_section_id ON digital_giving_type_settings (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_events_digital_payment_transaction_id_occur ON digital_payment_events (digital_payment_transaction_id, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_lines_committee_id ON digital_payment_lines (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_payment_lines_contribution_id ON digital_payment_lines (contribution_id) WHERE contribution_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_lines_contribution_type_id ON digital_payment_lines (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_lines_digital_payment_transaction_id ON digital_payment_lines (digital_payment_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_lines_group_id ON digital_payment_lines (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_lines_section_id ON digital_payment_lines (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_payment_transactions_idempotency_key ON digital_payment_transactions (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_transactions_member_id ON digital_payment_transactions (member_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_transactions_payment_method_definition_id ON digital_payment_transactions (payment_method_definition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_payment_transactions_payment_provider_configuration ON digital_payment_transactions (payment_provider_configuration_id, provider_transaction_id) WHERE provider_transaction_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_payment_transactions_payment_reference ON digital_payment_transactions (payment_reference);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_digital_payment_transactions_public_status_token_hash ON digital_payment_transactions (public_status_token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_transactions_settlement_status_created_date ON digital_payment_transactions (settlement_status, created_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_digital_payment_transactions_status_created_date ON digital_payment_transactions (status, created_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_payment_provider_configurations_provider_key ON payment_provider_configurations (provider_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_payment_provider_routes_payment_method_definition_id_paymen ON payment_provider_routes (payment_method_definition_id, payment_provider_configuration_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_payment_provider_routes_payment_method_definition_id_priori ON payment_provider_routes (payment_method_definition_id, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_payment_provider_routes_payment_provider_configuration_id ON payment_provider_routes (payment_provider_configuration_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_payment_webhook_events_payload_hash ON payment_webhook_events (payload_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE UNIQUE INDEX ix_payment_webhook_events_payment_provider_configuration_id_ev ON payment_webhook_events (payment_provider_configuration_id, event_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    CREATE INDEX ix_payment_webhook_events_processing_status_received_at_utc ON payment_webhook_events (processing_status, received_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024184254_AddDigitalGivingPaymentFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261024184254_AddDigitalGivingPaymentFoundation', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    ALTER TABLE digital_payment_transactions ADD contribution_source_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    ALTER TABLE digital_payment_transactions ADD protected_public_status_token text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    ALTER TABLE digital_payment_transactions ADD request_fingerprint character varying(128) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    CREATE TABLE member_verification_challenges (
        id uuid NOT NULL,
        lookup_hash character varying(128) NOT NULL,
        member_id uuid,
        client_fingerprint_hash character varying(128) NOT NULL,
        otp_salt bytea NOT NULL,
        otp_hash bytea NOT NULL,
        attempt_count integer NOT NULL,
        maximum_attempts integer NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        is_verified boolean NOT NULL,
        verified_at_utc timestamp with time zone,
        verification_token_hash character varying(128),
        consumed_at_utc timestamp with time zone,
        delivery_channel character varying(30) NOT NULL,
        destination_hint character varying(80),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_member_verification_challenges PRIMARY KEY (id),
        CONSTRAINT ck_member_verification_attempt_count CHECK (attempt_count >= 0 AND maximum_attempts > 0),
        CONSTRAINT ck_member_verification_otp_material CHECK (length(otp_salt) >= 16 AND length(otp_hash) >= 32),
        CONSTRAINT fk_member_verification_challenges_members_member_id FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    UPDATE digital_payment_transactions AS payment
    SET contribution_source_id = configuration.contribution_source_id
    FROM (
        SELECT contribution_source_id
        FROM digital_giving_configurations
        ORDER BY created_date
        LIMIT 1
    ) AS configuration
    WHERE payment.contribution_source_id IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1 FROM digital_payment_transactions
            WHERE contribution_source_id IS NULL
        ) THEN
            RAISE EXCEPTION 'Cannot migrate existing digital payments: configure a Digital Giving contribution source first.';
        END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    ALTER TABLE digital_payment_transactions ALTER COLUMN contribution_source_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    CREATE INDEX ix_digital_payment_transactions_contribution_source_id ON digital_payment_transactions (contribution_source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    CREATE INDEX ix_member_verification_challenges_expires_at_utc ON member_verification_challenges (expires_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    CREATE INDEX ix_member_verification_challenges_lookup_hash_created_date ON member_verification_challenges (lookup_hash, created_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    CREATE INDEX ix_member_verification_challenges_member_id ON member_verification_challenges (member_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    CREATE INDEX ix_member_verification_challenges_verification_token_hash ON member_verification_challenges (verification_token_hash) WHERE verification_token_hash IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    ALTER TABLE digital_payment_transactions ADD CONSTRAINT fk_digital_payment_transactions_contribution_sources_contribut FOREIGN KEY (contribution_source_id) REFERENCES contribution_sources (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261024185811_AddDigitalGivingOrchestration') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261024185811_AddDigitalGivingOrchestration', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD is_enabled_for_digital_giving boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD default_digital_destination_type character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD require_destination_selection_for_digital_giving boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD digital_destination_eligibility_policy character varying(30) NOT NULL DEFAULT 'AS_CONFIGURED';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD allow_organization_wide_allocation boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD allow_section_allocation boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD allow_group_allocation boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD allow_committee_allocation boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD minimum_amount numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD maximum_amount numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    ALTER TABLE contribution_types ADD public_display_order integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    CREATE TABLE contribution_type_destination_policies (
        id uuid NOT NULL,
        contribution_type_id uuid NOT NULL,
        destination_type character varying(30) NOT NULL,
        section_id uuid,
        group_id uuid,
        committee_id uuid,
        is_eligible boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_contribution_type_destination_policies PRIMARY KEY (id),
        CONSTRAINT ck_destination_policy_single_destination CHECK ((CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END) <= 1),
        CONSTRAINT fk_contribution_type_destination_policies_contribution_types FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE CASCADE,
        CONSTRAINT fk_contribution_type_destination_policies_sections FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contribution_type_destination_policies_groups FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contribution_type_destination_policies_committees FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    CREATE INDEX ix_contribution_type_destination_policies_contribution_type_id ON contribution_type_destination_policies (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    CREATE INDEX ix_contribution_type_destination_policies_section_id ON contribution_type_destination_policies (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    CREATE INDEX ix_contribution_type_destination_policies_group_id ON contribution_type_destination_policies (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    CREATE INDEX ix_contribution_type_destination_policies_committee_id ON contribution_type_destination_policies (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261030120000_ExtendContributionTypeAndAllocationForDigitalGiving', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_giving_type_settings ADD allow_destination_selection boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_giving_type_settings ADD destination_eligibility_policy character varying(30) NOT NULL DEFAULT 'AS_CONFIGURED';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_giving_type_settings ADD destination_type character varying(20) NOT NULL DEFAULT 'ORGANIZATION';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_giving_type_settings
    DROP CONSTRAINT IF EXISTS ck_digital_giving_type_destination;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_giving_type_settings ADD CONSTRAINT ck_digital_giving_type_destination_updated CHECK (((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_payment_lines 
    DROP CONSTRAINT IF EXISTS ck_digital_payment_line_destination;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    ALTER TABLE digital_payment_lines ADD CONSTRAINT ck_digital_payment_line_destination_updated CHECK (((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE TABLE digital_payment_retries (
        id uuid NOT NULL,
        digital_payment_transaction_id uuid NOT NULL,
        retry_type character varying(50) NOT NULL,
        previous_status character varying(30) NOT NULL,
        reason character varying(500),
        attempt_number integer NOT NULL,
        attempted_at_utc timestamp with time zone,
        result_status character varying(30),
        result_message character varying(1000),
        initiated_by_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_payment_retries PRIMARY KEY (id),
        CONSTRAINT fk_digital_payment_retries_digital_payment_transactions FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions (id) ON DELETE CASCADE,
        CONSTRAINT fk_digital_payment_retries_users_initiated_by FOREIGN KEY (initiated_by_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE TABLE digital_payment_reversals (
        id uuid NOT NULL,
        digital_payment_transaction_id uuid NOT NULL,
        related_reversal_id uuid,
        reason character varying(500) NOT NULL,
        reversal_status character varying(30) NOT NULL,
        provider_refund_initiated boolean NOT NULL,
        provider_refund_transaction_id character varying(200),
        reversed_at_utc timestamp with time zone,
        reversed_by_id uuid,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_digital_payment_reversals PRIMARY KEY (id),
        CONSTRAINT fk_digital_payment_reversals_digital_payment_transactions FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions (id) ON DELETE CASCADE,
        CONSTRAINT fk_digital_payment_reversals_related_reversals FOREIGN KEY (related_reversal_id) REFERENCES digital_payment_reversals (id) ON DELETE SET NULL,
        CONSTRAINT fk_digital_payment_reversals_users_reversed_by FOREIGN KEY (reversed_by_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE INDEX ix_digital_payment_retries_digital_payment_transaction_id ON digital_payment_retries (digital_payment_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE INDEX ix_digital_payment_retries_initiated_by_id ON digital_payment_retries (initiated_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE INDEX ix_digital_payment_reversals_digital_payment_transaction_id ON digital_payment_reversals (digital_payment_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE INDEX ix_digital_payment_reversals_related_reversal_id ON digital_payment_reversals (related_reversal_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    CREATE INDEX ix_digital_payment_reversals_reversed_by_id ON digital_payment_reversals (reversed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030130000_ExtendDigitalPaymentAndAddRetryReversal') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261030130000_ExtendDigitalPaymentAndAddRetryReversal', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE TABLE organization_overviews (
        id uuid NOT NULL,
        organization_name character varying(200) NOT NULL,
        total_organization_wide_contributions numeric(18,2) NOT NULL,
        total_section_contributions numeric(18,2) NOT NULL,
        total_group_contributions numeric(18,2) NOT NULL,
        total_committee_contributions numeric(18,2) NOT NULL,
        total_contributions numeric(18,2) NOT NULL,
        active_section_count integer NOT NULL,
        active_group_count integer NOT NULL,
        active_committee_count integer NOT NULL,
        last_updated_utc timestamp with time zone,
        is_stale boolean NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_organization_overviews PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE TABLE organization_contribution_type_summaries (
        id uuid NOT NULL,
        contribution_type_id uuid NOT NULL,
        organization_overview_id uuid NOT NULL,
        total_amount numeric(18,2) NOT NULL,
        contribution_count integer NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_organization_contribution_type_summaries PRIMARY KEY (id),
        CONSTRAINT fk_organization_contribution_type_summaries_contribution_types FOREIGN KEY (contribution_type_id) REFERENCES contribution_types (id) ON DELETE CASCADE,
        CONSTRAINT fk_organization_contribution_type_summaries_organization_overviews FOREIGN KEY (organization_overview_id) REFERENCES organization_overviews (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE TABLE section_financial_summaries (
        id uuid NOT NULL,
        section_id uuid NOT NULL,
        direct_contributions numeric(18,2) NOT NULL,
        group_contributions numeric(18,2) NOT NULL,
        committee_contributions numeric(18,2) NOT NULL,
        total_contributions numeric(18,2) NOT NULL,
        active_group_count integer NOT NULL,
        active_committee_count integer NOT NULL,
        last_updated_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_section_financial_summaries PRIMARY KEY (id),
        CONSTRAINT fk_section_financial_summaries_sections FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE TABLE group_financial_summaries (
        id uuid NOT NULL,
        group_id uuid NOT NULL,
        direct_contributions numeric(18,2) NOT NULL,
        committee_contributions numeric(18,2) NOT NULL,
        total_contributions numeric(18,2) NOT NULL,
        active_committee_count integer NOT NULL,
        last_updated_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_group_financial_summaries PRIMARY KEY (id),
        CONSTRAINT fk_group_financial_summaries_groups FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE TABLE committee_financial_summaries (
        id uuid NOT NULL,
        committee_id uuid NOT NULL,
        total_contributions numeric(18,2) NOT NULL,
        contribution_count integer NOT NULL,
        last_updated_utc timestamp with time zone,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_committee_financial_summaries PRIMARY KEY (id),
        CONSTRAINT fk_committee_financial_summaries_committees FOREIGN KEY (committee_id) REFERENCES committees (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE INDEX ix_organization_contribution_type_summaries_contribution_type_id ON organization_contribution_type_summaries (contribution_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE INDEX ix_organization_contribution_type_summaries_organization_overview_id ON organization_contribution_type_summaries (organization_overview_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE UNIQUE INDEX ix_section_financial_summaries_section_id ON section_financial_summaries (section_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE UNIQUE INDEX ix_group_financial_summaries_group_id ON group_financial_summaries (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    CREATE UNIQUE INDEX ix_committee_financial_summaries_committee_id ON committee_financial_summaries (committee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030140000_AddOrganizationOverviewEntities') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261030140000_AddOrganizationOverviewEntities', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030150000_RepairContributionDestinationDefaults') THEN
    UPDATE contribution_types AS ct
    SET allow_organization_wide_allocation = EXISTS (
            SELECT 1 FROM contribution_allocations AS ca
            WHERE ca.contribution_type_id = ct.id
              AND ca.section_id IS NULL
              AND ca.group_id IS NULL
              AND ca.committee_id IS NULL),
        allow_section_allocation = EXISTS (
            SELECT 1 FROM contribution_allocations AS ca
            WHERE ca.contribution_type_id = ct.id AND ca.section_id IS NOT NULL),
        allow_group_allocation = EXISTS (
            SELECT 1 FROM contribution_allocations AS ca
            WHERE ca.contribution_type_id = ct.id AND ca.group_id IS NOT NULL),
        allow_committee_allocation = EXISTS (
            SELECT 1 FROM contribution_allocations AS ca
            WHERE ca.contribution_type_id = ct.id AND ca.committee_id IS NOT NULL)
    WHERE ct.allow_organization_wide_allocation = FALSE
      AND ct.allow_section_allocation = FALSE
      AND ct.allow_group_allocation = FALSE
      AND ct.allow_committee_allocation = FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030150000_RepairContributionDestinationDefaults') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS ux_fund_balances_organization_fiscal_year
    ON fund_balances (fiscal_year)
    WHERE group_id IS NULL AND section_id IS NULL AND committee_id IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261030150000_RepairContributionDestinationDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261030150000_RepairContributionDestinationDefaults', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261031100000_BackfillContributionDestinationPolicies') THEN
    UPDATE contribution_types AS ct
    SET allow_organization_wide_allocation = ct.allow_organization_wide_allocation OR EXISTS (
            SELECT 1
            FROM contribution_allocations AS ca
            JOIN contributions AS c ON c.id = ca.contribution_id
            WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
              AND ca.section_id IS NULL
              AND ca.group_id IS NULL
              AND ca.committee_id IS NULL),
        allow_section_allocation = ct.allow_section_allocation OR EXISTS (
            SELECT 1
            FROM contribution_allocations AS ca
            JOIN contributions AS c ON c.id = ca.contribution_id
            WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
              AND ca.section_id IS NOT NULL),
        allow_group_allocation = ct.allow_group_allocation OR EXISTS (
            SELECT 1
            FROM contribution_allocations AS ca
            JOIN contributions AS c ON c.id = ca.contribution_id
            WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
              AND ca.group_id IS NOT NULL),
        allow_committee_allocation = ct.allow_committee_allocation OR EXISTS (
            SELECT 1
            FROM contribution_allocations AS ca
            JOIN contributions AS c ON c.id = ca.contribution_id
            WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
              AND ca.committee_id IS NOT NULL);

    -- Tithe is explicitly an organization-wide basket in the approved
    -- requirements. Other new policies remain administrator-controlled.
    UPDATE contribution_types
    SET allow_organization_wide_allocation = TRUE,
        default_digital_destination_type = COALESCE(default_digital_destination_type, 'ORGANIZATION')
    WHERE upper(code) = 'TITHE';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261031100000_BackfillContributionDestinationPolicies') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261031100000_BackfillContributionDestinationPolicies', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261031110000_AlignDigitalGivingSourceAndTypes') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261031110000_AlignDigitalGivingSourceAndTypes') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261031110000_AlignDigitalGivingSourceAndTypes', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261031120000_SyncDigitalGivingAndOrganizationModelSnapshot') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261031120000_SyncDigitalGivingAndOrganizationModelSnapshot', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    DROP INDEX ix_payment_provider_configurations_provider_key;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE payment_webhook_events ADD digital_payment_transaction_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE payment_provider_configurations ADD configuration_version integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE payment_provider_configurations ADD is_validated boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE payment_provider_configurations ADD last_validated_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE fund_transactions ADD posting_key character varying(180);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE digital_payment_transactions ADD last_reconciliation_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE digital_payment_transactions ADD next_reconciliation_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE digital_payment_transactions ADD provider_configuration_version integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE digital_payment_transactions ADD provider_environment_snapshot character varying(30) NOT NULL DEFAULT 'SANDBOX';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE digital_payment_transactions ADD reconciliation_attempt_count integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE digital_payment_transactions ADD reconciliation_status character varying(30) NOT NULL DEFAULT 'NONE';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE TABLE payment_api_exchanges (
        id uuid NOT NULL,
        payment_provider_configuration_id uuid NOT NULL,
        digital_payment_transaction_id uuid,
        configuration_version integer NOT NULL,
        environment character varying(30) NOT NULL,
        operation character varying(50) NOT NULL,
        http_method character varying(10) NOT NULL,
        endpoint character varying(500) NOT NULL,
        transaction_reference character varying(80),
        sanitized_request_headers jsonb NOT NULL,
        sanitized_request_payload jsonb,
        http_status_code integer,
        sanitized_response_headers jsonb NOT NULL,
        sanitized_response_payload jsonb,
        duration_milliseconds bigint NOT NULL,
        attempt_number integer NOT NULL,
        error_category character varying(50),
        correlation_id character varying(100),
        requested_at_utc timestamp with time zone NOT NULL,
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_api_exchanges PRIMARY KEY (id),
        CONSTRAINT ck_payment_api_exchange_duration CHECK (duration_milliseconds >= 0 AND attempt_number > 0),
        CONSTRAINT fk_payment_api_exchanges_digital_payment_transactions_digital_ FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions (id) ON DELETE SET NULL,
        CONSTRAINT fk_payment_api_exchanges_payment_provider_configurations_payme FOREIGN KEY (payment_provider_configuration_id) REFERENCES payment_provider_configurations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE TABLE payment_provider_configuration_versions (
        id uuid NOT NULL,
        payment_provider_configuration_id uuid NOT NULL,
        configuration_version integer NOT NULL,
        protected_configuration_snapshot text NOT NULL,
        sanitized_configuration_snapshot jsonb NOT NULL,
        changed_fields_json jsonb NOT NULL,
        validation_status character varying(30) NOT NULL,
        validation_message character varying(1000),
        validated_at_utc timestamp with time zone,
        change_reason character varying(500),
        created_date timestamp with time zone NOT NULL,
        updated_date timestamp with time zone NOT NULL,
        last_processed_time_stamp timestamp with time zone,
        created_by_id uuid,
        updated_by_id uuid,
        version integer NOT NULL,
        CONSTRAINT pk_payment_provider_configuration_versions PRIMARY KEY (id),
        CONSTRAINT ck_provider_configuration_version_positive CHECK (configuration_version > 0),
        CONSTRAINT fk_payment_provider_configuration_versions_payment_provider_co FOREIGN KEY (payment_provider_configuration_id) REFERENCES payment_provider_configurations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    UPDATE digital_payment_transactions AS payment
    SET provider_configuration_version = provider.configuration_version,
        provider_environment_snapshot = provider.environment
    FROM payment_provider_configurations AS provider
    WHERE payment.payment_provider_configuration_id = provider.id;

    UPDATE digital_payment_transactions
    SET reconciliation_status = 'SCHEDULED',
        next_reconciliation_at_utc = NOW()
    WHERE provider_transaction_id IS NOT NULL
      AND status IN ('PENDING', 'PROCESSING', 'UNKNOWN');

    INSERT INTO payment_provider_configuration_versions
        (id, payment_provider_configuration_id, configuration_version,
         protected_configuration_snapshot, sanitized_configuration_snapshot,
         changed_fields_json, validation_status, validation_message,
         created_date, updated_date, created_by_id, updated_by_id, version)
    SELECT gen_random_uuid(), id, configuration_version,
           protected_configuration, '{}'::jsonb,
           '["MIGRATED_EXISTING_CONFIGURATION"]'::jsonb, 'NOT_TESTED',
           'Existing provider configuration captured during migration.',
           NOW(), NOW(), created_by_id, updated_by_id, 1
    FROM payment_provider_configurations;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_webhook_events_digital_payment_transaction_id ON payment_webhook_events (digital_payment_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE UNIQUE INDEX ix_payment_provider_configurations_provider_key_environment ON payment_provider_configurations (provider_key, environment);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE UNIQUE INDEX ix_fund_transactions_posting_key ON fund_transactions (posting_key) WHERE posting_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_digital_payment_transactions_reconciliation_status_next_rec ON digital_payment_transactions (reconciliation_status, next_reconciliation_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_api_exchanges_digital_payment_transaction_id_reques ON payment_api_exchanges (digital_payment_transaction_id, requested_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_api_exchanges_environment_requested_at_utc ON payment_api_exchanges (environment, requested_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_api_exchanges_http_status_code_requested_at_utc ON payment_api_exchanges (http_status_code, requested_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_api_exchanges_payment_provider_configuration_id_req ON payment_api_exchanges (payment_provider_configuration_id, requested_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_api_exchanges_transaction_reference ON payment_api_exchanges (transaction_reference);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE UNIQUE INDEX ix_payment_provider_configuration_versions_payment_provider_co ON payment_provider_configuration_versions (payment_provider_configuration_id, configuration_version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    CREATE INDEX ix_payment_provider_configuration_versions_payment_provider_co1 ON payment_provider_configuration_versions (payment_provider_configuration_id, created_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    ALTER TABLE payment_webhook_events ADD CONSTRAINT fk_payment_webhook_events_digital_payment_transactions_digital FOREIGN KEY (digital_payment_transaction_id) REFERENCES digital_payment_transactions (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261101100000_AddMtnProviderReliabilityAndDiagnostics') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261101100000_AddMtnProviderReliabilityAndDiagnostics', '8.0.10');
    END IF;
END $EF$;
COMMIT;

