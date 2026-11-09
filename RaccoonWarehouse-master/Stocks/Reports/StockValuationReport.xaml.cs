using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class StockValuationReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private readonly IUserService _userService;

        public StockValuationReport(IStockReportService stockReportService, IUserService userService)
        {
            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent();
            SupplierComboBoxSearch.Attach(SupplierComboBox);
            _stockReportService = stockReportService;
            _userService = userService;
            UiText.ApplyWindow(this);
            Loaded += StockValuationReport_Loaded;
        }

        private async void StockValuationReport_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSupplierFilterAsync();
            await LoadReportAsync();
        }

        private async Task LoadSupplierFilterAsync()
        {
            var suppliers = (await _userService.GetAllAsync()).Data?
                .Where(x => x.Role == UserRole.Supplier).OrderBy(x => x.Name).ToList() ?? new();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = UiText.T("الكل", "All suppliers") });
            SupplierComboBoxSearch.SetItems(SupplierComboBox, suppliers);
            SupplierComboBox.SelectedIndex = 0;
        }

        private async Task LoadReportAsync()
        {
            try
            {
                var supplierId = SupplierComboBox.SelectedValue is int selectedSupplierId && selectedSupplierId > 0 ? selectedSupplierId : (int?)null;
                var rows = await _stockReportService.GetStockValuationAsync(supplierId);
                ValuationGrid.ItemsSource = rows;
                TotalValueTextBlock.Text = rows.Sum(x => x.TotalValue).ToString("N3");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ", "Error")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void ValuationGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ValuationGrid.SelectedItem is RaccoonWarehouse.Domain.Reports.Stocks.Dtos.StockValuationRowDto row && row.ProductId > 0)
                WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window => window.Initialize(row.ProductId));
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
