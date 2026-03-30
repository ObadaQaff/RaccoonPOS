using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddStockLots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockLot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityPerUnitSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingBaseQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLot_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockLot_ProductUnit_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_ProductId",
                table: "StockLot",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_ProductUnitId",
                table: "StockLot",
                column: "ProductUnitId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockLot");
        }
    }
}
