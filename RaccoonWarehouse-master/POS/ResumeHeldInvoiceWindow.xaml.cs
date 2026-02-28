using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
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

namespace RaccoonWarehouse.POS
{
    /// <summary>
    /// Interaction logic for ResumeHeldInvoiceWindow.xaml
    /// </summary>
    public partial class ResumeHeldInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceReadDto? SelectedInvoice { get; private set; }

        public ResumeHeldInvoiceWindow(IInvoiceService invoiceService)
        {
            InitializeComponent();
            _invoiceService = invoiceService;
            Loaded += ResumeHeldInvoiceWindow_Loaded;
        }

        private async void ResumeHeldInvoiceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await _invoiceService.GetHeldPOSInvoicesAsync();
            if (result.Success)
                HeldInvoicesGrid.ItemsSource = result.Data;
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedInvoice = HeldInvoicesGrid.SelectedItem as InvoiceReadDto;
            if (SelectedInvoice != null)
            {
                DialogResult = true;
                Close();
            }
        }
    }

}
