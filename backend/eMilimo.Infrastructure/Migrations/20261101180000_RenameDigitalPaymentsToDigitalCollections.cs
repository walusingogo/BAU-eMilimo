using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

/// <summary>
/// Renames the existing digital payment aggregate in place. No rows are copied,
/// recreated, or discarded by this migration.
/// </summary>
public partial class RenameDigitalPaymentsToDigitalCollections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable("digital_payment_transactions", newName: "digital_collections");
        migrationBuilder.RenameTable("digital_payment_lines", newName: "digital_collection_items");
        migrationBuilder.RenameTable("digital_payment_events", newName: "digital_collection_events");

        RenameColumns(migrationBuilder, toDigitalCollections: true);
        RenameIndexes(migrationBuilder, toDigitalCollections: true);

        migrationBuilder.Sql("""
            ALTER TABLE digital_collections RENAME CONSTRAINT pk_digital_payment_transactions TO pk_digital_collections;
            ALTER TABLE digital_collections RENAME CONSTRAINT ck_digital_payment_transaction_amounts TO ck_digital_collection_amounts;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_payment_transactions_contribution_sources_contribut TO fk_digital_collections_contribution_sources_contribution_sourc;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_payment_transactions_integration_configurations_pay TO fk_digital_collections_integration_configurations_payment_prov;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_payment_transactions_members_member_id TO fk_digital_collections_members_member_id;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_payment_transactions_payment_method_definitions_pay TO fk_digital_collections_payment_method_definitions_payment_meth;

            ALTER TABLE digital_collection_items RENAME CONSTRAINT pk_digital_payment_lines TO pk_digital_collection_items;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT ck_digital_payment_line_amount TO ck_digital_collection_item_amount;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT ck_digital_payment_line_destination_updated TO ck_digital_collection_item_destination;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_payment_lines_committees_committee_id TO fk_digital_collection_items_committees_committee_id;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_payment_lines_contribution_types_contribution_type_ TO fk_digital_collection_items_contribution_types_contribution_ty;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_payment_lines_contributions_contribution_id TO fk_digital_collection_items_contributions_contribution_id;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_payment_lines_digital_payment_transactions_digital_ TO fk_digital_collection_items_digital_collections_digital_collec;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_payment_lines_groups_group_id TO fk_digital_collection_items_groups_group_id;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_payment_lines_sections_section_id TO fk_digital_collection_items_sections_section_id;

            ALTER TABLE digital_collection_events RENAME CONSTRAINT pk_digital_payment_events TO pk_digital_collection_events;
            ALTER TABLE digital_collection_events RENAME CONSTRAINT fk_digital_payment_events_digital_payment_transactions_digital TO fk_digital_collection_events_digital_collections_digital_colle;

            ALTER TABLE financial_adjustments RENAME CONSTRAINT fk_financial_adjustments_digital_payment_transactions TO fk_financial_adjustments_digital_collections_digital_collectio;
            ALTER TABLE integration_exchanges RENAME CONSTRAINT fk_integration_exchanges_digital_payment_transactions_digital_ TO fk_integration_exchanges_digital_collections_digital_collectio;
            ALTER TABLE integration_webhook_events RENAME CONSTRAINT fk_integration_webhook_events_digital_payment_transactions_dig TO fk_integration_webhook_events_digital_collections_digital_coll;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE digital_collections RENAME CONSTRAINT pk_digital_collections TO pk_digital_payment_transactions;
            ALTER TABLE digital_collections RENAME CONSTRAINT ck_digital_collection_amounts TO ck_digital_payment_transaction_amounts;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_collections_contribution_sources_contribution_sourc TO fk_digital_payment_transactions_contribution_sources_contribut;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_collections_integration_configurations_payment_prov TO fk_digital_payment_transactions_integration_configurations_pay;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_collections_members_member_id TO fk_digital_payment_transactions_members_member_id;
            ALTER TABLE digital_collections RENAME CONSTRAINT fk_digital_collections_payment_method_definitions_payment_meth TO fk_digital_payment_transactions_payment_method_definitions_pay;

            ALTER TABLE digital_collection_items RENAME CONSTRAINT pk_digital_collection_items TO pk_digital_payment_lines;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT ck_digital_collection_item_amount TO ck_digital_payment_line_amount;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT ck_digital_collection_item_destination TO ck_digital_payment_line_destination_updated;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_collection_items_committees_committee_id TO fk_digital_payment_lines_committees_committee_id;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_collection_items_contribution_types_contribution_ty TO fk_digital_payment_lines_contribution_types_contribution_type_;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_collection_items_contributions_contribution_id TO fk_digital_payment_lines_contributions_contribution_id;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_collection_items_digital_collections_digital_collec TO fk_digital_payment_lines_digital_payment_transactions_digital_;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_collection_items_groups_group_id TO fk_digital_payment_lines_groups_group_id;
            ALTER TABLE digital_collection_items RENAME CONSTRAINT fk_digital_collection_items_sections_section_id TO fk_digital_payment_lines_sections_section_id;

            ALTER TABLE digital_collection_events RENAME CONSTRAINT pk_digital_collection_events TO pk_digital_payment_events;
            ALTER TABLE digital_collection_events RENAME CONSTRAINT fk_digital_collection_events_digital_collections_digital_colle TO fk_digital_payment_events_digital_payment_transactions_digital;

            ALTER TABLE financial_adjustments RENAME CONSTRAINT fk_financial_adjustments_digital_collections_digital_collectio TO fk_financial_adjustments_digital_payment_transactions;
            ALTER TABLE integration_exchanges RENAME CONSTRAINT fk_integration_exchanges_digital_collections_digital_collectio TO fk_integration_exchanges_digital_payment_transactions_digital_;
            ALTER TABLE integration_webhook_events RENAME CONSTRAINT fk_integration_webhook_events_digital_collections_digital_coll TO fk_integration_webhook_events_digital_payment_transactions_dig;
            """);

        RenameIndexes(migrationBuilder, toDigitalCollections: false);
        RenameColumns(migrationBuilder, toDigitalCollections: false);

        migrationBuilder.RenameTable("digital_collections", newName: "digital_payment_transactions");
        migrationBuilder.RenameTable("digital_collection_items", newName: "digital_payment_lines");
        migrationBuilder.RenameTable("digital_collection_events", newName: "digital_payment_events");
    }

    private static void RenameColumns(MigrationBuilder migrationBuilder, bool toDigitalCollections)
    {
        var oldName = toDigitalCollections ? "digital_payment_transaction_id" : "digital_collection_id";
        var newName = toDigitalCollections ? "digital_collection_id" : "digital_payment_transaction_id";

        migrationBuilder.RenameColumn(oldName, "digital_collection_items", newName);
        migrationBuilder.RenameColumn(oldName, "digital_collection_events", newName);
        migrationBuilder.RenameColumn(oldName, "financial_adjustments", newName);
        migrationBuilder.RenameColumn(oldName, "integration_exchanges", newName);
        migrationBuilder.RenameColumn(oldName, "integration_webhook_events", newName);
    }

    private static void RenameIndexes(MigrationBuilder migrationBuilder, bool toDigitalCollections)
    {
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_contribution_source_id", "ix_digital_collections_contribution_source_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_idempotency_key", "ix_digital_collections_idempotency_key", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_member_id", "ix_digital_collections_member_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_payment_method_definition_id", "ix_digital_collections_payment_method_definition_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_payment_provider_configuration", "ix_digital_collections_payment_provider_configuration_id_provi", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_payment_reference", "ix_digital_collections_payment_reference", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_public_status_token_hash", "ix_digital_collections_public_status_token_hash", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_reconciliation_status_next_rec", "ix_digital_collections_reconciliation_status_next_reconciliati", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_settlement_status_created_date", "ix_digital_collections_settlement_status_created_date", toDigitalCollections);
        Rename(migrationBuilder, "digital_collections", "ix_digital_payment_transactions_status_created_date", "ix_digital_collections_status_created_date", toDigitalCollections);

        Rename(migrationBuilder, "digital_collection_items", "ix_digital_payment_lines_committee_id", "ix_digital_collection_items_committee_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collection_items", "ix_digital_payment_lines_contribution_id", "ix_digital_collection_items_contribution_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collection_items", "ix_digital_payment_lines_contribution_type_id", "ix_digital_collection_items_contribution_type_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collection_items", "ix_digital_payment_lines_digital_payment_transaction_id", "ix_digital_collection_items_digital_collection_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collection_items", "ix_digital_payment_lines_group_id", "ix_digital_collection_items_group_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collection_items", "ix_digital_payment_lines_section_id", "ix_digital_collection_items_section_id", toDigitalCollections);
        Rename(migrationBuilder, "digital_collection_events", "ix_digital_payment_events_digital_payment_transaction_id_occur", "ix_digital_collection_events_digital_collection_id_occurred_at", toDigitalCollections);

        Rename(migrationBuilder, "financial_adjustments", "ix_financial_adjustments_digital_payment_transaction_id", "ix_financial_adjustments_digital_collection_id", toDigitalCollections);
        Rename(migrationBuilder, "financial_adjustments", "ix_financial_adjustments_digital_payment_transaction_id_status", "ix_financial_adjustments_digital_collection_id_status", toDigitalCollections);
        Rename(migrationBuilder, "integration_exchanges", "ix_integration_exchanges_digital_payment_transaction_id_reques", "ix_integration_exchanges_digital_collection_id_requested_at_utc", toDigitalCollections);
        Rename(migrationBuilder, "integration_webhook_events", "ix_integration_webhook_events_digital_payment_transaction_id", "ix_integration_webhook_events_digital_collection_id", toDigitalCollections);
    }

    private static void Rename(MigrationBuilder migrationBuilder, string table, string oldName, string newName, bool forward) =>
        migrationBuilder.RenameIndex(
            name: forward ? oldName : newName,
            newName: forward ? newName : oldName,
            table: table);
}
