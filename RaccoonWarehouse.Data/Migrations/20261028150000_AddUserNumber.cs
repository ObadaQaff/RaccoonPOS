using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddUserNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF OBJECT_ID(N'dbo.UserNumberSequence', N'SO') IS NULL CREATE SEQUENCE [dbo].[UserNumberSequence] AS int START WITH 1 INCREMENT BY 1;");
        migrationBuilder.AddColumn<int>(name: "UserNumber", table: "Users", type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.UserNumberSequence");
        migrationBuilder.CreateIndex(name: "IX_Users_UserNumber", table: "Users", column: "UserNumber", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Users_UserNumber", table: "Users");
        migrationBuilder.DropColumn(name: "UserNumber", table: "Users");
        migrationBuilder.Sql("IF OBJECT_ID(N'dbo.UserNumberSequence', N'SO') IS NOT NULL DROP SEQUENCE [dbo].[UserNumberSequence];");
    }
}
