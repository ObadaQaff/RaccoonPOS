using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.Filters;
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

namespace RaccoonWarehouse.Products.Reports
{
    /// <summary>
    /// Interaction logic for InactiveProductsReport.xaml
    /// </summary>
    public partial class InactiveProductsReport : Window
    {
        private readonly IStockReportService _reportService;
        public InactiveProductsReport( IStockReportService reportService)
        {
            _reportService = reportService;
            InitializeComponent();
        }
        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int days = int.Parse(DaysTextBox.Text);

                var filter = new InactiveProductsFilterDto
                {
                    DaysWithoutMovement = days,
                    AsOfDate = AsOfDatePicker.SelectedDate ?? DateTime.Today,
                    IncludeZeroStockOnly = ZeroStockCheck.IsChecked == true
                };

                var data = await _reportService.GetInactiveProductsAsync(filter);

                InactiveGrid.ItemsSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

    
