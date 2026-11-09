using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddCustomerRequestPhone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "CustomerPhone", table: "CustomerRequests", type: "nvarchar(50)", maxLength: 50, nullable: true);
        migrationBuilder.Sql("UPDATE cr SET CustomerPhone = u.PhoneNumber FROM dbo.CustomerRequests cr INNER JOIN dbo.Users u ON u.Id = cr.CustomerId WHERE cr.CustomerPhone IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CustomerPhone", table: "CustomerRequests");
    }
}
