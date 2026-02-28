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
    /// Interaction logic for ItemCostDetailReport.xaml
    /// </summary>
    public partial class ItemCostDetailReport : Window
    {
        public ItemCostDetailReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var sampleData = new List<ItemCostDetail>
            {
                new ItemCostDetail { ItemID = "I001", ItemName = "صنف 1", Barcode = "123456", Quantity = 10, Cost = 15.5m, Total = 155m },
                new ItemCostDetail { ItemID = "I002", ItemName = "صنف 2", Barcode = "654321", Quantity = 5, Cost = 20m, Total = 100m },
                new ItemCostDetail { ItemID = "I003", ItemName = "صنف 3", Barcode = "987654", Quantity = 8, Cost = 12.5m, Total = 100m },
                new ItemCostDetail { ItemID = "I004", ItemName = "صنف 4", Barcode = "456789", Quantity = 15, Cost = 8m, Total = 120m }
            };

            ItemCostDetailGrid.ItemsSource = sampleData;
        }
   

    public class ItemCostDetail
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal Total { get; set; }
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
