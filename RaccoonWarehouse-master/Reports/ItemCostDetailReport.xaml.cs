using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Reports.Stocks.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using ClosedXML.Excel;
using Microsoft.Win32;

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
            TotalValueText.Text = rows.Sum(x => x.Total).ToString("0.00000");
        }

        private void ItemCostDetailGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ItemCostDetailGrid.SelectedItem is ItemCostDetailRowDto row && row.ProductId > 0)
                WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window => window.Initialize(row.ProductId));
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

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var rows = ItemCostDetailGrid.Items.OfType<ItemCostDetailRowDto>().ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show(this, "لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    FileName = $"ItemCostDetails_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog(this) != true) return;

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("Item Cost Details");
                sheet.RightToLeft = true;
                sheet.Cell(1, 1).Value = "تفاصيل تكلفة الأصناف / Item Cost Details";
                sheet.Cell(2, 1).Value = "تاريخ التصدير / Exported at";
                sheet.Cell(2, 2).Value = DateTime.Now;
                sheet.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                sheet.Cell(3, 1).Value = "البحث / Search";
                sheet.Cell(3, 2).Value = SearchTextBox.Text?.Trim() ?? string.Empty;

                var headers = new[] { "كود الصنف", "اسم الصنف", "الوحدة", "الكمية", "التكلفة", "الإجمالي" };
                for (var column = 0; column < headers.Length; column++) sheet.Cell(5, column + 1).Value = headers[column];

                var rowNumber = 6;
                foreach (var row in rows)
                {
                    sheet.Cell(rowNumber, 1).Value = row.ItemID;
                    sheet.Cell(rowNumber, 2).Value = row.ItemName;
                    sheet.Cell(rowNumber, 3).Value = row.UnitName;
                    sheet.Cell(rowNumber, 4).Value = row.Quantity;
                    sheet.Cell(rowNumber, 5).Value = row.Cost;
                    sheet.Cell(rowNumber, 6).Value = row.Total;
                    rowNumber++;
                }

                sheet.Range(5, 1, rowNumber - 1, headers.Length).CreateTable();
                sheet.Range(6, 4, rowNumber - 1, 6).Style.NumberFormat.Format = "#,##0.00000";
                sheet.Columns(1, headers.Length).AdjustToContents();
                sheet.SheetView.FreezeRows(5);
                workbook.SaveAs(dialog.FileName);
                MessageBox.Show(this, "تم تصدير ملف Excel بنجاح.\n" + dialog.FileName, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "تعذر تصدير ملف Excel.\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
