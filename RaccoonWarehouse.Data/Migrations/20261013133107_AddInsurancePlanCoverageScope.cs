using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInsurancePlanCoverageScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InsurancePlanId",
                table: "InsuranceExclusions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsurancePlanId",
                table: "InsuranceCoverageRules",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsurancePlanId",
                table: "InsuranceExclusions");

            migrationBuilder.DropColumn(
                name: "InsurancePlanId",
                table: "InsuranceCoverageRules");
        }
    }
}
