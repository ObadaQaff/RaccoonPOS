using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class hello : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockItem_ProductUnitId",
                table: "StockItem",
                column: "ProductUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItem_ProductUnit_ProductUnitId",
                table: "StockItem",
                column: "ProductUnitId",
                principalTable: "ProductUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItem_ProductUnit_ProductUnitId",
                table: "StockItem");

            migrationBuilder.DropIndex(
                name: "IX_StockItem_ProductUnitId",
                table: "StockItem");
        }
    }
}
