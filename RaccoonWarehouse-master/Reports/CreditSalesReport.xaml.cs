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
    /// Interaction logic for CreditSalesReport.xaml
    /// </summary>
    public partial class CreditSalesReport : Window
    {
        public CreditSalesReport()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var sampleData = new List<CreditSalesItem>
            {
                new CreditSalesItem { InvoiceID = "INV001", Date = DateTime.Today.AddDays(-10), CustomerName = "زبون 1", AmountDue = 500, AmountPaid = 200, Status = "مسدد جزئي" },
                new CreditSalesItem { InvoiceID = "INV002", Date = DateTime.Today.AddDays(-7), CustomerName = "زبون 2", AmountDue = 300, AmountPaid = 0, Status = "غير مسدد" },
                new CreditSalesItem { InvoiceID = "INV003", Date = DateTime.Today.AddDays(-5), CustomerName = "زبون 3", AmountDue = 700, AmountPaid = 700, Status = "مسدد بالكامل" },
                new CreditSalesItem { InvoiceID = "INV004", Date = DateTime.Today.AddDays(-2), CustomerName = "زبون 1", AmountDue = 450, AmountPaid = 100, Status = "مسدد جزئي" }
            };

            CreditSalesReportGrid.ItemsSource = sampleData;
        }
   

    public class CreditSalesItem
    {
        public string InvoiceID { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public string Status { get; set; }
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
