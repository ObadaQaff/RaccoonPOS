using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoiceLineDiscountAndFreeQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FreeQuantity",
                table: "InvoiceLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscountAmount",
                table: "InvoiceLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeQuantity",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "LineDiscountAmount",
                table: "InvoiceLine");

        }
    }
}
