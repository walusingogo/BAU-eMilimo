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

