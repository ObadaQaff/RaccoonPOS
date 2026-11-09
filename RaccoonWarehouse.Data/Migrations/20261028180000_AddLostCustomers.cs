using Microsoft.EntityFrameworkCore.Migrations;

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddLostCustomers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LostCustomers",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Type = table.Column<int>(nullable: false),
                Reason = table.Column<int>(nullable: false),
                ProductId = table.Column<int>(nullable: true),
                ProductUnitId = table.Column<int>(nullable: true),
                ProductName = table.Column<string>(maxLength: 300, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                Message = table.Column<string>(maxLength: 2000, nullable: true),
                Notes = table.Column<string>(maxLength: 2000, nullable: true),
                CreatedBy = table.Column<int>(nullable: true),
                CreatedByName = table.Column<string>(maxLength: 200, nullable: false),
                CreatedDate = table.Column<DateTime>(nullable: false),
                UpdatedDate = table.Column<DateTime>(nullable: false)
            }, constraints: table => table.PrimaryKey("PK_LostCustomers", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_LostCustomers_CreatedDate", table: "LostCustomers", column: "CreatedDate");
        migrationBuilder.CreateIndex(name: "IX_LostCustomers_ProductId", table: "LostCustomers", column: "ProductId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "LostCustomers");
}
