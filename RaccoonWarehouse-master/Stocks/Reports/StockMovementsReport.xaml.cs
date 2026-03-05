using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.DTOs;
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
                MessageBox.Show($"خطأ: {ex.Message}");
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
                MessageBox.Show($"خطأ في تصدير التقرير: {ex.Message}");
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
                MessageBox.Show($"خطأ في طباعة التقرير: {ex.Message}");
            }
        }

        private StockMovementsSummaryReportDocument? BuildPdfDocument()
        {
            if (_currentRows.Count == 0)
            {
                MessageBox.Show("اعرض التقرير أولاً قبل التصدير أو الطباعة.");
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
