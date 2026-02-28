using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Reports.Stocks.Filters;
using RaccoonWarehouse.Domain.Stock.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
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

            Loaded += InventoryMovementSummary_Loaded;
        }

        private async void InventoryMovementSummary_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Now.Date;
            ToDatePicker.SelectedDate = DateTime.Now.Date;

            // Products (optional filter)
            var productsRes = await _productService.GetAllAsync();
            var list = productsRes?.Data?.ToList() ?? new();
            list.Insert(0, new RaccoonWarehouse.Domain.Products.DTOs.ProductReadDto { Id = 0, Name = "الكل" });

            ProductComboBox.ItemsSource = list;
            ProductComboBox.SelectedValue = 0;
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("اختر من/إلى تاريخ");
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
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}