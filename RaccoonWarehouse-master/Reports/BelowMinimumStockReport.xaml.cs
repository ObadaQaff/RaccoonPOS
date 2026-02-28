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
    /// Interaction logic for BelowMinimumStockReport.xaml
    /// </summary>
    public partial class BelowMinimumStockReport : Window
    {
        public BelowMinimumStockReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var sampleData = new List<BelowMinimumStockItem>
            {
                new BelowMinimumStockItem { ItemID = "IT001", ItemName = "صنف 1", Barcode = "123456789", CurrentQuantity = 3, MinimumQuantity = 10 },
                new BelowMinimumStockItem { ItemID = "IT002", ItemName = "صنف 2", Barcode = "987654321", CurrentQuantity = 5, MinimumQuantity = 15 },
                new BelowMinimumStockItem { ItemID = "IT003", ItemName = "صنف 3", Barcode = "112233445", CurrentQuantity = 2, MinimumQuantity = 8 },
                new BelowMinimumStockItem { ItemID = "IT004", ItemName = "صنف 4", Barcode = "556677889", CurrentQuantity = 0, MinimumQuantity = 5 }
            };

            BelowMinimumStockGrid.ItemsSource = sampleData;
        }
   

    public class BelowMinimumStockItem
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public int CurrentQuantity { get; set; }
        public int MinimumQuantity { get; set; }
    }        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
