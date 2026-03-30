using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Reports.Stocks.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class InventoryMovementSummary : Window
    {
        private readonly IStockReportService _stockReportService;
        private readonly IProductService _productService;

        public InventoryMovementSummary(IStockReportService stockReportService, IProductService productService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            _productService = productService;
            UiText.ApplyWindow(this);

            Loaded += InventoryMovementSummary_Loaded;
        }

        private async void InventoryMovementSummary_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Now.Date;
            ToDatePicker.SelectedDate = DateTime.Now.Date;

            var productsRes = await _productService.GetAllAsync();
            var list = productsRes?.Data?.ToList() ?? new();
            list.Insert(0, new RaccoonWarehouse.Domain.Products.DTOs.ProductReadDto { Id = 0, Name = UiText.T("الكل", "All") });

            ProductComboBox.ItemsSource = list;
            ProductComboBox.SelectedValue = 0;
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(
                        UiText.T("اختر من/إلى تاريخ", "Choose a from/to date."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var filter = new InventoryMovementSummaryFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date,
                    IncludeInvoices = IncludeInvoicesCheck.IsChecked == true
                };

                if (ProductComboBox.SelectedValue is int pid && pid != 0)
                    filter.ProductId = pid;

                var rows = await _stockReportService.GetInventoryMovementSummaryAsync(filter);
                MovementGrid.ItemsSource = rows;
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

        private void BackBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}
