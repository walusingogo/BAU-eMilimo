using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndependentFinancialModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "lines_snapshot_json",
                table: "financial_record_book_service_collections",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "service_date_snapshot",
                table: "financial_record_book_service_collections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_snapshot",
                table: "financial_record_book_service_collections",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lines_snapshot_json",
                table: "financial_record_book_contributions",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lines_snapshot_json",
                table: "financial_record_book_service_collections");

            migrationBuilder.DropColumn(
                name: "service_date_snapshot",
                table: "financial_record_book_service_collections");

            migrationBuilder.DropColumn(
                name: "status_snapshot",
                table: "financial_record_book_service_collections");

            migrationBuilder.DropColumn(
                name: "lines_snapshot_json",
                table: "financial_record_book_contributions");
        }
    }
}
