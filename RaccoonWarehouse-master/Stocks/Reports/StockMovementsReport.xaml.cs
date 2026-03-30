using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class StockMovementsReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private List<StockMovementDto> _currentRows = new();

        public StockMovementsReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            UiText.ApplyWindow(this);

            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? from = FromDatePicker.SelectedDate?.Date;
                DateTime? to = ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);

                var data = await _stockReportService.GetStockMovementsAsync(from, to);
                _currentRows = data ?? new List<StockMovementDto>();
                MovementsGrid.ItemsSource = _currentRows;
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

        private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            var document = BuildPdfDocument();
            if (document == null)
                return;

            try
            {
                ReportPrintService.ExportPdf(document, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ في تصدير التقرير", "Error exporting the report")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            var document = BuildPdfDocument();
            if (document == null)
                return;

            try
            {
                ReportPrintService.Print(document, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ في طباعة التقرير", "Error printing the report")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private StockMovementsSummaryReportDocument? BuildPdfDocument()
        {
            if (_currentRows.Count == 0)
            {
                MessageBox.Show(
                    UiText.T("اعرض التقرير أولاً قبل التصدير أو الطباعة.", "Show the report first before exporting or printing."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }

            return new StockMovementsSummaryReportDocument(_currentRows, FromDatePicker.SelectedDate, ToDatePicker.SelectedDate);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
