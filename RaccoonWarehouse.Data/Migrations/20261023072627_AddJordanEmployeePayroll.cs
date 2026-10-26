using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace RaccoonWarehouse.Data.Migrations;
public partial class AddJordanEmployeePayroll : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM dbo.Employee WHERE DATALENGTH(NationalId)>200)
    THROW 51000, 'Employee NationalId values exceed 100 characters; resolve manually before migration.', 1;
IF EXISTS (SELECT NationalId FROM dbo.Employee WHERE NationalId IS NOT NULL AND NationalId<>N'' GROUP BY NationalId HAVING COUNT(*)>1)
    THROW 51001, 'Duplicate employee NationalId values must be resolved manually; no records were changed.', 1;
""");
            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Employee",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialSecurityNumberEncrypted",
                table: "Employee",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeEmploymentHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEmploymentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEmploymentHistory_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BasicMonthlySalary = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FixedAllowances = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StandardDailyHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WorkingDaysPerWeek = table.Column<int>(type: "int", nullable: false),
                    StandardMonthlyHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HourlyRateMethod = table.Column<int>(type: "int", nullable: false),
                    OvertimeMode = table.Column<int>(type: "int", nullable: false),
                    NormalOvertimeMultiplier = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HolidayOvertimeMultiplier = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SocialSecurityCovered = table.Column<bool>(type: "bit", nullable: false),
                    SocialSecurityEligibleWage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ContributionCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaryVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryVersion_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SalaryDayDivisor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmployeeSscPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmployerSscPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InsurableWageCeiling = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DefaultDailyHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DefaultWorkingDaysPerWeek = table.Column<int>(type: "int", nullable: false),
                    DefaultMonthlyHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LegalDailyHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LegalWeeklyHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    MinimumNormalMultiplier = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    MinimumHolidayMultiplier = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdvanceDeductionMaximumPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WeekStartsOn = table.Column<int>(type: "int", nullable: false),
                    ProrationMethod = table.Column<int>(type: "int", nullable: false),
                    IncludeFixedAllowancesInWageBasis = table.Column<bool>(type: "bit", nullable: false),
                    IsLegallyReviewed = table.Column<bool>(type: "bit", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePayroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    SalaryVersionId = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePayroll", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePayroll_EmployeeSalaryVersion_SalaryVersionId",
                        column: x => x.SalaryVersionId,
                        principalTable: "EmployeeSalaryVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePayroll_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePayroll_PayrollPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "PayrollPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdjustment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeePayrollId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DeductionBasis = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LegalReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AuthorizedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustment_EmployeePayroll_EmployeePayrollId",
                        column: x => x.EmployeePayrollId,
                        principalTable: "EmployeePayroll",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCorrection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeePayrollId = table.Column<int>(type: "int", nullable: false),
                    OriginalRevision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalInputsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalAdjustmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalApprovedBy = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrectedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCorrection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCorrection_EmployeePayroll_EmployeePayrollId",
                        column: x => x.EmployeePayrollId,
                        principalTable: "EmployeePayroll",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employee_NationalId",
                table: "Employee",
                column: "NationalId",
                unique: true,
                filter: "[NationalId] IS NOT NULL AND [NationalId] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEmploymentHistory_EmployeeId",
                table: "EmployeeEmploymentHistory",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayroll_EmployeeId_Year_Month",
                table: "EmployeePayroll",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayroll_PolicyId",
                table: "EmployeePayroll",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayroll_SalaryVersionId",
                table: "EmployeePayroll",
                column: "SalaryVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryVersion_EmployeeId_EffectiveFrom",
                table: "EmployeeSalaryVersion",
                columns: new[] { "EmployeeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustment_EmployeePayrollId",
                table: "PayrollAdjustment",
                column: "EmployeePayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCorrection_EmployeePayrollId",
                table: "PayrollCorrection",
                column: "EmployeePayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPolicy_Category_EffectiveFrom",
                table: "PayrollPolicy",
                columns: new[] { "Category", "EffectiveFrom" },
                unique: true);

        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM dbo.PayrollPolicy WHERE Category=N'JO-PRIVATE-GENERAL' AND EffectiveFrom='20260101')
INSERT INTO dbo.PayrollPolicy
(Name,Category,EffectiveFrom,EffectiveTo,SalaryDayDivisor,EmployeeSscPercent,EmployerSscPercent,InsurableWageCeiling,
 DefaultDailyHours,DefaultWorkingDaysPerWeek,DefaultMonthlyHours,LegalDailyHours,LegalWeeklyHours,MinimumNormalMultiplier,MinimumHolidayMultiplier,
 AdvanceDeductionMaximumPercent,WeekStartsOn,ProrationMethod,IncludeFixedAllowancesInWageBasis,IsLegallyReviewed,SourceReference,ReviewedBy,ReviewReason,CreatedDate,UpdatedDate)
VALUES
(N'الأردن — القطاع الخاص العام — ٢٠٢٦',N'JO-PRIVATE-GENERAL','20260101','20261231',30,7.5,14.25,3733,
 8,6,208,8,48,1.25,1.50,10,6,1,0,0,N'https://www.ssc.gov.jo/wp-content/uploads/2021/10/قانون-الضمان-الاجتماعي-رقم-١-لسنة-٢٠١٤-1.pdf ; https://www.mol.gov.jo/AR/List/القوانين ; https://org.petra.gov.jo/Include/InnerPage.jsp?ID=348404&lang=ar&name=news ; قاسم اليوم وطريقة الإجازة يلزمان مراجعة العقد والسياسة، ولا يعتمدان تلقائياً.',NULL,NULL,SYSDATETIME(),SYSDATETIME());
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("THROW 51002, 'Payroll rollback requires an explicit data-preserving manual migration.', 1;");
    }
}
