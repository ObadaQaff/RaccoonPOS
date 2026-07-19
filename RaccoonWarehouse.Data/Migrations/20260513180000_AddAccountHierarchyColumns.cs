using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddAccountHierarchyColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountCategory",
                table: "Accounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountCode",
                table: "Accounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountLevel",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNature",
                table: "Accounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountTypeCode",
                table: "Accounts",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountCategory",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AccountCode",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AccountLevel",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AccountNature",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AccountTypeCode",
                table: "Accounts");
        }
    }
}
