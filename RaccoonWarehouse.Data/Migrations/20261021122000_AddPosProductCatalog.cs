using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddPosProductCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosProductCatalogEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ItemCode = table.Column<long>(type: "bigint", nullable: true),
                    TaxExempt = table.Column<bool>(type: "bit", nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    AlternateBarcode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitSalePrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitPurchasePrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AllowFractionalSale = table.Column<bool>(type: "bit", nullable: false),
                    FractionalPartsPerUnit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FractionalPartName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultSaleUnit = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultPurchaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StockPurchasePrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StockSalePrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableToSale = table.Column<bool>(type: "bit", nullable: false),
                    StockStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosProductCatalogEntry", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosProductCatalogEntry_ProductId_ProductUnitId",
                table: "PosProductCatalogEntry",
                columns: new[] { "ProductId", "ProductUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosProductCatalogEntry_AlternateBarcode",
                table: "PosProductCatalogEntry",
                column: "AlternateBarcode");

            migrationBuilder.CreateIndex(
                name: "IX_PosProductCatalogEntry_ProductName",
                table: "PosProductCatalogEntry",
                column: "ProductName");

            migrationBuilder.CreateIndex(
                name: "IX_PosProductCatalogEntry_AvailableToSale",
                table: "PosProductCatalogEntry",
                column: "AvailableToSale");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosProductCatalogEntry");
        }
    }
}
