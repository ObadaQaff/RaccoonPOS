using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddStockAdjustmentAndBatchLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "StockLot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "StockLot",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedReason",
                table: "StockLot",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplacedByStockLotId",
                table: "StockLot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplacesStockLotId",
                table: "StockLot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "StockLot",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "StockTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockAdjustmentId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockLotId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockAdjustment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    NewStockLotId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityPerUnitSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseQuantityDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustment_ProductUnit_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnit",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustment_StockLot_NewStockLotId",
                        column: x => x.NewStockLotId,
                        principalTable: "StockLot",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustment_StockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "StockLot",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustment_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_ReplacesStockLotId",
                table: "StockLot",
                column: "ReplacesStockLotId",
                unique: true,
                filter: "[ReplacesStockLotId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockAdjustmentId",
                table: "StockTransaction",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockLotId",
                table: "StockTransaction",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_NewStockLotId",
                table: "StockAdjustment",
                column: "NewStockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_ProductId",
                table: "StockAdjustment",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_ProductUnitId",
                table: "StockAdjustment",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_StockLotId",
                table: "StockAdjustment",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_UserId",
                table: "StockAdjustment",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLot_StockLot_ReplacesStockLotId",
                table: "StockLot",
                column: "ReplacesStockLotId",
                principalTable: "StockLot",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransaction_StockAdjustment_StockAdjustmentId",
                table: "StockTransaction",
                column: "StockAdjustmentId",
                principalTable: "StockAdjustment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransaction_StockLot_StockLotId",
                table: "StockTransaction",
                column: "StockLotId",
                principalTable: "StockLot",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLot_StockLot_ReplacesStockLotId",
                table: "StockLot");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransaction_StockAdjustment_StockAdjustmentId",
                table: "StockTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransaction_StockLot_StockLotId",
                table: "StockTransaction");

            migrationBuilder.DropTable(
                name: "StockAdjustment");

            migrationBuilder.DropIndex(
                name: "IX_StockLot_ReplacesStockLotId",
                table: "StockLot");

            migrationBuilder.DropIndex(
                name: "IX_StockTransaction_StockAdjustmentId",
                table: "StockTransaction");

            migrationBuilder.DropIndex(
                name: "IX_StockTransaction_StockLotId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "StockLot");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "StockLot");

            migrationBuilder.DropColumn(
                name: "ClosedReason",
                table: "StockLot");

            migrationBuilder.DropColumn(
                name: "ReplacedByStockLotId",
                table: "StockLot");

            migrationBuilder.DropColumn(
                name: "ReplacesStockLotId",
                table: "StockLot");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StockLot");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "StockLotId",
                table: "StockTransaction");
        }
    }
}
