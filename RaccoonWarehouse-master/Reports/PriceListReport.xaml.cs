using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Reports.Stocks.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Reports
{
    public partial class PriceListReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private List<PriceListRowDto> _rows = new();

        public PriceListReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            Loaded += PriceListReport_Loaded;
        }

        private async void PriceListReport_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _rows = await _stockReportService.GetPriceListAsync();
                ApplyRows(_rows);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void ApplyRows(List<PriceListRowDto> rows)
        {
            PriceListGrid.ItemsSource = rows;
            TotalItemsText.Text = rows.Count.ToString();
            TotalProductsText.Text = rows.Select(x => x.ProductId).Distinct().Count().ToString();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            var search = SearchTextBox.Text?.Trim();
            var rows = _rows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows.Where(x =>
                    x.ItemName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.ItemID.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.UnitName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ApplyRows(rows.ToList());
        }
    }
}
