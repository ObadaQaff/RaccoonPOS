using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EligibilityStatus",
                table: "InsuranceMemberPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalVerificationReference",
                table: "InsuranceMemberPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsurancePlanId",
                table: "InsuranceMemberPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastVerificationMethod",
                table: "InsuranceMemberPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedDate",
                table: "InsuranceMemberPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdministratorId",
                table: "InsuranceContracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractualPayerId",
                table: "InsuranceContracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationType",
                table: "InsuranceCompanies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InsuranceEligibilityChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceMemberPolicyId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceEligibilityChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceInvoiceCoverageLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceInvoiceCoverageId = table.Column<int>(type: "int", nullable: false),
                    InvoiceLineId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ContractedUnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CoveredAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PatientAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExcludedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DeductibleAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CopayAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RequiresAuthorization = table.Column<bool>(type: "bit", nullable: false),
                    IsExcluded = table.Column<bool>(type: "bit", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsurancePreAuthorizationId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceInvoiceCoverageLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceContractId = table.Column<int>(type: "int", nullable: false),
                    InsuranceContractVersionId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePreAuthorizationLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsurancePreAuthorizationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePreAuthorizationLines", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceEligibilityChecks_InsuranceMemberPolicyId_CheckedAt",
                table: "InsuranceEligibilityChecks",
                columns: new[] { "InsuranceMemberPolicyId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceInvoiceCoverageLines_InsuranceInvoiceCoverageId_InvoiceLineId",
                table: "InsuranceInvoiceCoverageLines",
                columns: new[] { "InsuranceInvoiceCoverageId", "InvoiceLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_InsuranceContractId_Code",
                table: "InsurancePlans",
                columns: new[] { "InsuranceContractId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePreAuthorizationLines_InsurancePreAuthorizationId_ProductId",
                table: "InsurancePreAuthorizationLines",
                columns: new[] { "InsurancePreAuthorizationId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceEligibilityChecks");

            migrationBuilder.DropTable(
                name: "InsuranceInvoiceCoverageLines");

            migrationBuilder.DropTable(
                name: "InsurancePlans");

            migrationBuilder.DropTable(
                name: "InsurancePreAuthorizationLines");

            migrationBuilder.DropColumn(
                name: "EligibilityStatus",
                table: "InsuranceMemberPolicies");

            migrationBuilder.DropColumn(
                name: "ExternalVerificationReference",
                table: "InsuranceMemberPolicies");

            migrationBuilder.DropColumn(
                name: "InsurancePlanId",
                table: "InsuranceMemberPolicies");

            migrationBuilder.DropColumn(
                name: "LastVerificationMethod",
                table: "InsuranceMemberPolicies");

            migrationBuilder.DropColumn(
                name: "LastVerifiedDate",
                table: "InsuranceMemberPolicies");

            migrationBuilder.DropColumn(
                name: "AdministratorId",
                table: "InsuranceContracts");

            migrationBuilder.DropColumn(
                name: "ContractualPayerId",
                table: "InsuranceContracts");

            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "InsuranceCompanies");
        }
    }
}
