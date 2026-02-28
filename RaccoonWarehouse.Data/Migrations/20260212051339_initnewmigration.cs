using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class initnewmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashierSessionId",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnTaxedPrice",
                table: "ProductUnit",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Product",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalInvoiceId",
                table: "InvoiceLine",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashierSessionId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPOS",
                table: "Invoice",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalInvoiceId",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashierSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashierId = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatrBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierSession_User_CashierId",
                        column: x => x.CashierId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tax",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tax", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    CasherId = table.Column<int>(type: "int", nullable: true),
                    CashierSessionId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransaction_CashierSession_CashierSessionId",
                        column: x => x.CashierSessionId,
                        principalTable: "CashierSession",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    CasherId = table.Column<int>(type: "int", nullable: true),
                    CashierSessionId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransaction_CashierSession_CashierSessionId",
                        column: x => x.CashierSessionId,
                        principalTable: "CashierSession",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransaction_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransaction_ProductUnit_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransaction_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransaction_Stock_StockId",
                        column: x => x.StockId,
                        principalTable: "Stock",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransaction_User_CasherId",
                        column: x => x.CasherId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransaction_User_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransaction_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Voucher",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_CashierSessionId",
                table: "Voucher",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_CashierSessionId",
                table: "Invoice",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierSession_CashierId",
                table: "CashierSession",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransaction_CashierSessionId",
                table: "FinancialTransaction",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_CasherId",
                table: "StockTransaction",
                column: "CasherId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_CashierSessionId",
                table: "StockTransaction",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_CustomerId",
                table: "StockTransaction",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_InvoiceId",
                table: "StockTransaction",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_ProductId",
                table: "StockTransaction",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_ProductUnitId",
                table: "StockTransaction",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockId",
                table: "StockTransaction",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_VoucherId",
                table: "StockTransaction",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_CashierSession_CashierSessionId",
                table: "Invoice",
                column: "CashierSessionId",
                principalTable: "CashierSession",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Voucher_CashierSession_CashierSessionId",
                table: "Voucher",
                column: "CashierSessionId",
                principalTable: "CashierSession",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_CashierSession_CashierSessionId",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_Voucher_CashierSession_CashierSessionId",
                table: "Voucher");

            migrationBuilder.DropTable(
                name: "FinancialTransaction");

            migrationBuilder.DropTable(
                name: "StockTransaction");

            migrationBuilder.DropTable(
                name: "Tax");

            migrationBuilder.DropTable(
                name: "CashierSession");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_CashierSessionId",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_CashierSessionId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "CashierSessionId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "UnTaxedPrice",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "CashierSessionId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "IsPOS",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoice");
        }
    }
}
