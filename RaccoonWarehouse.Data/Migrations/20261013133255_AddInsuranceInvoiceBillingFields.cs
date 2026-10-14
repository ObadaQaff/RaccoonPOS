using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceInvoiceBillingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InsuranceCompanyId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceContractId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceContractVersionId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceMemberPolicyId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceResponsibility",
                table: "Invoice",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsInsuranceSale",
                table: "Invoice",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PatientResponsibility",
                table: "Invoice",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceCompanyId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "InsuranceContractId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "InsuranceContractVersionId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "InsuranceMemberPolicyId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "InsuranceResponsibility",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "IsInsuranceSale",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "PatientResponsibility",
                table: "Invoice");
        }
    }
}
