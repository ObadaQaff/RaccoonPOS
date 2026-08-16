# Feature Map

Last verified: 2026-06-13

Use this map to identify the smallest source area to inspect. Status means
source is present, not that the feature has completed production validation.

| Feature | Status | UI | Application services | Domain/data | Tests/docs |
| --- | --- | --- | --- | --- | --- |
| POS | Active, known risks | `RaccoonWarehouse-master/Invoices/POS.*`, `RaccoonWarehouse-master/POS/` | Invoices, Stocks, FinancialTransactions, Cashers | Invoices, InvoiceLines, POS, Cashiers, Stock | `docs/features/POS.md`, POS QA reports |
| Stock | Active, known risks | `RaccoonWarehouse-master/Stocks/` | Stocks, StockDocuments, StockTransactions | Stock, StockLots, StockDocuments, StockItems, StockAdjustments | `docs/features/STOCK.md`, stock tests/QA |
| Accounting | Active; navigation feature-flagged | `RaccoonWarehouse-master/Accounting/` | Accounting, Settings, operational posting services | Accounting, FinancialTransactions, migrations | `docs/features/ACCOUNTING.md`, accounting tests |
| Orders / Box | Active temporary integration | `RaccoonWarehouse-master/Orders/`, dashboard badge | Orders | Orders DTOs, Invoices, Stock | `docs/features/ORDERS_BOX_INTEGRATION.md` |
| Permissions | Active, partial enforcement | Settings, dashboard modules | Permissions, AuthService, Settings | Permissions, Users | `docs/features/PERMISSIONS.md` |
| Products | Active | `Products/`, `Categories/`, `SubCategories/`, `Brands/`, `Units/` | matching service folders | matching domain folders | Product/category/brand QA reports |
| Sales invoices | Active | `RaccoonWarehouse-master/Invoices/` | Invoices, InvoiceLines | Invoices, InvoiceLines | invoice/accounting tests |
| Vouchers/checks | Active | `Vouchers/`, `Checks/` | Vouchers, Checks | Vouchers, Checks | voucher QA and accounting tests |
| Financial transactions | Active | `FinancialTransactions/` | FinancialTransactions | FinancialTransactions | accounting tests |
| Reports | Active | `Reports/`, feature report folders | report services | `Domain/Reports/` | QA summary |
| Employees | Feature-flagged; navigation incomplete | `Employees/`, some user screens | Employees, Settings | Employees | employee CRUD tests |
| Delegates | Feature-flagged | `Delegates/` | Delegates, Settings | Delegates, Invoice delegate link | delegate CRUD tests |
| Localization | Active | all WPF windows | LanguageSettingsService | AppSettings | literal translation JSON |
| Notifications | Active | notification toast/dashboard | Notifications | Notifications DTO | QA summary |
| Warehouses | Active; isolation needs review | `Warehouses/` | Warehouses | Warehouses and warehouse foreign keys | indirect stock tests |
| Falcon stock import | Temporary | Current Stock | Stocks/FalconStockImportService | Stock import DTOs | stock tests |

## Cross-Feature Entry Points

- Composition root and startup: `RaccoonWarehouse-master/App.xaml.cs`
- Dashboard: `RaccoonWarehouse-master/Home/`
- Navigation modules: `RaccoonWarehouse-master/Navigation/Modules/`
- Report module: `RaccoonWarehouse.Modules.Reports/`
- EF model: `RaccoonWarehouse.Data/ApplicationDbContext.cs`
- Migrations: `RaccoonWarehouse.Data/Migrations/`
- Shared localization: `RaccoonWarehouse-master/Helpers/Localization/`
- Current QA record: `RaccoonWarehouse.Tests/QA_Testing_Summary.md`

## Documentation Gaps

Dedicated feature documents should be added later for products, invoices,
vouchers/checks, reports, localization, employees/delegates, and notifications.

