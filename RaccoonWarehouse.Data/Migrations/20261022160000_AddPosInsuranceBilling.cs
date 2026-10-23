using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace RaccoonWarehouse.Data.Migrations;
public partial class AddPosInsuranceBilling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "InsuranceContractId", "InsuranceContractVersionId", "InsuranceMemberPolicyId" })
            migrationBuilder.AlterColumn<int>(column, "InsuranceInvoiceCoverages", type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
        migrationBuilder.AlterColumn<int>("InsuranceMemberPolicyId", "InsuranceClaims", type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A manual data migration is required before making optional POS references mandatory again.
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM InsuranceInvoiceCoverages WHERE InsuranceContractId IS NULL OR InsuranceContractVersionId IS NULL OR InsuranceMemberPolicyId IS NULL) OR EXISTS (SELECT 1 FROM InsuranceClaims WHERE InsuranceMemberPolicyId IS NULL) THROW 51000, 'POS insurance records prevent rollback; migrate these records first.', 1;");
        foreach (var column in new[] { "InsuranceContractId", "InsuranceContractVersionId", "InsuranceMemberPolicyId" })
            migrationBuilder.AlterColumn<int>(column, "InsuranceInvoiceCoverages", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.AlterColumn<int>("InsuranceMemberPolicyId", "InsuranceClaims", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
    }
}
