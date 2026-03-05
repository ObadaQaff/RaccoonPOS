using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddProductUnitDefaultsAndNormalizedQuantities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBaseUnit",
                table: "ProductUnit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultPurchaseUnit",
                table: "ProductUnit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultSaleUnit",
                table: "ProductUnit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerUnitSnapshot",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "StockItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerUnitSnapshot",
                table: "StockItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql(
                @"
WITH RankedUnits AS
(
    SELECT
        pu.Id,
        ROW_NUMBER() OVER
        (
            PARTITION BY pu.ProductId
            ORDER BY
                CASE WHEN pu.QuantityPerUnit = 1 THEN 0 ELSE 1 END,
                pu.QuantityPerUnit,
                pu.Id
        ) AS rn
    FROM ProductUnit pu
)
UPDATE pu
SET
    IsBaseUnit = CASE WHEN ru.rn = 1 THEN 1 ELSE 0 END,
    IsDefaultSaleUnit = CASE WHEN ru.rn = 1 THEN 1 ELSE 0 END,
    IsDefaultPurchaseUnit = CASE WHEN ru.rn = 1 THEN 1 ELSE 0 END
FROM ProductUnit pu
INNER JOIN RankedUnits ru ON ru.Id = pu.Id;

UPDATE il
SET
    QuantityPerUnitSnapshot = ISNULL(NULLIF(pu.QuantityPerUnit, 0), 1),
    BaseQuantity = il.Quantity * ISNULL(NULLIF(pu.QuantityPerUnit, 0), 1)
FROM InvoiceLine il
INNER JOIN ProductUnit pu ON pu.Id = il.ProductUnitId;

UPDATE si
SET
    QuantityPerUnitSnapshot = ISNULL(NULLIF(pu.QuantityPerUnit, 0), 1),
    BaseQuantity = si.Quantity * ISNULL(NULLIF(pu.QuantityPerUnit, 0), 1)
FROM StockItem si
INNER JOIN ProductUnit pu ON pu.Id = si.ProductUnitId;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBaseUnit",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "IsDefaultPurchaseUnit",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "IsDefaultSaleUnit",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "QuantityPerUnitSnapshot",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "QuantityPerUnitSnapshot",
                table: "StockItem");
        }
    }
}
