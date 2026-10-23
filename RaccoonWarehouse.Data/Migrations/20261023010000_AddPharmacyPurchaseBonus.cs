using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace RaccoonWarehouse.Data.Migrations;
public partial class AddPharmacyPurchaseBonus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("BonusRatio", "InvoiceLine", type: "nvarchar(40)", maxLength: 40, nullable: true);
        foreach (var table in new[] { "InvoiceLine", "StockLot" })
        {
            migrationBuilder.AddColumn<decimal>("PurchaseAcquisitionCost", table, type: "decimal(18,6)", precision: 18, scale: 6, nullable: true);
            migrationBuilder.AddColumn<int>("SourcePurchaseLineId", table, type: "int", nullable: true);
            migrationBuilder.CreateIndex($"IX_{table}_SourcePurchaseLineId", table, "SourcePurchaseLineId");
        }
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM InvoiceLine WHERE BonusRatio IS NOT NULL OR PurchaseAcquisitionCost IS NOT NULL OR SourcePurchaseLineId IS NOT NULL) OR EXISTS (SELECT 1 FROM StockLot WHERE PurchaseAcquisitionCost IS NOT NULL OR SourcePurchaseLineId IS NOT NULL) THROW 51000, 'Bonus data prevents rollback; preserve it before removing these columns.', 1;");
        foreach (var table in new[] { "InvoiceLine", "StockLot" })
        {
            migrationBuilder.DropIndex($"IX_{table}_SourcePurchaseLineId", table);
            migrationBuilder.DropColumn("PurchaseAcquisitionCost", table);
            migrationBuilder.DropColumn("SourcePurchaseLineId", table);
        }
        migrationBuilder.DropColumn("BonusRatio", "InvoiceLine");
    }
}
