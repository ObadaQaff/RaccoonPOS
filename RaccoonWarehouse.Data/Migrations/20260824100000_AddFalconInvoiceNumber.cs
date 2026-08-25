using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddFalconInvoiceNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FalconInvoiceNumber",
                table: "Invoice",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FalconInvoiceNumber",
                table: "StockDocument",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FalconInvoiceNumber",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "FalconInvoiceNumber",
                table: "StockDocument");
        }
    }
}
