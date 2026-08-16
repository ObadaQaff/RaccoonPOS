using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    public partial class AddOpeningBalanceBaseScopeUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_AccountOpeningBalances_FiscalYearId_AccountId_BaseScope'
      AND object_id = OBJECT_ID(N'dbo.AccountOpeningBalances')
)
BEGIN
    CREATE UNIQUE INDEX [UX_AccountOpeningBalances_FiscalYearId_AccountId_BaseScope]
        ON [dbo].[AccountOpeningBalances]([FiscalYearId], [AccountId])
        WHERE [BranchId] IS NULL
          AND [CostCenterId] IS NULL
          AND [WarehouseId] IS NULL
          AND [PartyUserId] IS NULL;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_AccountOpeningBalances_FiscalYearId_AccountId_BaseScope'
      AND object_id = OBJECT_ID(N'dbo.AccountOpeningBalances')
)
BEGIN
    DROP INDEX [UX_AccountOpeningBalances_FiscalYearId_AccountId_BaseScope]
        ON [dbo].[AccountOpeningBalances];
END");
        }
    }
}
