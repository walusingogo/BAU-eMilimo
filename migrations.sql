CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

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

CREATE TABLE groups (
    id uuid NOT NULL,
    group_name text NOT NULL,
    description text,
    group_type text NOT NULL,
    is_active boolean NOT NULL,
    leader_id uuid,
    section_id uuid,
    parent_group_id uuid,
    created_date timestamp with time zone NOT NULL,
    updated_date timestamp with time zone NOT NULL,
    last_processed_time_stamp timestamp with time zone,
    created_by_id uuid,
    updated_by_id uuid,
    version integer NOT NULL,
    CONSTRAINT pk_groups PRIMARY KEY (id),
    CONSTRAINT fk_groups_groups_parent_group_id FOREIGN KEY (parent_group_id) REFERENCES groups (id) ON DELETE RESTRICT,
    CONSTRAINT fk_groups_sections_section_id FOREIGN KEY (section_id) REFERENCES sections (id) ON DELETE SET NULL
);

CREATE TABLE users (
    id uuid NOT NULL,
    title text NOT NULL,
    first_name text NOT NULL,
    middle_name text,
    last_name text NOT NULL,
    gender text NOT NULL,
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
    signature_data text NOT NULL,
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
    created_date timestamp with time zone NOT NULL,
    updated_date timestamp with time zone NOT NULL,
    last_processed_time_stamp timestamp with time zone,
    created_by_id uuid,
    updated_by_id uuid,
    version integer NOT NULL,
    CONSTRAINT pk_requisitions PRIMARY KEY (id),
    CONSTRAINT fk_requisitions_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE SET NULL,
    CONSTRAINT fk_requisitions_users_approved_by_id FOREIGN KEY (approved_by_id) REFERENCES users (id) ON DELETE SET NULL,
    CONSTRAINT fk_requisitions_users_archived_by_id FOREIGN KEY (archived_by_id) REFERENCES users (id) ON DELETE SET NULL,
    CONSTRAINT fk_requisitions_users_collected_by_id FOREIGN KEY (collected_by_id) REFERENCES users (id),
    CONSTRAINT fk_requisitions_users_money_ready_by_id FOREIGN KEY (money_ready_by_id) REFERENCES users (id),
    CONSTRAINT fk_requisitions_users_requisitor_id FOREIGN KEY (requisitor_id) REFERENCES users (id) ON DELETE SET NULL
);

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

CREATE UNIQUE INDEX ix_groups_group_name ON groups (group_name);

CREATE INDEX ix_groups_leader_id ON groups (leader_id);

CREATE INDEX ix_groups_parent_group_id ON groups (parent_group_id);

CREATE INDEX ix_groups_section_id ON groups (section_id);

CREATE INDEX ix_requisition_items_requisition_id ON requisition_items (requisition_id);

CREATE INDEX ix_requisitions_approved_by_id ON requisitions (approved_by_id);

CREATE INDEX ix_requisitions_archived_by_id ON requisitions (archived_by_id);

CREATE INDEX ix_requisitions_collected_by_id ON requisitions (collected_by_id);

CREATE INDEX ix_requisitions_group_id ON requisitions (group_id);

CREATE INDEX ix_requisitions_money_ready_by_id ON requisitions (money_ready_by_id);

CREATE UNIQUE INDEX ix_requisitions_requisition_number ON requisitions (requisition_number);

CREATE INDEX ix_requisitions_requisitor_id ON requisitions (requisitor_id);

CREATE INDEX ix_requisitions_status ON requisitions (status);

CREATE UNIQUE INDEX ix_sections_section_number ON sections (section_number);

CREATE UNIQUE INDEX ix_system_configs_config_key ON system_configs (config_key);

CREATE INDEX ix_users_created_by_id ON users (created_by_id);

CREATE UNIQUE INDEX ix_users_email ON users (email);

CREATE INDEX ix_users_group_id ON users (group_id);

CREATE UNIQUE INDEX ix_users_membership_id ON users (membership_id);

CREATE INDEX ix_users_section_id ON users (section_id);

CREATE INDEX ix_users_updated_by_id ON users (updated_by_id);

ALTER TABLE groups ADD CONSTRAINT fk_groups_users_leader_id FOREIGN KEY (leader_id) REFERENCES users (id) ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260706124059_InitialCreate', '8.0.4');

COMMIT;

