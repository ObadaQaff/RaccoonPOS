using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Reports.Products.Dtos;
using RaccoonWarehouse.Domain.Reports.Products.Filters;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.Products.Reports
{
    public partial class ProductProfitReport : Window
    {
        private readonly IStockReportService _reportsService; // أو IFinancialReportService
        private readonly IProductService _productService;

        public ProductProfitReport(IStockReportService reportsService, IProductService productService)
        {
            InitializeComponent();
            _reportsService = reportsService;
            _productService = productService;

            Loaded += ProductProfitReport_Loaded;
        }

        private async void ProductProfitReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;

            // تحميل المنتجات (اختياري)
            try
            {
                var pRes = await _productService.GetAllAsync();
                var products = pRes.Data ?? new List<ProductReadDto>();

                // خيار "الكل"
                products.Insert(0, new ProductReadDto { Id = 0, Name = "الكل" });

                ProductComboBox.ItemsSource = products;
                ProductComboBox.SelectedValue = 0;
            }
            catch
            {
                // ignore
            }
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("يرجى اختيار تاريخ البداية والنهاية.");
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
                    filter.ProductId = pid;

                var rows = await _reportsService.GetProductProfitAsync(filter);
                ProductProfitGrid.ItemsSource = rows;

                FillSummary(rows);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void FillSummary(List<ProductProfitRowDto> rows)
        {
            decimal netSales = rows.Sum(x => x.NetSales);
            decimal tax = rows.Sum(x => x.Tax);
            decimal discount = rows.Sum(x => x.Discount);
            decimal cogs = rows.Sum(x => x.COGS);
            decimal gp = rows.Sum(x => x.GrossProfit);
            decimal margin = netSales == 0 ? 0 : Math.Round((gp / netSales) * 100m, 2);

            NetSalesText.Text = netSales.ToString("0.00");
            TotalTaxText.Text = tax.ToString("0.00");
            TotalDiscountText.Text = discount.ToString("0.00");
            TotalCogsText.Text = cogs.ToString("0.00");
            GrossProfitText.Text = gp.ToString("0.00");
            MarginText.Text = margin.ToString("0.##");
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}