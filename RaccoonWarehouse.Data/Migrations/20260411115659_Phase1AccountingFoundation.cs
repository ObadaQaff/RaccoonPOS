using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase1AccountingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockItem_StockId",
                table: "StockItem");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Warehouse",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Warehouse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Warehouse",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Warehouse",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Voucher",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PostingStatus",
                table: "Voucher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Voucher",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoucherDate",
                table: "Voucher",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "StockTransaction",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "StockTransaction",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerUnitSnapshot",
                table: "StockTransaction",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "StockTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockAdjustmentId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockDocumentId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockLotId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "StockTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "StockItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LineNumber",
                table: "StockItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerUnitSnapshot",
                table: "StockItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockLotId",
                table: "StockItem",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                table: "StockDocument",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "StockDocument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "StockDocument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentDate",
                table: "StockDocument",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostingStatus",
                table: "StockDocument",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "StockDocument",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "StockDocument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "StockDocument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMovementDate",
                table: "Stock",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "Stock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Stock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Stock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBaseUnit",
                table: "ProductUnit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultPurchaseUnit",
                table: "ProductUnit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultSaleUnit",
                table: "ProductUnit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineSubTotal",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Profit",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitBeforeTax",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerUnitSnapshot",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TaxExempt",
                table: "InvoiceLine",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "InvoiceLine",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DelegateId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentDate",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossProfit",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetSales",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PostingStatus",
                table: "Invoice",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Invoice",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCOGS",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTax",
                table: "Invoice",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Invoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "FinancialTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "FinancialTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "FinancialTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "FinancialTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "FinancialTransaction",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PostingStatus",
                table: "FinancialTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "FinancialTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "FinancialTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "FinancialTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FinancialTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "FinancialTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "FinancialTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CashierSession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "CashierSession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DifferenceAmount",
                table: "CashierSession",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedClosingBalance",
                table: "CashierSession",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionNumber",
                table: "CashierSession",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "CashierSession",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    NormalBalanceType = table.Column<int>(type: "int", nullable: false),
                    IsPosting = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "bit", nullable: false),
                    AllowManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    ParentAccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentCostCenterId = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostCenters_CostCenters_ParentCostCenterId",
                        column: x => x.ParentCostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Delegate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternatePhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DelegateType = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: true),
                    AreaName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Delegate_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternatePhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employee",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employee_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyReportKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockLot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityPerUnitSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingBaseQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReplacesStockLotId = table.Column<int>(type: "int", nullable: true),
                    ReplacedByStockLotId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLot_ProductUnit_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLot_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLot_StockLot_ReplacesStockLotId",
                        column: x => x.ReplacesStockLotId,
                        principalTable: "StockLot",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalYearId = table.Column<int>(type: "int", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingPeriods_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountOpeningBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalYearId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    PartyUserId = table.Column<int>(type: "int", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountOpeningBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountOpeningBalances_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountOpeningBalances_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    NewStockLotId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityPerUnitSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseQuantityDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_ProductUnit_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_StockLot_NewStockLotId",
                        column: x => x.NewStockLotId,
                        principalTable: "StockLot",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustment_StockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "StockLot",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustment_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FiscalYearId = table.Column<int>(type: "int", nullable: true),
                    AccountingPeriodId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    CashierSessionId = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_AccountingPeriods_AccountingPeriodId",
                        column: x => x.AccountingPeriodId,
                        principalTable: "AccountingPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JournalEntries_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartyUserId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    CashierId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    StockDocumentId = table.Column<int>(type: "int", nullable: true),
                    FinancialTransactionId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_ReferenceNumber",
                table: "Voucher",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_SourceType_SourceId",
                table: "StockTransaction",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockAdjustmentId",
                table: "StockTransaction",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockDocumentId",
                table: "StockTransaction",
                column: "StockDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockLotId",
                table: "StockTransaction",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_StockId_LineNumber",
                table: "StockItem",
                columns: new[] { "StockId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_DocumentNumber",
                table: "StockDocument",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_WarehouseId_ProductId_ProductUnitId",
                table: "Stock",
                columns: new[] { "WarehouseId", "ProductId", "ProductUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_DelegateId",
                table: "Invoice",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_ReferenceNumber",
                table: "Invoice",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransaction_CasherId",
                table: "FinancialTransaction",
                column: "CasherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransaction_SourceType_SourceId",
                table: "FinancialTransaction",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_FiscalYearId_PeriodNumber",
                table: "AccountingPeriods",
                columns: new[] { "FiscalYearId", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountOpeningBalances_AccountId",
                table: "AccountOpeningBalances",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountOpeningBalances_FiscalYearId_AccountId_BranchId_CostCenterId_WarehouseId_PartyUserId",
                table: "AccountOpeningBalances",
                columns: new[] { "FiscalYearId", "AccountId", "BranchId", "CostCenterId", "WarehouseId", "PartyUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Code",
                table: "Accounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentAccountId",
                table: "Accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_Code",
                table: "CostCenters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_ParentCostCenterId",
                table: "CostCenters",
                column: "ParentCostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Delegate_Code",
                table: "Delegate",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Delegate_UserId",
                table: "Delegate",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_BranchId",
                table: "Employee",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Code",
                table: "Employee",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DepartmentId",
                table: "Employee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_ManagerId",
                table: "Employee",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Status",
                table: "Employee",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserId",
                table: "Employee",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_Code",
                table: "FiscalYears",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_AccountingPeriodId",
                table: "JournalEntries",
                column: "AccountingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FiscalYearId",
                table: "JournalEntries",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId_LineNumber",
                table: "JournalEntryLines",
                columns: new[] { "JournalEntryId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDefinitions_Key",
                table: "PermissionDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportPermissions_ReportKey_Role",
                table: "ReportPermissions",
                columns: new[] { "ReportKey", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_Role_PermissionKey",
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_NewStockLotId",
                table: "StockAdjustment",
                column: "NewStockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_ProductId",
                table: "StockAdjustment",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_ProductUnitId",
                table: "StockAdjustment",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_StockLotId",
                table: "StockAdjustment",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_UserId",
                table: "StockAdjustment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_ProductId",
                table: "StockLot",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_ProductUnitId",
                table: "StockLot",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_ReplacesStockLotId",
                table: "StockLot",
                column: "ReplacesStockLotId",
                unique: true,
                filter: "[ReplacesStockLotId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransaction_User_CasherId",
                table: "FinancialTransaction",
                column: "CasherId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Delegate_DelegateId",
                table: "Invoice",
                column: "DelegateId",
                principalTable: "Delegate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransaction_StockAdjustment_StockAdjustmentId",
                table: "StockTransaction",
                column: "StockAdjustmentId",
                principalTable: "StockAdjustment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransaction_StockLot_StockLotId",
                table: "StockTransaction",
                column: "StockLotId",
                principalTable: "StockLot",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransaction_User_CasherId",
                table: "FinancialTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Delegate_DelegateId",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransaction_StockAdjustment_StockAdjustmentId",
                table: "StockTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransaction_StockLot_StockLotId",
                table: "StockTransaction");

            migrationBuilder.DropTable(
                name: "AccountOpeningBalances");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Delegate");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "PermissionDefinitions");

            migrationBuilder.DropTable(
                name: "ReportPermissions");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "StockAdjustment");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "StockLot");

            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_ReferenceNumber",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_StockTransaction_SourceType_SourceId",
                table: "StockTransaction");

            migrationBuilder.DropIndex(
                name: "IX_StockTransaction_StockAdjustmentId",
                table: "StockTransaction");

            migrationBuilder.DropIndex(
                name: "IX_StockTransaction_StockDocumentId",
                table: "StockTransaction");

            migrationBuilder.DropIndex(
                name: "IX_StockTransaction_StockLotId",
                table: "StockTransaction");

            migrationBuilder.DropIndex(
                name: "IX_StockItem_StockId_LineNumber",
                table: "StockItem");

            migrationBuilder.DropIndex(
                name: "IX_StockDocument_DocumentNumber",
                table: "StockDocument");

            migrationBuilder.DropIndex(
                name: "IX_Stock_WarehouseId_ProductId_ProductUnitId",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_DelegateId",
                table: "Invoice");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_ReferenceNumber",
                table: "Invoice");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransaction_CasherId",
                table: "FinancialTransaction");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransaction_SourceType_SourceId",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "PostingStatus",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "VoucherDate",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "QuantityPerUnitSnapshot",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "StockDocumentId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "StockLotId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockTransaction");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "QuantityPerUnitSnapshot",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "StockLotId",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "PostingStatus",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockDocument");

            migrationBuilder.DropColumn(
                name: "LastMovementDate",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "IsBaseUnit",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "IsDefaultPurchaseUnit",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "IsDefaultSaleUnit",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "LineSubTotal",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "Profit",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "ProfitBeforeTax",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "QuantityPerUnitSnapshot",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "TaxExempt",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DelegateId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "GrossProfit",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "NetSales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "PostingStatus",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "TotalCOGS",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "TotalTax",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "PostingStatus",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashierSession");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CashierSession");

            migrationBuilder.DropColumn(
                name: "DifferenceAmount",
                table: "CashierSession");

            migrationBuilder.DropColumn(
                name: "ExpectedClosingBalance",
                table: "CashierSession");

            migrationBuilder.DropColumn(
                name: "SessionNumber",
                table: "CashierSession");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CashierSession");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                table: "StockDocument",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_StockId",
                table: "StockItem",
                column: "StockId");
        }
    }
}
