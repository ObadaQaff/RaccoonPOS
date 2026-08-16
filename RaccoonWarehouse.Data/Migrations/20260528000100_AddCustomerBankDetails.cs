using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaccoonWarehouse.Data;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260528000100_AddCustomerBankDetails")]
    public partial class AddCustomerBankDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankIban",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankSwiftCode",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BankIban",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BankSwiftCode",
                table: "User");
        }
    }
}
