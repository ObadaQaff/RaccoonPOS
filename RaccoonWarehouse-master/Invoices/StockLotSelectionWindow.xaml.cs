using RaccoonWarehouse.Application.Service.Stocks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RaccoonWarehouse.Invoices
{
    public partial class StockLotSelectionWindow : Window
    {
        public PosStockLotOptionDto? SelectedLot { get; private set; }

        public StockLotSelectionWindow(
            string productName,
            IReadOnlyList<PosStockLotOptionDto> lots)
        {
            InitializeComponent();
            ProductNameTextBlock.Text = productName;
            LotsGrid.ItemsSource = lots;
            LotsGrid.SelectedIndex = 0;
            Loaded += (_, _) => LotsGrid.Focus();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }

        private void LotsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrentLot();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e) => SelectCurrentLot();

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void SelectCurrentLot()
        {
            SelectedLot = LotsGrid.SelectedItem as PosStockLotOptionDto;
            if (SelectedLot != null)
                DialogResult = true;
        }
    }
}
