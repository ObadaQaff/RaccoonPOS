using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Notifications;
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
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Navigation.Modules;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Notifications;
using RaccoonWarehouse.Application.Service.Orders;
using System.Windows.Threading;





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
        private readonly INotificationService _notificationService;
        private readonly IBoxCartApiService _boxCartApiService;
        private readonly ILoadingService _loadingService;
        private readonly DashboardModuleRegistry _dashboardModules;
        private readonly DashboardActionRegistry _dashboardActions;
        private bool _isLoadingReports;
        private bool _isOpeningReport;
        private bool _isPollingPendingOrders;
        private bool _pendingSnapshotInitialized;
        private readonly HashSet<int> _knownPendingCartIds = new();
        private readonly HashSet<int> _unreadPendingCartIds = new();
        private readonly DispatcherTimer _pendingOrdersTimer = new()
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        private const bool BoxPendingOrdersPollingEnabled = false;
        private const string OrderReceivedNotificationCategory = "OrderReceived";

        public Dashboard()
            : this(
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IUserSession>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IReportPermissionService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IPermissionService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IDelegateFeatureService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IEmployeeFeatureService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IAccountingFeatureService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<INotificationService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IBoxCartApiService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<ILoadingService>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<DashboardModuleRegistry>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<DashboardActionRegistry>())
        {
        }

        public Dashboard(
            IUserSession userSession,
            IReportPermissionService reportPermissionService,
            IPermissionService permissionService,
            IDelegateFeatureService delegateFeatureService,
            IEmployeeFeatureService employeeFeatureService,
            IAccountingFeatureService accountingFeatureService,
            INotificationService notificationService,
            IBoxCartApiService boxCartApiService,
            ILoadingService loadingService,
            DashboardModuleRegistry dashboardModules,
            DashboardActionRegistry dashboardActions)
        {
            InitializeComponent();
            _userSession = userSession;
            _reportPermissionService = reportPermissionService;
            _permissionService = permissionService;
            _delegateFeatureService = delegateFeatureService;
            _employeeFeatureService = employeeFeatureService;
            _accountingFeatureService = accountingFeatureService;
            _notificationService = notificationService;
            _boxCartApiService = boxCartApiService;
            _loadingService = loadingService;
            _dashboardModules = dashboardModules;
            _dashboardActions = dashboardActions;
            _pendingOrdersTimer.Tick += PendingOrdersTimer_Tick;
            Closed += Dashboard_Closed;
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

                EmployeeNavButton.Visibility = await _employeeFeatureService.IsEnabledAsync()
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                _knownPendingCartIds.Clear();
                _unreadPendingCartIds.Clear();
                _pendingSnapshotInitialized = false;
                UpdateOrdersBadge();
                Receipt_Click(null, null);
            }
            finally
            {
                _loadingService.Hide();
            }

            if (BoxPendingOrdersPollingEnabled)
            {
                _pendingOrdersTimer.Start();
                await PollPendingOrdersAsync();
            }
        }

        private static string? GetActionKey(object? sender)
        {
            return sender is Button button
                ? button.Tag?.ToString() ?? button.Content?.ToString()
                : null;
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

        private Button CreateDashboardButton(ModuleActionDefinition option, RoutedEventHandler clickHandler, Border container)
        {
            var button = new Button
            {
                Content = option.DisplayLabel,
                Tag = option.Key,
                Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")
            };

            button.MouseEnter += (s, ev) => container.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
            button.MouseLeave += (s, ev) => container.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));
            button.Click += clickHandler;

            return button;
        }

        private void ShowDashboardGroups(IEnumerable<ModuleGroupDefinition> groups, RoutedEventHandler clickHandler, int maxColumns = 2)
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
                    Columns = Math.Max(1, Math.Min(maxColumns, group.Actions.Count)),
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                foreach (var option in group.Actions)
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

        private async Task ShowDashboardModuleAsync(string moduleKey, RoutedEventHandler clickHandler, int maxColumns = 2)
        {
            var moduleDefinition = await _dashboardModules.GetDefinitionAsync(moduleKey);
            ShowDashboardGroups(moduleDefinition.Groups, clickHandler, maxColumns);
        }

        private async Task RefreshAccountingNavigationAsync()
        {
            AccountingNavButton.Visibility = await _accountingFeatureService.IsEnabledAsync()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async Task RefreshEmployeeNavigationAsync()
        {
            EmployeeNavButton.Visibility = await _employeeFeatureService.IsEnabledAsync()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private DashboardActionContext CreateDashboardActionContext()
        {
            return new DashboardActionContext
            {
                OpenReportWindow = OpenReportWindow,
                RefreshAccountingNavigationAsync = RefreshAccountingNavigationAsync
            };
        }

        private async Task ExecuteDashboardActionAsync(string? actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
            {
                return;
            }

            try
            {
                await _dashboardActions.ExecuteAsync(actionKey, CreateDashboardActionContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تنفيذ الإجراء", "Failed to execute action")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private async void DashboardActionButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteDashboardActionAsync(GetActionKey(sender));
        }

        private void ShowDashboardEmptyState(string message)
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
                    Text = message,
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
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
        public async void StocksBtn_Click(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(ProductsDashboardModule.Key, DashboardActionButton_Click);
        }

        private void POSBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<RaccoonWarehouse.Invoices.POS>(WindowSizeType.FullScreen);    
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(CategoriesDashboardModule.Key, DashboardActionButton_Click);
        }


        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<SubCategoryTable>();
        }

        private async void Receipt_Click(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(SalesDashboardModule.Key, DashboardActionButton_Click);
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            _unreadPendingCartIds.Clear();
            UpdateOrdersBadge();
            WindowManager.Show<OrdersTable>(WindowSizeType.LargeRectangle);
        }

        private async void Reports_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingReports)
                return;

            try
            {
                _isLoadingReports = true;
                var moduleDefinition = await _dashboardModules.GetDefinitionAsync(ReportsDashboardModule.Key);
                if (moduleDefinition.Groups.Count == 0)
                {
                    ShowDashboardEmptyState(UiText.T("لا توجد تقارير متاحة لهذا المستخدم.", "There are no reports available for this user."));
                    return;
                }

                ShowDashboardGroups(moduleDefinition.Groups, DashboardActionButton_Click, maxColumns: 3);
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


        private async void Button_Click_6(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(WarehousesDashboardModule.Key, DashboardActionButton_Click);
        }
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(BrandsDashboardModule.Key, DashboardActionButton_Click);
        }

        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(SettingsDashboardModule.Key, DashboardActionButton_Click);
        }

        private async void Customers_Click(object sender, RoutedEventArgs e)
        {
            await ShowDashboardModuleAsync(CustomersDashboardModule.Key, DashboardActionButton_Click);
        }

        private async void Employees_Click(object sender, RoutedEventArgs e)
        {
            if (!await _employeeFeatureService.IsEnabledAsync())
            {
                MessageBox.Show(UiText.T("نظام الموظفين متوقف حالياً.", "The employees module is currently disabled."));
                return;
            }

            WindowManager.Show<EmployeesTable>();
        }

        private async void NotificationTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var delivered = await _notificationService.PublishAsync(new AppNotificationDto
                {
                    Title = UiText.T("تنبيه تجريبي", "Test notification"),
                    Message = UiText.T("هذا اختبار لعرض التنبيه داخل التطبيق.", "This is a test notification shown inside the app."),
                    Severity = NotificationSeverity.Warning,
                    RecipientRole = UserRole.Admin,
                    CreatedAt = DateTime.Now
                });

                if (!delivered)
                {
                    MessageBox.Show(UiText.T("لا يوجد مستخدم إداري نشط لعرض التنبيه.", "No active admin user is available to receive the notification."), UiText.T("تنبيه", "Alert"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر إرسال التنبيه التجريبي", "Failed to send the test notification")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private void ChatAssistant_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<RaccoonWarehouse.ChatAssistant.ChatAssistantWindow>(WindowSizeType.MediumRectangle);
        }

        private async void PendingOrdersTimer_Tick(object? sender, EventArgs e)
        {
            await PollPendingOrdersAsync();
        }

        private async Task PollPendingOrdersAsync()
        {
            if (_isPollingPendingOrders)
                return;

            _isPollingPendingOrders = true;
            try
            {
                var result = await _boxCartApiService.GetPendingOrdersAsync();
                if (!result.Success || result.Data == null)
                    return;

                var pendingCartIds = result.Data.Orders
                    .Select(order => order.CartId)
                    .ToHashSet();

                if (!_pendingSnapshotInitialized)
                {
                    _knownPendingCartIds.Clear();
                    foreach (var cartId in pendingCartIds)
                    {
                        _knownPendingCartIds.Add(cartId);
                        _unreadPendingCartIds.Add(cartId);
                    }

                    _pendingSnapshotInitialized = true;
                    if (_unreadPendingCartIds.Count > 0)
                    {
                        UpdateOrdersBadge();
                        await PublishPendingOrdersNotificationAsync(_unreadPendingCartIds.Count);
                    }
                    return;
                }

                foreach (var removedCartId in _knownPendingCartIds.Except(pendingCartIds).ToList())
                {
                    _knownPendingCartIds.Remove(removedCartId);
                    _unreadPendingCartIds.Remove(removedCartId);
                }

                var newCartIds = pendingCartIds
                    .Where(cartId => !_knownPendingCartIds.Contains(cartId))
                    .ToList();

                foreach (var cartId in newCartIds)
                {
                    _knownPendingCartIds.Add(cartId);
                    _unreadPendingCartIds.Add(cartId);
                }

                if (newCartIds.Count > 0)
                {
                    UpdateOrdersBadge();
                    await PublishPendingOrdersNotificationAsync(newCartIds.Count);
                }
                else
                {
                    UpdateOrdersBadge();
                }
            }
            finally
            {
                _isPollingPendingOrders = false;
            }
        }

        private async Task PublishPendingOrdersNotificationAsync(int count)
        {
            await _notificationService.PublishAsync(new AppNotificationDto
            {
                Title = UiText.T("طلب جديد", "New order"),
                Message = count == 1
                    ? UiText.T("تم استلام طلب جديد من Panda.", "A new Panda order was received.")
                    : string.Format(
                        UiText.T("تم استلام {0} طلبات جديدة من Panda.", "{0} new Panda orders were received."),
                        count),
                Category = OrderReceivedNotificationCategory,
                Severity = NotificationSeverity.Info,
                CreatedAt = DateTime.Now
            });
        }

        private void UpdateOrdersBadge()
        {
            if (OrdersBadgeBorder == null || OrdersBadgeText == null)
                return;

            var count = _unreadPendingCartIds.Count;
            OrdersBadgeText.Text = count > 99 ? "99+" : count.ToString();
            OrdersBadgeBorder.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Dashboard_Closed(object? sender, EventArgs e)
        {
            _pendingOrdersTimer.Stop();
            _pendingOrdersTimer.Tick -= PendingOrdersTimer_Tick;
            Closed -= Dashboard_Closed;
        }

        private async void Accounting_Click(object sender, RoutedEventArgs e)
        {
            if (!await _accountingFeatureService.IsEnabledAsync())
            {
                MessageBox.Show(UiText.T("نظام المحاسبة متوقف حالياً.", "Accounting is currently disabled."));
                return;
            }

            var moduleDefinition = await _dashboardModules.GetDefinitionAsync(AccountingDashboardModule.Key);
            ShowDashboardGroups(moduleDefinition.Groups, DashboardActionButton_Click);
        }
    }
}
