using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotFrbContributionValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount_snapshot",
                table: "financial_record_book_contributions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "contribution_date_snapshot",
                table: "financial_record_book_contributions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contribution_number_snapshot",
                table: "financial_record_book_contributions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contribution_type_id_snapshot",
                table: "financial_record_book_contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contribution_type_name_snapshot",
                table: "financial_record_book_contributions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_snapshot",
                table: "financial_record_book_contributions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_snapshot",
                table: "financial_record_book_contributions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amount_snapshot",
                table: "financial_record_book_contributions");

            migrationBuilder.DropColumn(
                name: "contribution_date_snapshot",
                table: "financial_record_book_contributions");

            migrationBuilder.DropColumn(
                name: "contribution_number_snapshot",
                table: "financial_record_book_contributions");

            migrationBuilder.DropColumn(
                name: "contribution_type_id_snapshot",
                table: "financial_record_book_contributions");

            migrationBuilder.DropColumn(
                name: "contribution_type_name_snapshot",
                table: "financial_record_book_contributions");

            migrationBuilder.DropColumn(
                name: "description_snapshot",
                table: "financial_record_book_contributions");

            migrationBuilder.DropColumn(
                name: "status_snapshot",
                table: "financial_record_book_contributions");
        }
    }
}
