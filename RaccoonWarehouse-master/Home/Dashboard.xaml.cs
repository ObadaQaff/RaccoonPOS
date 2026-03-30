using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Accounting;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Delegates;
using RaccoonWarehouse.Employees;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.Products;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.SubCategories;
using RaccoonWarehouse.Units;
using RaccoonWarehouse.Vouchers;
using RaccoonWarehouse.Warehouses;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;       // لـ Button, StackPanel, ContentControl
using System.Windows.Controls.Primitives;
using System.Windows.Input;          // لـ Cursors
using System.Windows.Media;          // لـ Brushes, Color, Solid    ColorBrush
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Invoices.Reports;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Stocks.Reports;
using RaccoonWarehouse.Products.Reports;
using RaccoonWarehouse.FinancialTransactions.Reports;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.Settings;
using RaccoonWarehouse.Common.Loading;





namespace RaccoonWarehouse
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        private readonly IUserSession _userSession;
        private readonly IReportPermissionService _reportPermissionService;
        private readonly IPermissionService _permissionService;
        private readonly IDelegateFeatureService _delegateFeatureService;
        private readonly IEmployeeFeatureService _employeeFeatureService;
        private readonly IAccountingFeatureService _accountingFeatureService;
        private readonly ILoadingService _loadingService;
        private bool _isLoadingReports;
        private bool _isOpeningReport;

        public Dashboard()
            : this(
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IUserSession>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IReportPermissionService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IPermissionService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IDelegateFeatureService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IEmployeeFeatureService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IAccountingFeatureService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<ILoadingService>())
        {
        }

        public Dashboard(
            IUserSession userSession,
            IReportPermissionService reportPermissionService,
            IPermissionService permissionService,
            IDelegateFeatureService delegateFeatureService,
            IEmployeeFeatureService employeeFeatureService,
            IAccountingFeatureService accountingFeatureService,
            ILoadingService loadingService)
        {
            InitializeComponent();
            _userSession = userSession;
            _reportPermissionService = reportPermissionService;
            _permissionService = permissionService;
            _delegateFeatureService = delegateFeatureService;
            _employeeFeatureService = employeeFeatureService;
            _accountingFeatureService = accountingFeatureService;
            _loadingService = loadingService;
            Loaded += Dashboard_Loaded;
        }

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loadingService.Show();
                UiText.ApplyWindow(this);
                await Task.Yield();

                AccountingNavButton.Visibility = await _accountingFeatureService.IsEnabledAsync()
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                Receipt_Click(null, null);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private static bool IsOption(string? option, string arabic, string english)
        {
            return string.Equals(option, arabic, StringComparison.Ordinal)
                || string.Equals(option, english, StringComparison.Ordinal);
        }

        private static string? GetActionKey(object? sender)
        {
            return sender is Button button
                ? button.Tag?.ToString() ?? button.Content?.ToString()
                : null;
        }

        private async Task<bool> HasPermissionAsync(string permissionKey)
        {
            var role = _userSession.CurrentUser?.Role;
            return role.HasValue && await _permissionService.HasPermissionAsync(role.Value, permissionKey);
        }

        private async void OpenReportWindow(Action openAction)
        {
            if (_isOpeningReport)
                return;

            try
            {
                _isOpeningReport = true;
                _loadingService.Show();
                await Task.Delay(100);
                openAction();
            }
            finally
            {
                _loadingService.Hide();
                _isOpeningReport = false;
            }
        }

        private sealed record DashboardOptionDefinition(string Label, string ActionKey);

        private sealed record DashboardGroupDefinition(string Title, IReadOnlyList<DashboardOptionDefinition> Options);

        private static DashboardOptionDefinition DashboardOption(string label, string actionKey)
        {
            return new DashboardOptionDefinition(label, actionKey);
        }

        private static DashboardGroupDefinition DashboardGroup(string title, params DashboardOptionDefinition[] options)
        {
            return new DashboardGroupDefinition(title, options);
        }

        private ScrollViewer CreateDashboardScrollViewer()
        {
            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };
        }

        private Border CreateDashboardCard()
        {
            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 270,
                    ShadowDepth = 2,
                    BlurRadius = 5,
                    Opacity = 0.2
                }
            };
        }

        private static TextBlock CreateDashboardHeader(string title)
        {
            return new TextBlock
            {
                Text = UiText.Translate(title.Trim()),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
        }

        private static Border CreateDashboardButtonContainer()
        {
            return new Border
            {
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(51, 150, 211))
            };
        }

        private Button CreateDashboardButton(DashboardOptionDefinition option, RoutedEventHandler clickHandler, Border container)
        {
            var button = new Button
            {
                Content = option.Label,
                Tag = option.ActionKey,
                Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")
            };

            button.MouseEnter += (s, ev) => container.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
            button.MouseLeave += (s, ev) => container.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));
            button.Click += clickHandler;

            return button;
        }

        private void ShowDashboardGroups(IEnumerable<DashboardGroupDefinition> groups, RoutedEventHandler clickHandler)
        {
            var scrollViewer = CreateDashboardScrollViewer();
            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            foreach (var group in groups)
            {
                var card = CreateDashboardCard();
                var cardPanel = new StackPanel { Orientation = Orientation.Vertical };
                cardPanel.Children.Add(CreateDashboardHeader(group.Title));

                var grid = new UniformGrid
                {
                    Columns = Math.Max(1, Math.Min(2, group.Options.Count)),
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                foreach (var option in group.Options)
                {
                    var container = CreateDashboardButtonContainer();
                    container.Child = CreateDashboardButton(option, clickHandler, container);
                    grid.Children.Add(container);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
            UiText.ApplyTranslations(scrollViewer);
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)System.Windows.Application.Current;

            if (_userSession.CurrentCashierSession != null)
            {
                var closeSessionWindow = app.ServiceProvider.GetRequiredService<CloseCashierSessionWindow>();
                var closeSessionResult = closeSessionWindow.ShowDialog();

                if (closeSessionResult != true)
                    return;
            }

            _userSession.EndSession();
            Hide();

            var login = app.ServiceProvider.GetRequiredService<LoginWindow>();
            var loginResult = login.ShowDialog();

            if (loginResult == true)
            {
                var dashboard = app.ServiceProvider.GetRequiredService<Dashboard>();
                System.Windows.Application.Current.MainWindow = dashboard;
                dashboard.Show();
                Close();
                return;
            }

            app.Shutdown();
        }

        private async void UsersTableBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<UsersTable>();
        }

        private void CategoriesTableBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<CategoriesTable>();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<StockIn>();

        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<CategoriesTable>();

        }
        public void StocksBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("بطاقات وأصناف", "Cards and Items"),
                        DashboardOption("بطاقةإدخال صنف", "Products.Create"),
                        DashboardOption("بحث عن صنف", "Products.List")),
                    DashboardGroup(
                        UiText.T("الأسعار والتحليل", "Pricing and Analysis"),
                        DashboardOption("قائمة الأسعار", "Products.PriceList"),
                        DashboardOption("أرباح الأصناف", "Products.ProfitReport"),
                        DashboardOption("اصناف لم تتحرك منذ مدة", "Products.InactiveReport")),
                    DashboardGroup(
                        UiText.T("الرقابة المخزنية", "Inventory Control"),
                        DashboardOption("الجرد والفرق", "Stocks.BalancesReport"),
                        DashboardOption("بضائع تحت الحد الأدنى", "Stocks.LowStockReport"),
                        DashboardOption("التسويات المخزنية", "Stocks.MaterialMovementsReport"))
                },
                DynamicButton_Click);
        }



        // one common handler for all buttons
        private void DynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Products.Create":
                        {
                            WindowManager.Show<CreateProduct>();
                            break;
                        }

                    case "Products.List":
                        {
                            WindowManager.Show<ProductsTable>();
                            break;
                        }
                    case "Products.PriceList":
                        {
                            OpenReportWindow(() => WindowManager.Show<PriceListReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Products.ProfitReport":
                        {
                            OpenReportWindow(() => WindowManager.Show<ProductProfitReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Products.InactiveReport":
                        {
                            OpenReportWindow(() => WindowManager.Show<InactiveProductsReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Stocks.BalancesReport":
                        {
                            OpenReportWindow(() => WindowManager.Show<StockBalancesReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Stocks.LowStockReport":
                        {
                            OpenReportWindow(() => WindowManager.Show<LowStockReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Stocks.MaterialMovementsReport":
                        {
                            OpenReportWindow(() => WindowManager.Show<MaterialMovementsReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                }
            }
        }

        private void POSBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<RaccoonWarehouse.Invoices.POS>(WindowSizeType.FullScreen);    
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("الفئات", "Categories"),
                        DashboardOption("إستعلام او نتعديل فئة", "Categories.List"),
                        DashboardOption("إضافة فئة", "Categories.Create")),
                    DashboardGroup(
                        UiText.T("الفئات الفرعية", "Subcategories"),
                        DashboardOption("إستعلام او تعديل فئة فرعية", "SubCategories.List"),
                        DashboardOption("إضافة فئة فرعية", "SubCategories.Create"))
                },
                CategoryDynamicButton_Click);
        }

        private void CategoryDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Categories.Create":
                        {
                            WindowManager.Show<CreateCategory>();
                            break;
                        }

                    case "Categories.List":
                        {
                            WindowManager.Show<CategoriesTable>();
                            break;
                        }

                    case "SubCategories.Create":
                        {
                            WindowManager.Show<CreateSubCategory>();
                            break;
                        }

                    case "SubCategories.List":
                        {
                            WindowManager.Show<SubCategoryTable>();
                            break;
                        }
                }
            }
        }


        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<SubCategoryTable>();
        }

        private void Receipt_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("الفواتير والمبيعات", "Invoices and Sales"),
                        DashboardOption("فاتورة مبيعات", "Invoices.Sales"),
                        DashboardOption("مردودات المبيعات", "Invoices.SalesReturn"),
                        DashboardOption("فاتورة مشتريات", "Invoices.Purchase")),
                    DashboardGroup(
                        UiText.T("التحصيل والدفع", "Receipts and Payments"),
                        DashboardOption("سند قبض", "Vouchers.Receipt"),
                        DashboardOption("سند دفع", "Vouchers.Payment")),
                    DashboardGroup(
                        UiText.T("المخزون", "Inventory"),
                        DashboardOption("سند ادخال بضاعة", "Stocks.In"),
                        DashboardOption("سند اخراج بضاعة", "Stocks.Out"),
                        DashboardOption("تسوية المخزون", "Stocks.Adjustment")),
                    DashboardGroup(
                        UiText.T("التحليلات", "Analysis"),
                        DashboardOption("تقرير المبيعات", "Reports.Sales"),
                        DashboardOption("تقرير مبيعات الآجل", "Reports.CreditSales"),
                        DashboardOption("تحليل ربحية الفواتير", "Reports.InvoiceProfit")),
                    DashboardGroup(
                        UiText.T("الطلبيات", "Orders"),
                        DashboardOption("طلبية استيراد", "Orders.Import"))
                },
                DynamicReceipts_Click);
        }

        private void DynamicReceipts_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Vouchers.Receipt":
                        {
                            WindowManager.Show<CreateVoucher>();
                            break;
                        }

                    case "Vouchers.Payment":
                        {
                            WindowManager.Show<PaymentVoucher>();
                            break;
                        }
                    case "Invoices.Sales":
                        {
                            WindowManager.Show<CreateSalesInvoice>();
                            break;
                        }
                    case "Invoices.Purchase":
                        {
                            WindowManager.Show<PayInvoice>();
                            break;
                        }
                    case "Invoices.SalesReturn":
                        {
                            WindowManager.Show<SalesReturn>();
                            break;
                        }
                    case "Stocks.In":
                        {
                            WindowManager.Show<StockIn>();
                            break;
                        }
                    case "Stocks.Out":
                        {
                            WindowManager.Show<StockOut>();
                            break;
                        }
                    case "Stocks.Adjustment":
                        {
                            WindowManager.Show<StockAdjustmentWindow>();
                            break;
                        }
                    case "Orders.Import":
                        {
                            WindowManager.Show<ImportOrder>();
                            break;
                        }
                    case "Reports.Sales":
                        {
                            OpenReportWindow(() => WindowManager.Show<SalesReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Reports.CreditSales":
                        {
                            OpenReportWindow(() => WindowManager.Show<CreditSalesReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    case "Reports.InvoiceProfit":
                        {
                            OpenReportWindow(() => WindowManager.Show<InvoicesProfitBrowser>(WindowSizeType.LargeRectangle));
                            break;
                        }
                }
            }
        }

        private async void Reports_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingReports)
                return;

            try
            {
                _isLoadingReports = true;
                var role = _userSession.CurrentUser?.Role;
                HashSet<string>? deniedReportKeys = null;

                if (role != null)
                    deniedReportKeys = await _reportPermissionService.GetDeniedReportKeysAsync(role.Value);

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Padding = new Thickness(20)
                };

                var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
                scrollViewer.Content = mainPanel;

                var groups = ReportCatalog.All
                    .Where(x => deniedReportKeys == null || !deniedReportKeys.Contains(x.Key))
                    .GroupBy(x => x.Category)
                    .ToDictionary(x => x.Key, x => x.ToArray());

                if (groups.Count == 0)
                {
                    MainContent.Content = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(12),
                        Margin = new Thickness(20),
                        Padding = new Thickness(30),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = UiText.T("لا توجد تقارير متاحة لهذا المستخدم.", "There are no reports available for this user."),
                            FontSize = 22,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    return;
                }

                foreach (var group in groups)
                {
                    var card = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(12),
                        Margin = new Thickness(0, 10, 0, 10),
                        Padding = new Thickness(15),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        BorderThickness = new Thickness(1),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Gray,
                            Direction = 270,
                            ShadowDepth = 2,
                            BlurRadius = 5,
                            Opacity = 0.2
                        }
                    };

                    var cardPanel = new StackPanel { Orientation = Orientation.Vertical };

                    var header = new TextBlock
                    {
                        Text = UiText.Translate(group.Key.Trim()),
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10),
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                    };
                    cardPanel.Children.Add(header);

                    var grid = new UniformGrid
                    {
                        Columns = 3,
                        Margin = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    foreach (var option in group.Value)
                    {
                        var border = new Border
                        {
                            CornerRadius = new CornerRadius(8),
                            Margin = new Thickness(5),
                            Background = new SolidColorBrush(Color.FromRgb(51, 150, 211)),
                        };

                        var btn = new Button
                        {
                            Content = option.DisplayName,
                            Tag = option.Key,
                            Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                        };

                        btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                        btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                        btn.Click += DynamicButtonReport_Click;

                        border.Child = btn;
                        grid.Children.Add(border);
                    }

                    cardPanel.Children.Add(grid);
                    card.Child = cardPanel;
                    mainPanel.Children.Add(card);
                }

                MainContent.Content = scrollViewer;
                UiText.ApplyTranslations(scrollViewer);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تحميل قائمة التقارير", "Failed to load report list")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoadingReports = false;
            }
        }


        private async void DynamicButtonReport_Click(object sender, RoutedEventArgs e)
        {
            if (_isOpeningReport)
                return;

            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                try
                {
                    _isOpeningReport = true;
                    var report = ReportCatalog.All.FirstOrDefault(x => string.Equals(x.Key, option, StringComparison.Ordinal));
                    var role = _userSession.CurrentUser?.Role;

                    if (report != null && role != null)
                    {
                        var canView = await _reportPermissionService.CanViewAsync(role.Value, report.Key);
                        if (!canView)
                        {
                            MessageBox.Show(UiText.T("ليس لديك صلاحية لعرض هذا التقرير.", "You do not have permission to view this report."));
                            return;
                        }
                    }

                    switch (option)
                    {
                        case "current-stock":
                        {
                            WindowManager.Show<CurrentStock>();
                            break;
                        }
                        case "stock-movements":
                        {
                            OpenReportWindow(() => WindowManager.Show<StockMovementsReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "sales-report":
                        {
                            OpenReportWindow(() => WindowManager.Show<SalesReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "credit-sales":
                        {
                            OpenReportWindow(() => WindowManager.Show<CreditSalesReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "inactive-products":
                        {
                            OpenReportWindow(() => WindowManager.Show<InactiveProductsReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "discount-summary":
                        {
                            OpenReportWindow(() => WindowManager.Show<DiscountSummaryReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "item-cost-detail":
                        {
                            OpenReportWindow(() => WindowManager.Show<ItemCostDetailReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "price-list":
                        {
                            OpenReportWindow(() => WindowManager.Show<PriceListReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "below-min-stock":
                        {
                            OpenReportWindow(() => WindowManager.Show<LowStockReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "stock-balance-by-date":
                        {
                            OpenReportWindow(() => WindowManager.Show<StockBalanceByDateReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "invoices-profit":
                        {
                            OpenReportWindow(() => WindowManager.Show<InvoicesProfitBrowser>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "inventory-movement-summary":
                        {
                            WindowManager.Show<InventoryMovementSummary>(WindowSizeType.LargeRectangle);
                            break;
                        }
                        case "stock-valuation":
                        {
                            OpenReportWindow(() => WindowManager.Show<StockValuationReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "product-profit":
                        {
                            OpenReportWindow(() => WindowManager.Show<ProductProfitReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "cash-flow":
                        {
                            OpenReportWindow(() => WindowManager.Show<CashFlowReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "profit-loss":
                        {
                            OpenReportWindow(() => WindowManager.Show<ProfitLossReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "stock-balances":
                        {
                            OpenReportWindow(() => WindowManager.Show<StockBalancesReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "material-movements":
                        {
                            OpenReportWindow(() => WindowManager.Show<MaterialMovementsReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                        case "inactive-items":
                        {
                            OpenReportWindow(() => WindowManager.Show<InactiveItemsReport>(WindowSizeType.LargeRectangle));
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{UiText.T("تعذر فتح التقرير", "Failed to open report")}: {ex.Message}", UiText.T("خطأ", "Error"));
                }
                finally
                {
                    _isOpeningReport = false;
                }
            }
        }


        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("إدارة المستودعات", "Warehouse Management"),
                        DashboardOption("إضافة مستودع جديد", "Warehouses.Create"),
                        DashboardOption("إستعلام او تعديل مستودع", "Warehouses.List"))
                },
                WarehouseDynamicButton_Click);
        }

        private void WarehouseDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Warehouses.Create":
                        {
                            WindowManager.Show<CreateWarehouse>();
                            break;
                        }

                    case "Warehouses.List":
                        {
                            WindowManager.Show<WarehousesTable>();
                            break;
                        }
                }
            }
        }


        private async void UsersDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Users.Create":
                        {
                            if (!await HasPermissionAsync("Users.Create"))
                            {
                                MessageBox.Show("ليس لديك صلاحية إنشاء مستخدم جديد.");
                                break;
                            }

                            WindowManager.Show<CreateUser>();
                            break;
                        }
                    case "Users.List":
                        {
                            if (!await HasPermissionAsync("Users.View"))
                            {
                                MessageBox.Show("ليس لديك صلاحية عرض المستخدمين.");
                                break;
                            }

                            WindowManager.Show<UsersTable>();
                            break;
                        }
                    case "Delegates.List":
                        {
                            if (!await _delegateFeatureService.IsEnabledAsync())
                            {
                                MessageBox.Show("نظام المندوبين غير مفعل حالياً.");
                                break;
                            }

                            WindowManager.Show<DelegatesTable>();
                            break;
                        }
                    case "Employees.List":
                        {
                            if (!await _employeeFeatureService.IsEnabledAsync())
                            {
                                MessageBox.Show("نظام الموظفين غير مفعل حالياً.");
                                break;
                            }

                            WindowManager.Show<EmployeesTable>();
                            break;
                        }
                }
            }
        
        }
       

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("إدارة العلامات التجارية", "Brand Management"),
                        DashboardOption("إضافة علامة تجارية جديدة", "Brands.Create"),
                        DashboardOption("إستعلام او تعديل العلامة التجارية", "Brands.List"))
                },
                BrandsDynamicButton_Click);
        }
        private void BrandsDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Brands.Create":
                        {
                            WindowManager.Show<CreateBrand>(WindowSizeType.MediumRectangle);
                            break;
                        }

                    case "Brands.List":
                        {
                            WindowManager.Show<BrandsTable>(WindowSizeType.MediumRectangle);
                            break;
                        }
                }
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var app = (App)System.Windows.Application.Current;
            var languageSettingsOption = app.IsEnglish ? "Language Settings" : "إعدادات اللغة";

            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("إعدادات الوحدات", "Unit Settings"),
                        DashboardOption("إضافة وحدة جديدة", "Units.Create"),
                        DashboardOption("إستعلام او تعديل وحدة", "Units.List")),
                    DashboardGroup(
                        UiText.T("إعدادات الوحدات الوظيفية", "Module Settings"),
                        DashboardOption("إعدادات نظام المندوبين", "Settings.Delegates"),
                        DashboardOption("إعدادات نظام الموظفين", "Settings.Employees"),
                        DashboardOption("إعدادات نظام المحاسبة", "Settings.Accounting"),
                        DashboardOption(languageSettingsOption, "Settings.Language")),
                    DashboardGroup(
                        UiText.T("الصلاحيات", "Permissions"),
                        DashboardOption("مدير صلاحيات النظام", "Settings.Permissions"))
                },
                SettingsDynamicButton_Click);
        }


        private async void SettingsDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (!string.IsNullOrWhiteSpace(option))
            {
                switch (option)
                {
                    case "Units.Create":
                        {
                            WindowManager.Show<CreateUnit>();
                            break;
                        }

                    case "Units.List":
                        {
                            WindowManager.Show<UnitsTable>();
                            break;
                        }
                    case "Settings.Permissions":
                        {
                            if (!await HasPermissionAsync("Permissions.ManageRoles"))
                            {
                                MessageBox.Show("ليس لديك صلاحية إدارة صلاحيات النظام.");
                                break;
                            }

                            WindowManager.Show<ReportPermissionsManager>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "Settings.Delegates":
                        {
                            if (!await HasPermissionAsync("Settings.ManageSettings"))
                            {
                                MessageBox.Show("ليس لديك صلاحية تعديل الإعدادات.");
                                break;
                            }

                            WindowManager.ShowDialog<DelegateFeatureSettingsWindow>(WindowSizeType.SmallSquare);
                            break;
                        }
                    case "Settings.Employees":
                        {
                            if (!await HasPermissionAsync("Settings.ManageSettings"))
                            {
                                MessageBox.Show("ليس لديك صلاحية تعديل الإعدادات.");
                                break;
                            }

                            WindowManager.ShowDialog<EmployeeFeatureSettingsWindow>(WindowSizeType.SmallSquare);
                            break;
                        }
                    case "Settings.Accounting":
                        {
                            if (!await HasPermissionAsync("Settings.ManageSettings"))
                            {
                                MessageBox.Show("ليس لديك صلاحية تعديل الإعدادات.");
                                break;
                            }

                            WindowManager.ShowDialog<AccountingFeatureSettingsWindow>(WindowSizeType.SmallSquare);
                            AccountingNavButton.Visibility = await _accountingFeatureService.IsEnabledAsync()
                                ? Visibility.Visible
                                : Visibility.Collapsed;

                            break;
                        }
                    case "Settings.Language":
                        {
                            if (!await HasPermissionAsync("Settings.ManageSettings"))
                            {
                                MessageBox.Show("ليس لديك صلاحية تعديل الإعدادات.");
                                break;
                            }

                            WindowManager.ShowDialog<LanguageSettingsWindow>(WindowSizeType.SmallSquare);
                            break;
                        }
                }
            }
        }

        private async void Customers_Click(object sender, RoutedEventArgs e)
        {
            var userOptions = new List<DashboardOptionDefinition>();
            if (await HasPermissionAsync("Users.Create"))
                userOptions.Add(DashboardOption("إضافة مستخدم جديد", "Users.Create"));
            if (await HasPermissionAsync("Users.View"))
                userOptions.Add(DashboardOption("إستعلام او تعديل مستخدم", "Users.List"));
            if (await _delegateFeatureService.IsEnabledAsync())
                userOptions.Add(DashboardOption("إدارة المندوبين", "Delegates.List"));
            if (await _employeeFeatureService.IsEnabledAsync())
                userOptions.Add(DashboardOption("إدارة الموظفين", "Employees.List"));

            ShowDashboardGroups(
                new[]
                {
                    new DashboardGroupDefinition(UiText.T("إدارة المستخدمين", "User Management"), userOptions)
                },
                UsersDynamicButton_Click);
        }

        private async void Accounting_Click(object sender, RoutedEventArgs e)
        {
            if (!await _accountingFeatureService.IsEnabledAsync())
            {
                MessageBox.Show(UiText.T("نظام المحاسبة متوقف حالياً.", "Accounting is currently disabled."));
                return;
            }

            ShowDashboardGroups(
                new[]
                {
                    DashboardGroup(
                        UiText.T("المحاسبة", "Accounting"),
                        DashboardOption("دليل الحسابات", "Accounting.Accounts"),
                        DashboardOption("قيد يومية يدوي", "Accounting.JournalEntry.Create"),
                        DashboardOption("سجل القيود", "Accounting.JournalEntries"),
                        DashboardOption("ميزان المراجعة", "Accounting.TrialBalance"),
                        DashboardOption("دفتر الأستاذ", "Accounting.GeneralLedger"),
                        DashboardOption("الميزانية العمومية", "Accounting.BalanceSheet"))
                },
                AccountingDynamicButton_Click);
        }

        private void AccountingDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var option = GetActionKey(sender);
            if (string.IsNullOrWhiteSpace(option))
            {
                return;
            }

            switch (option)
            {
                case "Accounting.Accounts":
                    WindowManager.Show<AccountsTable>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.JournalEntry.Create":
                    WindowManager.Show<CreateJournalEntry>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.JournalEntries":
                    WindowManager.Show<JournalEntriesBrowser>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.TrialBalance":
                    OpenReportWindow(() => WindowManager.Show<TrialBalanceReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Accounting.GeneralLedger":
                    OpenReportWindow(() => WindowManager.Show<GeneralLedgerReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Accounting.BalanceSheet":
                    OpenReportWindow(() => WindowManager.Show<BalanceSheetReport>(WindowSizeType.LargeRectangle));
                    break;
            }
        }
    }
}
