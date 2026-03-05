using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Categories;
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
using RaccoonWarehouse.Stocks.Reports;
using RaccoonWarehouse.Products.Reports;
using RaccoonWarehouse.FinancialTransactions.Reports;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.Settings;





namespace RaccoonWarehouse
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        private readonly IUserSession _userSession;
        private readonly IReportPermissionService _reportPermissionService;

        public Dashboard()
            : this(
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IUserSession>(),
                ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IReportPermissionService>())
        {
        }

        public Dashboard(IUserSession userSession, IReportPermissionService reportPermissionService)
        {
            InitializeComponent();
            _userSession = userSession;
            _reportPermissionService = reportPermissionService;
            Receipt_Click(null, null);
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
            WindowManager.Show<CreateStock>();

        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<CategoriesTable>();

        }
        public void StocksBtn_Click(object sender, RoutedEventArgs e)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)), // خلفية خفيفة
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار
            var groups = new Dictionary<string, string[]>
            {
                { "بطاقات وأصناف", new string[] { "بطاقةإدخال صنف", "بحث عن صنف" } },
                { "الأسعار والتحليل", new string[] { "قائمة الأسعار", "أرباح الأصناف", "اصناف لم تتحرك منذ مدة" } },
                { "الرقابة المخزنية", new string[] { "الجرد والفرق", "بضائع تحت الحد الأدنى", "التسويات المخزنية" } }
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أعمدة أو 3 حسب عدد الأزرار
                var grid = new UniformGrid
                {
                    Columns = 2,
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += DynamicButton_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }



        // one common handler for all buttons
        private void DynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();


                // Example: switch case for handling each button separately
                switch (option)
                {
                    case "بطاقةإدخال صنف":
                        {
                            WindowManager.Show<CreateProduct>();
                            break;
                        }

                    case "بحث عن صنف":
                        {
                            WindowManager.Show<ProductsTable>();
                            break;
                        }
                    case "قائمة الأسعار":
                        {
                            WindowManager.Show<PriceListReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "أرباح الأصناف":
                        {
                            WindowManager.Show<ProductProfitReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "اصناف لم تتحرك منذ مدة":
                        {
                            WindowManager.Show<InactiveProductsReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "الجرد والفرق":
                        {
                            WindowManager.Show<StockBalancesReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "بضائع تحت الحد الأدنى":
                        {
                            WindowManager.Show<LowStockReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "التسويات المخزنية":
                        {
                            WindowManager.Show<MaterialMovementsReport>(WindowSizeType.LargeRectangle);
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
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)), // خلفية خفيفة
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار
            var groups = new Dictionary<string, string[]>
            {
                { "الفئات", new string[] { "إستعلام او نتعديل فئة", "إضافة فئة" } },
                { "الفئات الفرعية", new string[] { "إستعلام او تعديل فئة فرعية", "إضافة فئة فرعية" } },
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أعمدة لأن الأزرار قليلة
                var grid = new UniformGrid
                {
                    Columns = 2,
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += CategoryDynamicButton_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }

        private void CategoryDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();

                // Example: switch case for handling each button separately
                switch (option)
                {
                    case "إضافة فئة":
                        {
                            WindowManager.Show<CreateCategory>();
                            break;
                        }

                    case "إستعلام او نتعديل فئة":
                        {
                            WindowManager.Show<CategoriesTable>();
                            break;
                        }

                    case "إضافة فئة فرعية":
                        {
                            WindowManager.Show<CreateSubCategory>();
                            break;
                        }

                    case "إستعلام او تعديل فئة فرعية":
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
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار حسب الموضوع
            var groups = new Dictionary<string, string[]>
            {
                { "الفواتير والمبيعات", new string[] { "فاتورة مبيعات", "مردودات المبيعات", "فاتورة مشتريات" } },
                { "التحصيل والدفع", new string[] { "سند قبض", "سند دفع" } },
                { "المخزون", new string[] { "سند ادخال بضاعة", "سند اخراج بضاعة" } },
                { "التحليلات", new string[] { "تقرير المبيعات", "تقرير مبيعات الآجل", "تحليل ربحية الفواتير" } },
                { "الطلبيات", new string[] { "طلبية استيراد" } }
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أو 3 أعمدة حسب عدد الأزرار
                int columns = group.Value.Length > 2 ? 2 : group.Value.Length;
                var grid = new UniformGrid
                {
                    Columns = columns,
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
                        Content = option,
                        Padding = new Thickness(14, 10, 14, 10),
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += DynamicReceipts_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }

        private void DynamicReceipts_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();

                switch (option)
                {
                    case "سند قبض":
                        {
                            WindowManager.Show<CreateVoucher>();
                            break;
                        }

                    case "سند دفع":
                        {
                            WindowManager.Show<PaymentVoucher>();
                            break;
                        }
                   /* case "إستعلام عن سند":
                        {
                          *//*  CreateReceipt productsTable = ((App)Application.Current)
                                  .ServiceProvider
                                  .GetRequiredService<CreateReceipt>();
                            productsTable.ShowDialog();
                            break;*//*
                        }*/
                    case "فاتورة مبيعات":
                        {
                            WindowManager.Show<CreateSalesInvoice>();
                            break;
                        }
                    case "فاتورة مشتريات":
                        {
                            WindowManager.Show<PayInvoice>();
                            break;
                        }
                    case "مردودات المبيعات":
                        {
                            WindowManager.Show<SalesReturn>();
                            break;
                        }
                    case "سند ادخال بضاعة":
                        {
                            WindowManager.Show<StockIn>();
                            break;
                        }
                    case "سند اخراج بضاعة":
                        {
                            WindowManager.Show<StockOut>();
                            break;
                        }
                    case "طلبية استيراد":
                        {
                            WindowManager.Show<ImportOrder>();
                            break;
                        }
                    case "تقرير المبيعات":
                        {
                            WindowManager.Show<SalesReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تقرير مبيعات الآجل":
                        {
                            WindowManager.Show<CreditSalesReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تحليل ربحية الفواتير":
                        {
                            WindowManager.Show<InvoicesProfitBrowser>(WindowSizeType.LargeRectangle);
                            break;
                        }
                }
            }
        }

        private async void Reports_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            HashSet<string>? deniedReportKeys = null;

            if (role != null)
                deniedReportKeys = await _reportPermissionService.GetDeniedReportKeysAsync(role.Value);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)), // خلفية خفيفة
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات التقارير
            var groups = ReportCatalog.All
                .Where(x => deniedReportKeys == null || !deniedReportKeys.Contains(x.Key))
                .GroupBy(x => x.Category)
                .ToDictionary(x => x.Key, x => x.Select(item => item.DisplayName).ToArray());

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
                        Text = "لا توجد تقارير متاحة لهذا المستخدم.",
                        FontSize = 22,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                return;
            }
            //"أرباح المبيعات بالتكلفة الحالية","تقرير مبيعات الذمم","حركة الاصناف المحولة", "أكثر الاصناف", "باقي الاصناف من صفقة معينة","باقي الاصناف من صفقة معينة","كشف تكلفة صنف تفصيلي", "اجمالي الخصم اصناف",
            //"إجمالي البونص", "قائمة الأسعار","كشف سعر تكلفة البضائع", "تقييم بضاعة أول المدة", "كشف الاماكن", "حركة المخزون تفصيلي",, "حركة المخزون"

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 3 أعمدة
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
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
        }


        private async void DynamicButtonReport_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();
                var report = ReportCatalog.FindByDisplayName(option);
                var role = _userSession.CurrentUser?.Role;

                if (report != null && role != null)
                {
                    var canView = await _reportPermissionService.CanViewAsync(role.Value, report.Key);
                    if (!canView)
                    {
                        MessageBox.Show("ليس لديك صلاحية لعرض هذا التقرير.");
                        return;
                    }
                }

                switch (option)
                {

                    case "المخزون الحالي":
                        {
                            WindowManager.Show<CurrentStock>();
                            break;
                        }
                    case "حركات الاصناف":
                    case "تفصيل حركة المخزون":
                        {
                            WindowManager.Show<StockMovementsReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تقرير المبيعات":
                        {
                            WindowManager.Show<SalesReport>(WindowSizeType.LargeRectangle);
                            break;
                        }

                    case "حركة المخزون":
                        {
                            WindowManager.Show<StockMovementsReport>();
                            break;
                        }
                    case "تقرير مبيعات الذمم":
                        {
                            WindowManager.Show<CreditSalesReport>();
                            break;
                        }
                    case "اصناف لم تتحرك منذ مدة":
                        {
                            WindowManager.Show<InactiveProductsReport>();
                            break;
                        }
                    case "اجمالي الخصم اصناف":
                        {
                            WindowManager.Show<DiscountSummaryReport>();
                            break;
                        }
                    case "كشف تكلفة صنف تفصيلي":
                        {
                            WindowManager.Show<ItemCostDetailReport>();
                            break;
                        }
                    case "قائمة الأسعار":
                        {
                            WindowManager.Show<PriceListReport>();
                            break;
                        }
                    case "بضائع تحت الحد الأدنى":
                        {
                            WindowManager.Show<LowStockReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "أرصدة المخزون بتاريخ معين":
                        {
                            WindowManager.Show<StockBalanceByDateReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تحليل ربحية الفواتير":
                        {
                            WindowManager.Show<InvoicesProfitBrowser>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "ملخص حركات المخزون":
                    case "ملخص حركة الأصناف":
                        {
                            WindowManager.Show<InventoryMovementSummary>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تقييم المخزون":
                        {
                            WindowManager.Show<StockValuationReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "أرباح الأصناف":
                        {
                            WindowManager.Show<ProductProfitReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "التحصيل والدفع":
                        {
                            WindowManager.Show<CashFlowReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تقرير الأرباح والخسائر":
                        {
                            WindowManager.Show<ProfitLossReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تقرير مبيعات الآجل":
                        {
                            WindowManager.Show<CreditSalesReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "ملخص الخصومات":
                        {
                            WindowManager.Show<DiscountSummaryReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "تفاصيل تكلفة الأصناف":
                        {
                            WindowManager.Show<ItemCostDetailReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "أرصدة المخزون":
                    case "الجرد والفرق":
                        {
                            WindowManager.Show<StockBalancesReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "حركة المواد":
                    case "التسويات المخزنية":
                        {
                            WindowManager.Show<MaterialMovementsReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                    case "الأصناف الراكدة":
                        {
                            WindowManager.Show<InactiveItemsReport>(WindowSizeType.LargeRectangle);
                            break;
                        }
                }
            }
        }


        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار حسب الموضوع
            var groups = new Dictionary<string, string[]>
            {
                { "إدارة المستودعات", new string[] { "إضافة مستودع جديد", "إستعلام او تعديل مستودع" } }
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أعمدة بما أن الأزرار قليلة
                var grid = new UniformGrid
                {
                    Columns = 2,
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += WarehouseDynamicButton_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }

        private void WarehouseDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();


                // Example: switch case for handling each button separately
                switch (option)
                {
                    case "إضافة مستودع جديد":
                        {
                            WindowManager.Show<CreateWarehouse>();
                            break;
                        }

                    case "إستعلام او تعديل مستودع":
                        {
                            WindowManager.Show<WarehousesTable>();
                            break;
                        }
                }
            }
        }


        private void UsersDynamicButton_Click(object sender, RoutedEventArgs e)
        {

            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();


                // Example: switch case for handling each button separately
                switch (option)
                {
                    case "إضافة مستخدم جديد":
                        {
                            if (_userSession.CurrentUser?.Role != Domain.Enums.UserRole.Admin)
                            {
                                MessageBox.Show("فقط المدير يمكنه إنشاء مستخدم جديد.");
                                break;
                            }

                            WindowManager.Show<CreateUser>();
                            break;
                        }
                    case "إستعلام او تعديل مستخدم":
                        {
                            WindowManager.Show<UsersTable>();
                            break;
                        }
                }
            }
        
        }
       

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار حسب الموضوع
            var groups = new Dictionary<string, string[]>
            {
                { "إدارة العلامات التجارية", new string[] { "إضافة علامة تجارية جديدة", "إستعلام او تعديل العلامة التجارية" } }
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أعمدة بما أن الأزرار قليلة
                var grid = new UniformGrid
                {
                    Columns = 2,
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += BrandsDynamicButton_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }
        private void BrandsDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();


                // Example: switch case for handling each button separately
                switch (option)
                {
                    case "إضافة علامة تجارية جديدة":
                        {
                            WindowManager.Show<CreateBrand>(WindowSizeType.MediumRectangle);
                            break;
                        }

                    case "إستعلام او تعديل العلامة التجارية":
                        {
                            WindowManager.Show<BrandsTable>(WindowSizeType.MediumRectangle);
                            break;
                        }
                }
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار حسب الموضوع
            var groups = new Dictionary<string, string[]>
            {
                { "إعدادات الوحدات", new string[]
                { "إضافة وحدة جديدة", "إستعلام او تعديل وحدة" } },
                { "صلاحيات التقارير", new string[]
                { "مدير صلاحيات التقارير" } }
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أعمدة بما أن الأزرار قليلة
                var grid = new UniformGrid
                {
                    Columns = 2,
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += SettingsDynamicButton_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }


        private void SettingsDynamicButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string option = clickedButton.Content.ToString();


                // Example: switch case for handling each button separately
                switch (option)
                {
                    case "إضافة وحدة جديدة":
                        {
                            WindowManager.Show<CreateUnit>();
                            break;
                        }

                    case "إستعلام او تعديل وحدة":
                        {
                            WindowManager.Show<UnitsTable>();
                            break;
                        }
                    case "مدير صلاحيات التقارير":
                        {
                            if (_userSession.CurrentUser?.Role != Domain.Enums.UserRole.Admin)
                            {
                                MessageBox.Show("فقط المدير يمكنه إدارة صلاحيات التقارير.");
                                break;
                            }

                            WindowManager.Show<ReportPermissionsManager>(WindowSizeType.LargeRectangle);
                            break;
                        }
                }
            }
        }

        private void Customers_Click(object sender, RoutedEventArgs e)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };
            scrollViewer.Content = mainPanel;

            // مجموعات الأزرار حسب الموضوع
            var groups = new Dictionary<string, string[]>
            {
                { "إدارة المستخدمين", new string[] { "إضافة مستخدم جديد", "إستعلام او تعديل مستخدم" } }
            };

            foreach (var group in groups)
            {
                // Card لكل مجموعة
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

                // عنوان المجموعة
                var header = new TextBlock
                {
                    Text = group.Key,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                };
                cardPanel.Children.Add(header);

                // Grid للأزرار → 2 أعمدة بما أن الأزرار قليلة
                var grid = new UniformGrid
                {
                    Columns = 2,
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
                        Content = option,
                        Style = (Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")

                    };

                    // Hover effect
                    btn.MouseEnter += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(41, 130, 190));
                    btn.MouseLeave += (s, ev) => border.Background = new SolidColorBrush(Color.FromRgb(51, 150, 211));

                    btn.Click += UsersDynamicButton_Click;

                    border.Child = btn;
                    grid.Children.Add(border);
                }

                cardPanel.Children.Add(grid);
                card.Child = cardPanel;
                mainPanel.Children.Add(card);
            }

            MainContent.Content = scrollViewer;
        }
    }
}
