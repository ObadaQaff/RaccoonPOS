using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260804010000_RepairCreditPurchaseSupplierLinks")]
    public partial class RepairCreditPurchaseSupplierLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Invoice]
                SET [SupplierId] = [CustomerId],
                    [CustomerId] = NULL
                WHERE [InvoiceType] = 2
                  AND [PaymentType] = 2
                  AND [SupplierId] IS NULL
                  AND [CustomerId] IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair is intentionally not reversed because new valid purchase invoices
            // also use SupplierId and cannot be distinguished safely after migration.
        }
    }
}
