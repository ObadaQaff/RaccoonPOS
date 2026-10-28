using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddCustomerRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "CustomerRequests", columns: table => new
        {
            Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            RequestNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            CustomerId = table.Column<int>(nullable: false), CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Status = table.Column<int>(nullable: false), ReceivingStatus = table.Column<int>(nullable: false), SalesStatus = table.Column<int>(nullable: false),
            TotalAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: false), PaidAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
            PaymentNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true), Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
            ApprovedBy = table.Column<int>(nullable: true), ApprovedDate = table.Column<DateTime>(nullable: true), CancelledBy = table.Column<int>(nullable: true), CancelledDate = table.Column<DateTime>(nullable: true), PurchaseRequestId = table.Column<int>(nullable: true),
            CreatedDate = table.Column<DateTime>(nullable: false), UpdatedDate = table.Column<DateTime>(nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_CustomerRequests", x => x.Id); });
        migrationBuilder.CreateIndex(name: "IX_CustomerRequests_RequestNumber", table: "CustomerRequests", column: "RequestNumber", unique: true);
        migrationBuilder.CreateTable(name: "CustomerRequestLines", columns: table => new
        {
            Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"), CustomerRequestId = table.Column<int>(nullable: false), ProductId = table.Column<int>(nullable: true), ProductUnitId = table.Column<int>(nullable: true),
            ItemName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false), UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true), Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false), UnitPrice = table.Column<decimal>(type: "decimal(18,6)", nullable: false), ReceivedQuantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false), SoldQuantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false), Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true), CreatedDate = table.Column<DateTime>(nullable: false), UpdatedDate = table.Column<DateTime>(nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_CustomerRequestLines", x => x.Id); table.ForeignKey("FK_CustomerRequestLines_CustomerRequests_CustomerRequestId", x => x.CustomerRequestId, "CustomerRequests", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex(name: "IX_CustomerRequestLines_CustomerRequestId", table: "CustomerRequestLines", column: "CustomerRequestId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable(name: "CustomerRequestLines"); migrationBuilder.DropTable(name: "CustomerRequests"); }
}
