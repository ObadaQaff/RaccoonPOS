using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.AuthService;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.Checks;
using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.InvoiceLines;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Application.Service.Notifications;
using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Integration;
using RaccoonWarehouse.Accounting;
using RaccoonWarehouse.Accounting.Services;
using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Delegates;
using RaccoonWarehouse.Employees;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.FinancialTransactions.Reports;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Invoices.Reports;
using RaccoonWarehouse.Modules.Reports;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Navigation.Modules;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.Products;
using RaccoonWarehouse.Products.Reports;
using RaccoonWarehouse.POS;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.Stocks.Reports;
using RaccoonWarehouse.SubCategories;
using RaccoonWarehouse.Settings;
using RaccoonWarehouse.Units;
using RaccoonWarehouse.Vouchers;
using RaccoonWarehouse.Warehouses;
using RaccoonWarehouse.Notifications;
using RaccoonWarehouse.Core.Modules;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;               
using System.Windows.Controls;
using System.Windows.Threading;

namespace RaccoonWarehouse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Arabic;
        private static readonly DependencyProperty IsWindowLocalizedProperty =
            DependencyProperty.RegisterAttached(
                "IsWindowLocalized",
                typeof(bool),
                typeof(App),
                new PropertyMetadata(false));
        private static readonly DependencyProperty IsWindowLocalizationScheduledProperty =
            DependencyProperty.RegisterAttached(
                "IsWindowLocalizationScheduled",
                typeof(bool),
                typeof(App),
                new PropertyMetadata(false));

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            QuestPDF.Settings.License = LicenseType.Community;
            RegisterLocalizationHooks();

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            var notificationService = ServiceProvider.GetRequiredService<INotificationService>();
            notificationService.NotificationRaised += NotificationService_NotificationRaised;
            await InitializeLocalizationAsync();
            WriteRuntimeInfo();

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

            ServiceProvider.GetRequiredService<AccountingOperationProcessor>().Start();

            // 🔐 Login
            var login = ServiceProvider.GetRequiredService<LoginWindow>();
            ApplyRuntimeTitle(login);
            var loginResult = login.ShowDialog();

            if (loginResult == true)
            {
                try
                {
                    await ServiceProvider.GetRequiredService<IPandaOrderSyncService>().StartAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        UiText.T(
                            $"تعذر بدء مزامنة طلبات Panda: {ex.Message}",
                            $"Panda order synchronization could not start: {ex.Message}"),
                        UiText.T("خطأ مزامنة Panda", "Panda Synchronization Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                var dashboard = ServiceProvider.GetRequiredService<Dashboard>();
                ApplyRuntimeTitle(dashboard);

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

        private void RegisterLocalizationHooks()
        {
            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(LocalizeWindowOnLoaded),
                handledEventsToo: true);
        }

        private void LocalizeWindowOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Window window || ReferenceEquals(window, e.OriginalSource) is false)
                return;

            UiText.ApplyWindow(window);
            window.SetValue(IsWindowLocalizedProperty, true);
            ScheduleDeferredLocalization(window);
        }

        private void ScheduleDeferredLocalization(Window window)
        {
            if (window.GetValue(IsWindowLocalizationScheduledProperty) is true)
                return;

            window.SetValue(IsWindowLocalizationScheduledProperty, true);

            _ = window.Dispatcher.BeginInvoke(async () =>
            {
                var delays = new[] { 0, 150, 500, 1200 };

                foreach (var delay in delays)
                {
                    if (delay > 0)
                        await Task.Delay(delay);

                    if (!window.IsLoaded)
                        break;

                    UiText.ApplyWindow(window);
                    UiText.ApplyTranslations(window);
                }

                window.SetValue(IsWindowLocalizationScheduledProperty, false);
            }, DispatcherPriority.Background);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogUnhandledException("DispatcherUnhandledException", e.Exception);
            MessageBox.Show("حدث خطأ غير متوقع وتم تسجيله. الرجاء إعادة المحاولة أو التواصل مع الدعم إذا تكرر الخطأ.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUnhandledException("AppDomainUnhandledException", e.ExceptionObject as Exception);
        }

        private void NotificationService_NotificationRaised(object? sender, RaccoonWarehouse.Domain.Notifications.AppNotificationDto notification)
        {
            Dispatcher.Invoke(() =>
            {
                var toast = ServiceProvider.GetRequiredService<NotificationToastWindow>();
                toast.ShowNotification(LocalizeNotification(notification));
            });
        }

        private static RaccoonWarehouse.Domain.Notifications.AppNotificationDto LocalizeNotification(
            RaccoonWarehouse.Domain.Notifications.AppNotificationDto notification)
        {
            if (!string.Equals(notification.Category, "OrderReceived", StringComparison.OrdinalIgnoreCase))
                return notification;

            return new RaccoonWarehouse.Domain.Notifications.AppNotificationDto
            {
                Title = UiText.T("استلام طلب جديد", "New order received"),
                Message = string.IsNullOrWhiteSpace(notification.Message)
                    ? UiText.T("تم استلام طلب جديد.", "A new order was received.")
                    : string.Format(
                        UiText.T("تم استلام الطلب رقم {0}.", "Order {0} was received."),
                        notification.Message),
                Category = notification.Category,
                Severity = notification.Severity,
                RecipientUserId = notification.RecipientUserId,
                RecipientRole = notification.RecipientRole,
                CreatedAt = notification.CreatedAt
            };
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogUnhandledException("TaskSchedulerUnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private static void LogUnhandledException(string source, Exception? exception)
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RaccoonWarehouse");
                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, "crash.log");
                var builder = new StringBuilder()
                    .AppendLine("========================================")
                    .AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .AppendLine($"Source: {source}")
                    .AppendLine(exception?.ToString() ?? "No exception details.");

                File.AppendAllText(logPath, builder.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Avoid secondary failures while reporting a crash.
            }
        }

        private static void WriteRuntimeInfo()
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RaccoonWarehouse");
                Directory.CreateDirectory(logDirectory);

                var executablePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
                var buildTime = File.Exists(executablePath)
                    ? File.GetLastWriteTime(executablePath).ToString("yyyy-MM-dd HH:mm:ss")
                    : "unknown";

                var content = new StringBuilder()
                    .AppendLine($"StartedAt={DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .AppendLine($"ExecutablePath={executablePath}")
                    .AppendLine($"AssemblyVersion={version}")
                    .AppendLine($"BuildTime={buildTime}")
                    .ToString();

                File.WriteAllText(Path.Combine(logDirectory, "runtime-info.log"), content, Encoding.UTF8);
            }
            catch
            {
                // Avoid startup failure while writing diagnostics.
            }
        }

        private static void ApplyRuntimeTitle(Window window)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var executablePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            var buildTime = File.Exists(executablePath)
                ? File.GetLastWriteTime(executablePath).ToString("yyyy-MM-dd HH:mm")
                : "unknown";

            window.Title = $"{window.Title} | build {version} | {buildTime}";
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
                await EnsureReportPermissionsTableAsync(db);
                await EnsureUnifiedPermissionsSchemaAsync(db);
                await EnsureAppSettingsTableAsync(db);
                await EnsureDelegateSchemaAsync(db);
                await EnsureEmployeeSchemaAsync(db);
                await EnsureCheckSchemaAsync(db);
                await EnsureAccountingOperationsSchemaAsync(db);
                await CurrencySeeder.SeedBaseCurrencyAsync(db);
                await scope.ServiceProvider.GetRequiredService<IWarehouseService>().EnsureDefaultWarehousesAsync();
                await scope.ServiceProvider.GetRequiredService<IPermissionService>().EnsureSeedDataAsync();
                await scope.ServiceProvider.GetRequiredService<ILanguageSettingsService>().EnsureDefaultsAsync();
                await scope.ServiceProvider.GetRequiredService<IDelegateFeatureService>().EnsureDefaultsAsync();
                await scope.ServiceProvider.GetRequiredService<IEmployeeFeatureService>().EnsureDefaultsAsync();
                await scope.ServiceProvider.GetRequiredService<IAccountingFeatureService>().EnsureDefaultsAsync();
                await scope.ServiceProvider.GetRequiredService<IAccountingService>().EnsureDefaultAccountsAsync();
                await FiscalYearSeeder.SeedLegacyAsync(db);
                await AccountTreeSeeder.SeedAsync(db);
                await scope.ServiceProvider.GetRequiredService<RecurringJournalService>().ExecuteDueAsync(DateTime.Today);

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

        private static async Task EnsureReportPermissionsTableAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.ReportPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportPermissions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ReportKey] NVARCHAR(150) NOT NULL,
        [Role] INT NOT NULL,
        [CanView] BIT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ReportPermissions_ReportKey_Role'
      AND object_id = OBJECT_ID(N'dbo.ReportPermissions')
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReportPermissions_ReportKey_Role]
        ON [dbo].[ReportPermissions] ([ReportKey], [Role]);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureAppSettingsTableAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.AppSettings', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppSettings]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(150) NOT NULL,
        [Value] NVARCHAR(MAX) NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AppSettings_Key'
      AND object_id = OBJECT_ID(N'dbo.AppSettings')
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppSettings_Key]
        ON [dbo].[AppSettings] ([Key]);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureUnifiedPermissionsSchemaAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.PermissionDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PermissionDefinitions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(200) NOT NULL,
        [Module] NVARCHAR(100) NOT NULL,
        [Resource] NVARCHAR(100) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [LegacyReportKey] NVARCHAR(150) NULL,
        [SortOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RolePermissions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Role] INT NOT NULL,
        [PermissionKey] NVARCHAR(200) NOT NULL,
        [IsAllowed] BIT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PermissionDefinitions_Key'
      AND object_id = OBJECT_ID(N'dbo.PermissionDefinitions')
)
BEGIN
    CREATE UNIQUE INDEX [IX_PermissionDefinitions_Key]
        ON [dbo].[PermissionDefinitions] ([Key]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_RolePermissions_Role_PermissionKey'
      AND object_id = OBJECT_ID(N'dbo.RolePermissions')
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_Role_PermissionKey]
        ON [dbo].[RolePermissions] ([Role], [PermissionKey]);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureDelegateSchemaAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.[Delegate]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Delegate]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [PhoneNumber] NVARCHAR(50) NULL,
        [AlternatePhoneNumber] NVARCHAR(50) NULL,
        [Status] INT NOT NULL DEFAULT(1),
        [DelegateType] INT NOT NULL DEFAULT(5),
        [RegionId] INT NULL,
        [AreaName] NVARCHAR(200) NULL,
        [HireDate] DATETIME2 NULL,
        [Notes] NVARCHAR(1000) NULL,
        [CreatedBy] INT NULL,
        [ModifiedBy] INT NULL,
        [IsDeleted] BIT NOT NULL DEFAULT(0),
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF COL_LENGTH('Delegate', 'AreaName') IS NULL
BEGIN
    ALTER TABLE [dbo].[Delegate] ADD [AreaName] NVARCHAR(200) NULL;
END;

IF COL_LENGTH('Invoice', 'DelegateId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Invoice] ADD [DelegateId] INT NULL;
END;

IF COL_LENGTH('User', 'CreditLimit') IS NULL
BEGIN
    ALTER TABLE [dbo].[User] ADD [CreditLimit] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_User_CreditLimit] DEFAULT (0);
END;

IF COL_LENGTH('User', 'CreditDays') IS NULL
BEGIN
    ALTER TABLE [dbo].[User] ADD [CreditDays] INT NOT NULL CONSTRAINT [DF_User_CreditDays] DEFAULT (0);
END;

IF COL_LENGTH('User', 'OpeningBalance') IS NULL
BEGIN
    ALTER TABLE [dbo].[User] ADD [OpeningBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_User_OpeningBalance] DEFAULT (0);
END;

IF COL_LENGTH('User', 'CurrentBalance') IS NULL
BEGIN
    ALTER TABLE [dbo].[User] ADD [CurrentBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_User_CurrentBalance] DEFAULT (0);
END;

IF COL_LENGTH('User', 'LastPaymentDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[User] ADD [LastPaymentDate] DATETIME2 NULL;
END;

IF COL_LENGTH('User', 'CreditStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[User] ADD [CreditStatus] INT NOT NULL CONSTRAINT [DF_User_CreditStatus] DEFAULT (1);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Delegate_Code'
      AND object_id = OBJECT_ID(N'dbo.[Delegate]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Delegate_Code] ON [dbo].[Delegate]([Code]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Delegate_UserId'
      AND object_id = OBJECT_ID(N'dbo.[Delegate]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Delegate_UserId]
        ON [dbo].[Delegate]([UserId])
        WHERE [UserId] IS NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Invoice_DelegateId'
      AND object_id = OBJECT_ID(N'dbo.[Invoice]')
)
BEGIN
    CREATE INDEX [IX_Invoice_DelegateId] ON [dbo].[Invoice]([DelegateId]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Delegate_User_UserId'
)
BEGIN
    ALTER TABLE [dbo].[Delegate]
        ADD CONSTRAINT [FK_Delegate_User_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Invoice_Delegate_DelegateId'
)
BEGIN
    ALTER TABLE [dbo].[Invoice]
        ADD CONSTRAINT [FK_Invoice_Delegate_DelegateId]
        FOREIGN KEY ([DelegateId]) REFERENCES [dbo].[Delegate]([Id]);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureEmployeeSchemaAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Employee]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [PhoneNumber] NVARCHAR(50) NULL,
        [AlternatePhoneNumber] NVARCHAR(50) NULL,
        [Email] NVARCHAR(200) NULL,
        [NationalId] NVARCHAR(100) NULL,
        [HireDate] DATETIME2 NULL,
        [TerminationDate] DATETIME2 NULL,
        [Status] INT NOT NULL DEFAULT(1),
        [Gender] INT NULL,
        [JobTitle] NVARCHAR(150) NULL,
        [DepartmentId] INT NULL,
        [BranchId] INT NULL,
        [ManagerId] INT NULL,
        [BasicSalary] DECIMAL(18,2) NULL,
        [Notes] NVARCHAR(1000) NULL,
        [Address] NVARCHAR(500) NULL,
        [DateOfBirth] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [ModifiedBy] INT NULL,
        [IsDeleted] BIT NOT NULL DEFAULT(0),
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_Code'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employee_Code] ON [dbo].[Employee]([Code]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_UserId'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employee_UserId]
        ON [dbo].[Employee]([UserId])
        WHERE [UserId] IS NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_BranchId'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE INDEX [IX_Employee_BranchId] ON [dbo].[Employee]([BranchId]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_DepartmentId'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE INDEX [IX_Employee_DepartmentId] ON [dbo].[Employee]([DepartmentId]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_Status'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE INDEX [IX_Employee_Status] ON [dbo].[Employee]([Status]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Employee_User_UserId'
)
BEGIN
    ALTER TABLE [dbo].[Employee]
        ADD CONSTRAINT [FK_Employee_User_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Employee_Employee_ManagerId'
)
BEGIN
    ALTER TABLE [dbo].[Employee]
        ADD CONSTRAINT [FK_Employee_Employee_ManagerId]
        FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employee]([Id]);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureAccountingOperationsSchemaAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.AccountingOperations', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AccountingOperations]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ReferenceType] NVARCHAR(50) NOT NULL,
        [ReferenceId] INT NOT NULL,
        [ReferenceNumber] NVARCHAR(100) NOT NULL,
        [OperationType] NVARCHAR(100) NOT NULL,
        [PayloadJson] NVARCHAR(MAX) NOT NULL,
        [Status] INT NOT NULL,
        [RetryCount] INT NOT NULL,
        [LastError] NVARCHAR(4000) NULL,
        [LastAttemptDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
        [NextAttemptDate] DATETIME2 NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AccountingOperations_Reference' AND object_id = OBJECT_ID(N'dbo.AccountingOperations'))
BEGIN
    CREATE UNIQUE INDEX [UX_AccountingOperations_Reference]
        ON [dbo].[AccountingOperations] ([ReferenceType], [ReferenceId], [OperationType]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AccountingOperations_Status_NextAttempt' AND object_id = OBJECT_ID(N'dbo.AccountingOperations'))
BEGIN
    CREATE INDEX [IX_AccountingOperations_Status_NextAttempt]
        ON [dbo].[AccountingOperations] ([Status], [NextAttemptDate]);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureCheckSchemaAsync(ApplicationDbContext db)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.[Check]', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.[Check]', N'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Check] ADD [Status] INT NOT NULL CONSTRAINT [DF_Check_Status] DEFAULT (1);
END;

IF OBJECT_ID(N'dbo.[Checks]', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.[Checks]', N'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checks] ADD [Status] INT NOT NULL CONSTRAINT [DF_Checks_Status] DEFAULT (1);
END;";

            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private async Task InitializeLocalizationAsync()
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await EnsureAppSettingsTableAsync(db);

                var languageService = scope.ServiceProvider.GetRequiredService<ILanguageSettingsService>();
                await languageService.EnsureDefaultsAsync();
                var language = await languageService.GetCurrentLanguageAsync();
                ApplyLanguage(language);
            }
            catch
            {
                ApplyLanguage(AppLanguage.Arabic);
            }
        }

        public void ApplyLanguage(AppLanguage language)
        {
            CurrentLanguage = language;

            var culture = language == AppLanguage.English
                ? new CultureInfo("en-US")
                : new CultureInfo("ar-JO");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            UiText.ResetCache();

            Resources["CurrentFlowDirection"] = language == AppLanguage.English
                ? FlowDirection.LeftToRight
                : FlowDirection.RightToLeft;

            Resources["CurrentTextAlignment"] = language == AppLanguage.English
                ? TextAlignment.Left
                : TextAlignment.Right;
        }

        public bool IsEnglish => CurrentLanguage == AppLanguage.English;

        public void Restart()
        {
            var executablePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            });

            Shutdown();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            var windowMap = new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                ["current-stock"] = typeof(CurrentStock),
                ["stock-movements"] = typeof(RaccoonWarehouse.Stocks.Reports.StockMovementsReport),
                ["sales-report"] = typeof(SalesReport),
                ["credit-sales"] = typeof(CreditSalesReport),
                ["inactive-products"] = typeof(InactiveProductsReport),
                ["discount-summary"] = typeof(DiscountSummaryReport),
                ["item-cost-detail"] = typeof(ItemCostDetailReport),
                ["price-list"] = typeof(PriceListReport),
                ["below-min-stock"] = typeof(LowStockReport),
                ["stock-balance-by-date"] = typeof(StockBalanceByDateReport),
                ["invoices-profit"] = typeof(InvoicesProfitBrowser),
                ["inventory-movement-summary"] = typeof(InventoryMovementSummary),
                ["stock-valuation"] = typeof(StockValuationReport),
                ["product-profit"] = typeof(ProductProfitReport),
                ["cash-flow"] = typeof(CashFlowReport),
                ["profit-loss"] = typeof(ProfitLossReport),
                ["stock-balances"] = typeof(StockBalancesReport),
                ["material-movements"] = typeof(MaterialMovementsReport),
                ["inactive-items"] = typeof(InactiveItemsReport)
            };

            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString()));
            services.AddTransient<IUOW, UOW>();

            // AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //session singelton 
            services.AddSingleton<IUserSession, UserSession>();
            services.AddSingleton<IReadOnlyDictionary<string, Type>>(windowMap);
            services.AddSingleton<IWindowNavigationService, WindowNavigationService>();
            services.AddSingleton<RaccoonWarehouse.Core.Localization.IUiTextLocalizer, UiTextLocalizer>();




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
            services.AddScoped<IInvoiceService>(sp => new InvoiceService(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IUOW>(),
                sp.GetRequiredService<IMapper>(),
                sp.GetRequiredService<IAccountingService>()));
            services.AddScoped<ISaleCheckoutService, SaleCheckoutService>();
            // Temporary Box API integration. Remove this registration with the isolated service when retired.
            services.AddSingleton<IBoxCartApiService, BoxCartApiService>();
            services.AddScoped<IEndpointOrderStatusService, EndpointOrderStatusService>();
            services.AddScoped<IBoxOrderImportService, BoxOrderImportService>();
            services.AddScoped<IPandaOrderProcessor, PandaOrderProcessor>();
            services.AddSingleton<IPandaOrderSyncService, PandaOrderSyncService>();
            services.AddScoped<IUnitService, UnitService>();
            services.AddScoped<IVoucherService>(sp => new VoucherService(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IUOW>(),
                sp.GetRequiredService<IMapper>(),
                sp.GetRequiredService<IAccountingService>()));
            services.AddScoped<IStockService>(sp => new StockService(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IUOW>(),
                sp.GetRequiredService<IMapper>(),
                sp.GetRequiredService<IAccountingService>()));
            services.AddScoped<IStockTransactionService, StockTransactionService>();
            services.AddScoped<IStockDocumentService>(sp => new StockDocumentService(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IUOW>(),
                sp.GetRequiredService<IMapper>(),
                sp.GetRequiredService<IAccountingService>()));
            services.AddScoped<IFalconStockImportService, FalconStockImportService>();
            services.AddScoped<IStockReportService, StockReportService>();
            services.AddScoped<ICheckService, CheckService>();
            services.AddScoped<IFinancialTransactionService, FinancialTransactionService>();
            services.AddScoped<IAccountingService, AccountingService>();
            services.AddScoped<IAccountingOperationService, AccountingOperationService>();
            services.AddSingleton<AccountingOperationProcessor>();
            services.AddScoped<IAccountingFeatureService, AccountingFeatureService>();
            services.AddScoped<IAccountTreeService, AccountTreeService>();
            services.AddScoped<CurrencyService>();
            services.AddScoped<TaxService>();
            services.AddScoped<AgingReportService>();
            services.AddScoped<UserStatementService>();
            services.AddScoped<BankReconciliationService>();
            services.AddScoped<RecurringJournalService>();
            services.AddScoped<ProfitAndLossService>();
            services.AddScoped<CashFlowService>();
            services.AddScoped<TrialBalanceService>();
            services.AddScoped<GeneralLedgerService>();
            services.AddScoped<RaccoonWarehouse.Application.Service.Dashboard.DashboardService>();
            services.AddScoped<SourceDocumentNavigationService>();
            services.AddTransient<AccountTreeViewModel>();
            services.AddTransient<AddAccountViewModel>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddScoped<FiscalYearService>();
            services.AddScoped<OpeningBalanceService>();
            services.AddScoped<CostCenterService>();
            services.AddSingleton<AccountService>();
            services.AddScoped<ILanguageSettingsService, LanguageSettingsService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IReportPermissionService, ReportPermissionService>();
            services.AddScoped<IDelegateService, DelegateService>();
            services.AddScoped<IDelegateFeatureService, DelegateFeatureService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IEmployeeFeatureService, EmployeeFeatureService>();
            services.AddSingleton<ILoadingService, LoadingService>();
            services.AddScoped<ICashierSessionService, CashierSessionService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddAppModule(new ReportsAppModule());
            services.AddSingleton<RaccoonWarehouse.Core.ChatAssistant.IChatAssistantSettingsService, RaccoonWarehouse.Application.Service.ChatAssistant.ChatAssistantSettingsService>();
            services.AddSingleton<RaccoonWarehouse.Core.ChatAssistant.IChatAssistantKnowledgeService, RaccoonWarehouse.Application.Service.ChatAssistant.ChatAssistantKnowledgeService>();
            services.AddSingleton<RaccoonWarehouse.Core.ChatAssistant.IChatAssistantService, RaccoonWarehouse.Application.Service.ChatAssistant.GeminiChatAssistantService>();
            services.AddTransient<IModuleDefinitionProvider, ProductsDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, CategoriesDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, SalesDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, WarehousesDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, BrandsDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, SettingsDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, UsersDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, CustomersDashboardModule>();
            services.AddTransient<IModuleDefinitionProvider, AccountingDashboardModule>();
            services.AddTransient<DashboardModuleRegistry>();
            services.AddTransient<IDashboardActionHandler, ProductsDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, CategoriesDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, SalesDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, WarehousesDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, BrandsDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, SettingsDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, UsersDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, CustomersDashboardActionHandler>();
            services.AddTransient<IDashboardActionHandler, AccountingDashboardActionHandler>();
            services.AddTransient<DashboardActionRegistry>();

            #endregion

            #region Views
            // Views (Windows)
            services.AddTransient<Dashboard>();
            services.AddTransient<AccountsTable>();
            services.AddTransient<AddAccountDialog>();
            services.AddTransient<CreateJournalEntry>();
            services.AddTransient<JournalEntriesBrowser>();
            services.AddTransient<AccountingOperationsBrowser>();
            services.AddTransient<TrialBalanceReport>();
            services.AddTransient<GeneralLedgerReport>();
            services.AddTransient<BalanceSheetReport>();
            services.AddTransient<UserStatementWindow>();
            services.AddTransient<PartyBalanceReportService>();
            services.AddTransient<PartyBalanceReport>();
            services.AddTransient<ChecksDashboard>();
            services.AddTransient<AccountingFeatureSettingsWindow>();


            services.AddTransient<UsersTable>();
            services.AddTransient<CustomersTable>();
            services.AddTransient<UpdateUser>();
            services.AddTransient<CreateUser>();
            services.AddTransient<DelegatesTable>();
            services.AddTransient<CreateDelegate>();
            services.AddTransient<UpdateDelegate>();
            services.AddTransient<DelegateDetails>();
            services.AddTransient<DelegateFeatureSettingsWindow>();
            services.AddTransient<EmployeesTable>();
            services.AddTransient<CreateEmployee>();
            services.AddTransient<UpdateEmployee>();
            services.AddTransient<EmployeeDetails>();
            services.AddTransient<EmployeeFeatureSettingsWindow>();

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
            services.AddTransient<StockValuationReport>();
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
            services.AddTransient<StockAdjustmentWindow>();
            services.AddTransient<ImportOrder>();
            services.AddTransient<OrdersTable>();
            services.AddTransient<OrderInvoiceDetails>();
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
            services.AddTransient<DailySalesReport>();
            services.AddTransient<Invoices.POS>();

            //Loading Window
            services.AddTransient<LoadingWindow>();
            services.AddTransient<NotificationToastWindow>();
            services.AddTransient<RaccoonWarehouse.ChatAssistant.ChatAssistantWindow>();
            services.AddTransient<RaccoonWarehouse.ChatAssistant.ChatAssistantSettingsWindow>();

            services.AddTransient<ReceiptWindow>();
            services.AddTransient<PaymentWindow>();
            //login window
            services.AddTransient<LoginWindow>();
            services.AddTransient<StartCashierSessionWindow>();
            services.AddTransient<CloseCashierSessionWindow>();
            services.AddTransient<ReportPermissionsManager>();
            services.AddTransient<LanguageSettingsWindow>();
            #endregion

        }




    }

}
