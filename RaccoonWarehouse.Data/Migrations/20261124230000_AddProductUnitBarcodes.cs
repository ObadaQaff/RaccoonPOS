using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddProductUnitBarcodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductUnitBarcode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedBarcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnitBarcode", x => x.Id);
                    table.ForeignKey("FK_ProductUnitBarcode_ProductUnit_ProductUnitId", x => x.ProductUnitId, "ProductUnit", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_ProductUnitBarcode_NormalizedBarcode", "ProductUnitBarcode", "NormalizedBarcode", unique: true);
            migrationBuilder.CreateIndex("IX_ProductUnitBarcode_ProductUnitId_NormalizedBarcode", "ProductUnitBarcode", new[] { "ProductUnitId", "NormalizedBarcode" }, unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO ProductUnitBarcode (ProductUnitId, Barcode, NormalizedBarcode, CreatedDate, UpdatedDate)
                SELECT Id, LTRIM(RTRIM(AlternateBarcode)), LTRIM(RTRIM(AlternateBarcode)), CreatedDate, UpdatedDate
                FROM ProductUnit
                WHERE AlternateBarcode IS NOT NULL AND LTRIM(RTRIM(AlternateBarcode)) <> ''");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductUnitBarcode");
        }
    }
}
