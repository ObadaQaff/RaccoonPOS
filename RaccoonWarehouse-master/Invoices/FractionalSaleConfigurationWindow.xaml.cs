using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Invoices
{
    public partial class FractionalSaleConfigurationWindow : Window
    {
        private readonly decimal _packagePrice;
        public decimal PartsPerUnit { get; private set; }
        public string PartName { get; private set; } = "Tape";

        public FractionalSaleConfigurationWindow(string productName, string unitName, decimal packagePrice)
        {
            InitializeComponent();
            _packagePrice = packagePrice;
            Title = $"Configure fractional sale - {productName}";
            PreviewTextBlock.Text = $"Package unit: {unitName}\nPackage price: {_packagePrice:0.000}\nPrice preview will be shown after entering the count.";
            PartsTextBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(PartsTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parts) &&
                !decimal.TryParse(PartsTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out parts))
            {
                MessageBox.Show("Enter a valid number of tapes or pieces.", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (parts < 2m || parts != decimal.Truncate(parts))
            {
                MessageBox.Show("The number must be a whole number greater than 1.", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PartsPerUnit = parts;
            PreviewTextBlock.Text = $"Price per {PartName}: {Math.Round(_packagePrice / parts, 3, MidpointRounding.AwayFromZero):0.000}";
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
