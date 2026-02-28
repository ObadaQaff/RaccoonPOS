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
    /// Interaction logic for DiscountSummaryReport.xaml
    /// </summary>
    public partial class DiscountSummaryReport : Window
    {
        public DiscountSummaryReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var sampleData = new List<DiscountSummaryItem>
            {
                new DiscountSummaryItem { ItemID = "I001", ItemName = "صنف 1", Barcode = "123456", QuantitySold = 20, TotalDiscount = 50 },
                new DiscountSummaryItem { ItemID = "I002", ItemName = "صنف 2", Barcode = "654321", QuantitySold = 15, TotalDiscount = 30 },
                new DiscountSummaryItem { ItemID = "I003", ItemName = "صنف 3", Barcode = "987654", QuantitySold = 10, TotalDiscount = 25 },
                new DiscountSummaryItem { ItemID = "I004", ItemName = "صنف 4", Barcode = "456789", QuantitySold = 5, TotalDiscount = 10 }
            };

            DiscountSummaryGrid.ItemsSource = sampleData;
        }
    

    public class DiscountSummaryItem
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalDiscount { get; set; }
    }        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
