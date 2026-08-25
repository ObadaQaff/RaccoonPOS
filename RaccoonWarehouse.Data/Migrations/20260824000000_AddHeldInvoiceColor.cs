using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddHeldInvoiceColor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeldColor",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeldColor",
                table: "Invoice");
        }
    }
}
