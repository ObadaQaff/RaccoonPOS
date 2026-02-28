using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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

namespace RaccoonWarehouse.Stocks
{
    /// <summary>
    /// Interaction logic for CreateStock.xaml
    /// </summary>
    public partial class CreateStock : Window
    {

        private IStockService _stockService;
        public CreateStock(IStockService stockService)
        {
            _stockService = stockService;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
        }
    }
}
