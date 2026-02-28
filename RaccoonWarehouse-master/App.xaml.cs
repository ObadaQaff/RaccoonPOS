using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;
using RaccoonWarehouse.Application.Service.AuthService;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.Checks;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.InvoiceLines;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.FinancialTransactions.Reports;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Invoices.Reports;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.Products;
using RaccoonWarehouse.Products.Reports;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.Stocks.Reports;
using RaccoonWarehouse.SubCategories;
using RaccoonWarehouse.Units;
using RaccoonWarehouse.Vouchers;
using RaccoonWarehouse.Warehouses;
using System.IO;
using System.Windows;               

namespace RaccoonWarehouse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var loading = ServiceProvider.GetRequiredService<LoadingWindow>();

            // ✅ مهم جدًا
            MainWindow = loading;
            loading.Show();

            await Task.Delay(100); // allow render

            try
            {
                await InitializeApplicationAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Startup Error");
                Shutdown();
                return;
            }

            // 🔐 Login
            var login = ServiceProvider.GetRequiredService<LoginWindow>();
            var loginResult = login.ShowDialog();

            if (loginResult == true)
            {
                var dashboard = ServiceProvider.GetRequiredService<Dashboard>();

                // ✅ غيّر MainWindow قبل الإغلاق
                MainWindow = dashboard;

                loading.Close();   // الآن آمن
                dashboard.Show();
            }
            else
            {
                Shutdown();
            }
        }

        // ----------------------------------
        // 🔹 BACKGROUND WARMUP (DB, EF, DI)
        // ----------------------------------
        private async Task InitializeApplicationAsync()
        {
            await Task.Run(async () =>
            {
                using var scope = ServiceProvider.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.CanConnectAsync();

                // Force EF model & query compilation
                await db.Database.ExecuteSqlRawAsync("SELECT 1");

                // Warm AutoMapper
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                mapper.Map<object>(new object());
            });
        }

        // ----------------------------------
        // 🔹 UI WARMUP (WINDOW CREATION)
        // ----------------------------------
        private async Task PreloadWindowsAsync()
        {
            // ⚠️ MUST run on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                // Create windows ONCE (not shown)
                ServiceProvider.GetRequiredService<Dashboard>();
                ServiceProvider.GetRequiredService<UsersTable>();
            });
        }
        private void ConfigureServices(IServiceCollection services)
        {
            // Database
            services.AddDbContext<ApplicationDbContext>();
            services.AddTransient<IUOW, UOW>();

            // AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //session singelton 
            services.AddSingleton<IUserSession, UserSession>();




            // UOW
            #region Services
            // Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ISubCategoryService, SubCategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IProductUnitService, ProductUnitService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IInvoiceLineService, InvoiceLineService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IUnitService, UnitService>();
            services.AddScoped<IVoucherService, VoucherService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IStockDocumentService, StockDocumentService>();
            services.AddScoped<IStockReportService, StockReportService>();
            services.AddScoped<ICheckService, CheckService>();
            services.AddScoped<IFinancialTransactionService, FinancialTransactionService>();
            services.AddSingleton<ILoadingService, LoadingService>();
            services.AddScoped<ICashierSessionService, CashierSessionService>();
            services.AddScoped<IAuthService, AuthService>();

            #endregion

            #region Views
            // Views (Windows)
            services.AddTransient<Dashboard>();


            services.AddTransient<UsersTable>();
            services.AddTransient<UpdateUser>();
            services.AddTransient<CreateUser>();

            services.AddTransient<CategoriesTable>();
            services.AddTransient<CreateCategory>();
            services.AddTransient<UpdateCategory>();
            services.AddTransient<SubCategoryTable>();
            services.AddTransient<CreateSubCategory>();
            services.AddTransient<UpdateSubCategory>();
            services.AddTransient<CreateStock>();
            services.AddTransient<CreateProduct>();
            services.AddTransient<CreateBrand>();
            services.AddTransient<UpdateBrand>();
            services.AddTransient<BrandsTable>();

            services.AddTransient<ProductsTable>();
            services.AddTransient<UpdateProduct>();
            services.AddTransient<LowStockReport>();
            services.AddTransient<StockBalanceByDateReport>();
            services.AddTransient<InventoryMovementSummary>();
            services.AddTransient<ProductProfitReport>();
            services.AddTransient<InactiveProductsReport>();

            services.AddTransient<CreateWarehouse>();
            services.AddTransient<WarehousesTable>();

            services.AddTransient<UnitsTable>();
            services.AddTransient<CreateUnit>();
            services.AddTransient<UpdateUnit>();
            services.AddTransient<CreateSalesInvoice>();

            services.AddTransient<PayInvoice>();

            services.AddTransient<CashFlowReport>();
            services.AddTransient<ProfitLossReport>();


            services.AddTransient<SalesReturn>();
            services.AddTransient<StockOut>();
            services.AddTransient<StockIn>();
            services.AddTransient<CurrentStock>();
            services.AddTransient<ImportOrder>();
            services.AddTransient<MaterialMovementsReport>();
            services.AddTransient<RaccoonWarehouse.Stocks.Reports.StockMovementsReport>();
            services.AddTransient<SalesReport>();
            services.AddTransient<CreditSalesReport>();
            services.AddTransient<InactiveItemsReport>();

            services.AddTransient<DiscountSummaryReport>();
            services.AddTransient<ItemCostDetailReport>();
            services.AddTransient<PriceListReport>();
            services.AddTransient<BelowMinimumStockReport>();

            services.AddTransient<StockBalancesReport>();
            services.AddTransient<PaymentVoucher>();
            services.AddTransient<CreateVoucher>();

            services.AddTransient<SearchStockInWindow>();
            services.AddTransient<SearchVoucherWindow>();
            services.AddTransient<InvoicesProfitBrowser>();
            services.AddTransient<Invoices.POS>();

            //Loading Window
            services.AddTransient<LoadingWindow>();

            services.AddTransient<ReceiptWindow>();
            services.AddTransient<PaymentWindow>();
            //login window
            services.AddTransient<LoginWindow>();
            services.AddTransient<StartCashierSessionWindow>();
            services.AddTransient<CloseCashierSessionWindow>();
            #endregion

        }




    }

}
