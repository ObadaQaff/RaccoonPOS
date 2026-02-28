using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RaccoonWarehouse.FinancialTransactions
{
    public partial class ProfitLossReport : Window
    {
        private readonly IFinancialTransactionService _service;

        public ProfitLossReport(IFinancialTransactionService service)
        {
            InitializeComponent();
            _service = service;

            Loaded += ProfitLossReport_Loaded;
        }

        private void ProfitLossReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            IncludeReturnsCheck.IsChecked = true;
            IncludeVoidedCheck.IsChecked = false;
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

                var filter = new ProfitLossFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date,
                    IncludeReturns = IncludeReturnsCheck.IsChecked == true,
                    IncludeVoidedTransactions = IncludeVoidedCheck.IsChecked == true
                };

                var (summary, rows) = await _service.GetProfitLossAsync(filter);

                ProfitLossGrid.ItemsSource = rows ?? new List<ProfitLossRowDto>();

                NetSalesText.Text = summary.NetSales.ToString("0.00");
                CogsText.Text = summary.TotalCOGS.ToString("0.00");
                GrossProfitText.Text = summary.GrossProfit.ToString("0.00");
                ExpensesText.Text = summary.TotalExpenses.ToString("0.00");
                NetProfitText.Text = summary.NetProfit.ToString("0.00");
                NetMarginText.Text = summary.NetMarginPercent.ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}