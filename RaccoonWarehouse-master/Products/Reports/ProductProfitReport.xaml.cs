using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Reports.Products.Dtos;
using RaccoonWarehouse.Domain.Reports.Products.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Products.Reports
{
    public partial class ProductProfitReport : Window
    {
        private readonly IStockReportService _reportsService;
        private readonly IProductService _productService;

        public ProductProfitReport(IStockReportService reportsService, IProductService productService)
        {
            InitializeComponent();
            _reportsService = reportsService;
            _productService = productService;
            UiText.ApplyWindow(this);
            Loaded += ProductProfitReport_Loaded;
        }

        private async void ProductProfitReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;

            try
            {
                var pRes = await _productService.GetAllAsync();
                var products = pRes.Data ?? new List<ProductReadDto>();
                products.Insert(0, new ProductReadDto { Id = 0, Name = UiText.T("الكل", "All") });

                ProductComboBox.ItemsSource = products;
                ProductComboBox.SelectedValue = 0;
                UiText.ApplyTranslations(this);
            }
            catch
            {
            }
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار تاريخ البداية والنهاية.", "Please choose the start and end dates."));
                    return;
                }

                var filter = new ProductProfitFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date,
                    IncludeReturns = IncludeReturnsCheckBox.IsChecked == true,
                    GroupByUnit = GroupByUnitCheckBox.IsChecked == true
                };

                if (ProductComboBox.SelectedValue is int pid && pid != 0)
                {
                    filter.ProductId = pid;
                }

                var rows = await _reportsService.GetProductProfitAsync(filter);
                ProductProfitGrid.ItemsSource = rows;
                FillSummary(rows);
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
        }

        private void FillSummary(List<ProductProfitRowDto> rows)
        {
            var netSales = rows.Sum(x => x.NetSales);
            var tax = rows.Sum(x => x.Tax);
            var discount = rows.Sum(x => x.Discount);
            var cogs = rows.Sum(x => x.COGS);
            var gp = rows.Sum(x => x.GrossProfit);
            var margin = netSales == 0 ? 0 : Math.Round((gp / netSales) * 100m, 2);

            NetSalesText.Text = netSales.ToString("0.00000");
            TotalTaxText.Text = tax.ToString("0.00000");
            TotalDiscountText.Text = discount.ToString("0.00000");
            TotalCogsText.Text = cogs.ToString("0.00000");
            GrossProfitText.Text = gp.ToString("0.00000");
            MarginText.Text = margin.ToString("0.00000");
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
