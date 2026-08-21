using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockDocumentPaymentTypeAndChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "StockDocument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockDocumentId",
                table: "Check",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Check_StockDocumentId",
                table: "Check",
                column: "StockDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Check_StockDocument_StockDocumentId",
                table: "Check",
                column: "StockDocumentId",
                principalTable: "StockDocument",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Check_StockDocument_StockDocumentId",
                table: "Check");

            migrationBuilder.DropIndex(
                name: "IX_Check_StockDocumentId",
                table: "Check");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "StockDocumentId",
                table: "Check");
        }
    }
}
