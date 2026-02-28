using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.DTOs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks
{
    public partial class CurrentStock : Window
    {
        private readonly IStockReportService _stockReportService;

        public ObservableCollection<CurrentStockDto> CurrentStockItems { get; set; }
            = new ObservableCollection<CurrentStockDto>();

        public CurrentStock(IStockReportService stockReportService)
        {
            _stockReportService = stockReportService;
            InitializeComponent();
            DataContext = this;

            Loaded += CurrentStock_Loaded;
        }

        private async void CurrentStock_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStock();
        }

        private async Task LoadStock()
        {
            try
            {
                CurrentStockItems.Clear();

                var data = await _stockReportService.GetCurrentStockAsync();

                foreach (var item in data)
                    CurrentStockItems.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل المخزون: {ex.Message}");
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadStock();
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            string term = SearchText.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(term))
            {
                StockGrid.ItemsSource = CurrentStockItems;
                return;
            }

            var filtered = CurrentStockItems
                .Where(x =>
                    (x.ProductName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ITEMCODE?.Contains(term) ?? false)
                )
                .ToList();

            StockGrid.ItemsSource = filtered;
        }


        /* private void SearchBtn_Click(object sender, RoutedEventArgs e)
         {
             var search = Microsoft.VisualBasic.Interaction.InputBox(
                 "أدخل اسم المنتج للبحث:",
                 "بحث عن منتج");

             if (string.IsNullOrWhiteSpace(search))
                 return;

             var filtered = CurrentStockItems
                 .Where(x => x.ProductName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                 .ToList();

             StockGrid.ItemsSource = filtered;
         }
 */
        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel File (*.xlsx)|*.xlsx",
                    FileName = "CurrentStock.xlsx"
                };

                if (dlg.ShowDialog() != true)
                    return;

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Current Stock");

                    // HEADER ROW
                    ws.Cell(1, 1).Value = "الباركود";
                    ws.Cell(1, 2).Value = "المنتج";
                    ws.Cell(1, 3).Value = "الوحدة";
                    ws.Cell(1, 4).Value = "الكمية";
                    ws.Cell(1, 5).Value = "الحد الأدنى";
                    ws.Cell(1, 6).Value = "تنبيه";

                    ws.Row(1).Style.Font.Bold = true;
                    ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                    int row = 2;

                    foreach (var item in CurrentStockItems)
                    {
                        ws.Cell(row, 1).Value = item.ITEMCODE;
                        ws.Cell(row, 2).Value = item.ProductName;
                        ws.Cell(row, 3).Value = item.UnitName;
                        ws.Cell(row, 4).Value = item.Quantity;
                        ws.Cell(row, 5).Value = item.MinimumQuantity;
                        ws.Cell(row, 6).Value = item.IsLowStock ? "⚠ قليل" : "";

                        row++;
                    }

                    // Auto fit columns
                    ws.Columns().AdjustToContents();

                    workbook.SaveAs(dlg.FileName);
                }

                MessageBox.Show("تم استخراج ملف Excel بنجاح!", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء التصدير: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
