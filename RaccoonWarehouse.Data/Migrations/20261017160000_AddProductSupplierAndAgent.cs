using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <summary>
    /// Adds the optional product supplier link and the temporary agent text field.
    /// </summary>
    public partial class AddProductSupplierAndAgent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Product', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Product', N'SupplierId') IS NULL
        ALTER TABLE [dbo].[Product] ADD [SupplierId] int NULL;

    IF COL_LENGTH(N'dbo.Product', N'AgentName') IS NULL
        ALTER TABLE [dbo].[Product] ADD [AgentName] nvarchar(max) NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Product_User_SupplierId'
          AND parent_object_id = OBJECT_ID(N'dbo.Product')
    )
    AND OBJECT_ID(N'dbo.User', N'U') IS NOT NULL
        ALTER TABLE [dbo].[Product]
        ADD CONSTRAINT [FK_Product_User_SupplierId]
        FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[User]([Id]);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Product_SupplierId'
          AND object_id = OBJECT_ID(N'dbo.Product')
    )
        CREATE INDEX [IX_Product_SupplierId] ON [dbo].[Product]([SupplierId]);
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally non-destructive to preserve product metadata.
        }
    }
}
