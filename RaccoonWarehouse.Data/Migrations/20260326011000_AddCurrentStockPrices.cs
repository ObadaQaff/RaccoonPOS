using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddCurrentStockPrices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "Stock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Stock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                @"
UPDATE s
SET
    PurchasePrice = ISNULL(pu.PurchasePrice, 0),
    SalePrice = ISNULL(pu.SalePrice, 0)
FROM Stock s
LEFT JOIN ProductUnit pu ON pu.Id = s.ProductUnitId;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Stock");
        }
    }
}
