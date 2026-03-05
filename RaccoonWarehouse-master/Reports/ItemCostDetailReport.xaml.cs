using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Reports.Stocks.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Reports
{
    public partial class ItemCostDetailReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private List<ItemCostDetailRowDto> _rows = new();

        public ItemCostDetailReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            Loaded += ItemCostDetailReport_Loaded;
        }

        private async void ItemCostDetailReport_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _rows = await _stockReportService.GetItemCostDetailsAsync();
                ApplyRows(_rows);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void ApplyRows(List<ItemCostDetailRowDto> rows)
        {
            ItemCostDetailGrid.ItemsSource = rows;
            TotalItemsText.Text = rows.Count.ToString();
            TotalValueText.Text = rows.Sum(x => x.Total).ToString("0.000");
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
                    x.ItemID.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ApplyRows(rows.ToList());
        }
    }
}
