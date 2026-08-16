using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddTaxRatesAndJournalLineTaxFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    TaxAccountId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRates_Accounts_TaxAccountId",
                        column: x => x.TaxAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.AddColumn<int>(
                name: "TaxRateId",
                table: "JournalEntryLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "JournalEntryLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TaxRateId",
                table: "JournalEntryLines",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_Name_TaxType",
                table: "TaxRates",
                columns: new[] { "Name", "TaxType" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_TaxAccountId",
                table: "TaxRates",
                column: "TaxAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_TaxRates_TaxRateId",
                table: "JournalEntryLines",
                column: "TaxRateId",
                principalTable: "TaxRates",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_TaxRates_TaxRateId",
                table: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_TaxRateId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "JournalEntryLines");
        }
    }
}
