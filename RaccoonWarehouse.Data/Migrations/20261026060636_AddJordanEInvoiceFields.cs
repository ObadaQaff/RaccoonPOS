using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJordanEInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JordanEInvoiceAttemptCount",
                table: "Invoice",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "JordanEInvoiceLastAttemptAtUtc",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JordanEInvoiceQr",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JordanEInvoiceReference",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JordanEInvoiceResponse",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JordanEInvoiceStatus",
                table: "Invoice",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "JordanEInvoiceSubmittedAtUtc",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierJoFotaraNumber",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierJoFotaraQr",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierJoFotaraUuid",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JordanEInvoiceAttemptCount",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "JordanEInvoiceLastAttemptAtUtc",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "JordanEInvoiceQr",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "JordanEInvoiceReference",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "JordanEInvoiceResponse",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "JordanEInvoiceStatus",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "JordanEInvoiceSubmittedAtUtc",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SupplierJoFotaraNumber",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SupplierJoFotaraQr",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SupplierJoFotaraUuid",
                table: "Invoice");

        }
    }
}
