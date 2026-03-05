using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Reports
{
    public partial class DiscountSummaryReport : Window
    {
        private readonly IFinancialTransactionService _financialTransactionService;
        private List<DiscountSummaryRowDto> _rows = new();

        public DiscountSummaryReport(IFinancialTransactionService financialTransactionService)
        {
            InitializeComponent();
            _financialTransactionService = financialTransactionService;
            Loaded += DiscountSummaryReport_Loaded;
        }

        private async void DiscountSummaryReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            await LoadReportAsync();
        }

        private async System.Threading.Tasks.Task LoadReportAsync()
        {
            if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                return;

            try
            {
                _rows = await _financialTransactionService.GetDiscountSummaryAsync(
                    FromDatePicker.SelectedDate.Value,
                    ToDatePicker.SelectedDate.Value);
                ApplyRows(_rows);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void ApplyRows(List<DiscountSummaryRowDto> rows)
        {
            DiscountSummaryGrid.ItemsSource = rows;
            TotalLinesText.Text = rows.Count.ToString();
            TotalDiscountText.Text = rows.Sum(x => x.TotalDiscount).ToString("0.000");
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }
    }
}
