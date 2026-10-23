using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddPurchaseRequestApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "ApprovedUnitPrice",
            table: "PurchaseRequestLines",
            type: "decimal(18,6)",
            precision: 18,
            scale: 6,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "ApprovalNotes",
            table: "PurchaseRequestLines",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Decision",
            table: "PurchaseRequestLines",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ApprovedUnitPrice",
            table: "PurchaseRequestLines");

        migrationBuilder.DropColumn(
            name: "ApprovalNotes",
            table: "PurchaseRequestLines");

        migrationBuilder.DropColumn(
            name: "Decision",
            table: "PurchaseRequestLines");
    }
}
