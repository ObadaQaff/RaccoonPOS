using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckAndUpdateInvoiceAndVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CasherId",
                table: "Voucher",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Check",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Check", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Check_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Check_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Voucher",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Check_InvoiceId",
                table: "Check",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Check_VoucherId",
                table: "Check",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Check");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Invoice");

            migrationBuilder.AlterColumn<int>(
                name: "CasherId",
                table: "Voucher",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
