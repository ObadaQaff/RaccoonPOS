using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateSupplier2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "StockDocument",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_SupplierId",
                table: "StockDocument",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockDocument_User_SupplierId",
                table: "StockDocument",
                column: "SupplierId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockDocument_User_SupplierId",
                table: "StockDocument");

            migrationBuilder.DropIndex(
                name: "IX_StockDocument_SupplierId",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "StockDocument");
        }
    }
}
