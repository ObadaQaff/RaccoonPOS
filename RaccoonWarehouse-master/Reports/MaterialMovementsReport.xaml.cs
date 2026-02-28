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
    /// Interaction logic for MaterialMovementsReport.xaml
    /// </summary>
    public partial class MaterialMovementsReport : Window
    {
        public MaterialMovementsReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // بيانات عرضية لتعبئة DataGrid
            var sampleData = new List<MaterialMovement>
            {
                new MaterialMovement { MovementID = "M001", Date = DateTime.Now.AddDays(-5), ProductName = "منتج 1", Quantity = 10, MovementType = "إدخال" },
                new MaterialMovement { MovementID = "M002", Date = DateTime.Now.AddDays(-4), ProductName = "منتج 2", Quantity = 5, MovementType = "إخراج" },
                new MaterialMovement { MovementID = "M003", Date = DateTime.Now.AddDays(-3), ProductName = "منتج 3", Quantity = 12, MovementType = "إدخال" },
                new MaterialMovement { MovementID = "M004", Date = DateTime.Now.AddDays(-2), ProductName = "منتج 1", Quantity = 3, MovementType = "إخراج" },
                new MaterialMovement { MovementID = "M005", Date = DateTime.Now.AddDays(-1), ProductName = "منتج 4", Quantity = 7, MovementType = "إدخال" }
            };

            MaterialMovementsGrid.ItemsSource = sampleData;
        }
     

    // نموذج البيانات لكل صف
        public class MaterialMovement
        {
            public string MovementID { get; set; }
            public DateTime Date { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public string MovementType { get; set; }
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
