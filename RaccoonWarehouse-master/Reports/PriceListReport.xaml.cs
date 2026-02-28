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
    /// Interaction logic for PriceListReport.xaml
    /// </summary>
    public partial class PriceListReport : Window
    {
        public PriceListReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // بيانات عرضية لتعبئة DataGrid
            var sampleData = new List<PriceItem>
            {
                new PriceItem { ItemID = "I001", ItemName = "صنف 1", Barcode = "1234567890123", PurchasePrice = 10, SalePrice = 15, WholesalePrice = 13 },
                new PriceItem { ItemID = "I002", ItemName = "صنف 2", Barcode = "1234567890456", PurchasePrice = 20, SalePrice = 28, WholesalePrice = 25 },
                new PriceItem { ItemID = "I003", ItemName = "صنف 3", Barcode = "1234567890789", PurchasePrice = 15, SalePrice = 22, WholesalePrice = 20 },
                new PriceItem { ItemID = "I004", ItemName = "صنف 4", Barcode = "1234567890111", PurchasePrice = 8, SalePrice = 12, WholesalePrice = 10 },
                new PriceItem { ItemID = "I005", ItemName = "صنف 5", Barcode = "1234567890222", PurchasePrice = 12, SalePrice = 18, WholesalePrice = 15 }
            };

            PriceListGrid.ItemsSource = sampleData;
        }
  
    // نموذج البيانات لكل صف
    public class PriceItem
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public double PurchasePrice { get; set; }
        public double SalePrice { get; set; }
        public double WholesalePrice { get; set; }
    }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
