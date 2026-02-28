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
    /// Interaction logic for InactiveItemsReport.xaml
    /// </summary>
    public partial class InactiveItemsReport : Window
    {
        public InactiveItemsReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var sampleData = new List<InactiveItem>
            {
                new InactiveItem { ItemID = "I001", ItemName = "صنف 1", Barcode = "123456", Quantity = 10, LastMovementDate = new DateTime(2025, 1, 15) },
                new InactiveItem { ItemID = "I002", ItemName = "صنف 2", Barcode = "654321", Quantity = 5, LastMovementDate = new DateTime(2024, 12, 5) },
                new InactiveItem { ItemID = "I003", ItemName = "صنف 3", Barcode = "987654", Quantity = 8, LastMovementDate = new DateTime(2024, 11, 20) },
                new InactiveItem { ItemID = "I004", ItemName = "صنف 4", Barcode = "456789", Quantity = 15, LastMovementDate = new DateTime(2025, 2, 1) }
            };

            InactiveItemsGrid.ItemsSource = sampleData;
        }
     

    public class InactiveItem
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public DateTime LastMovementDate { get; set; }
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
