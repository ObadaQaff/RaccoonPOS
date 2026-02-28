using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.Reports
{
    /// <summary>
    /// Interaction logic for StockBalancesReport.xaml
    /// </summary>
    public partial class StockBalancesReport : Window
    {
        public StockBalancesReport()
        {
            InitializeComponent();
            LoadSampleData();

        }
        private void LoadSampleData()
        {
            // بيانات تجريبية للعرض
            var sampleData = new List<StockBalance>
            {
                new StockBalance { ItemID = "001", ItemName = "صنف أ", Barcode = "1234567890", Quantity = 50, BalanceDate = new DateTime(2025,9,18) },
                new StockBalance { ItemID = "002", ItemName = "صنف ب", Barcode = "2345678901", Quantity = 20, BalanceDate = new DateTime(2025,9,18) },
                new StockBalance { ItemID = "003", ItemName = "صنف ج", Barcode = "3456789012", Quantity = 0, BalanceDate = new DateTime(2025,9,18) },
                new StockBalance { ItemID = "004", ItemName = "صنف د", Barcode = "4567890123", Quantity = 15, BalanceDate = new DateTime(2025,9,18) },
                new StockBalance { ItemID = "005", ItemName = "صنف هـ", Barcode = "5678901234", Quantity = 100, BalanceDate = new DateTime(2025,9,18) }
            };

            StockBalancesGrid.ItemsSource = sampleData;
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
        }
        // نموذج البيانات لكل صف
        public class StockBalance
        {
            public string ItemID { get; set; }
            public string ItemName { get; set; }
            public string Barcode { get; set; }
            public int Quantity { get; set; }
            public DateTime BalanceDate { get; set; }
        }
    }
}
