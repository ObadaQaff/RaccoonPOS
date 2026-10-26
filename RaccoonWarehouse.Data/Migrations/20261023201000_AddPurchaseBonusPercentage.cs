using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddPurchaseBonusPercentage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "BonusPercentage",
            table: "InvoiceLine",
            type: "decimal(18,6)",
            precision: 18,
            scale: 6,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BonusPercentage",
            table: "InvoiceLine");
    }
}
