using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Invoices
{
    public partial class ProductUnitSaleConfigurationWindow : Window
    {
        public decimal SalePrice { get; private set; }
        public decimal QuantityPerUnit { get; private set; }

        public ProductUnitSaleConfigurationWindow(
            string productName,
            string unitName,
            decimal expectedSalePrice,
            decimal expectedQuantityPerUnit)
        {
            
            InitializeComponent();

            Title = UiText.T("إعداد وحدة البيع", "Configure sale unit");
            DescriptionTextBlock.Text = UiText.T(
                $"الصنف: {productName}\nالوحدة: {unitName}\nأدخل بيانات التحويل والسعر قبل البيع.",
                $"Product: {productName}\nUnit: {unitName}\nEnter the conversion and price before selling.");
            SalePriceHintTextBlock.Text = UiText.T(
                $"السعر المتوقع: {expectedSalePrice:0.000}",
                $"Expected sale price: {expectedSalePrice:0.000}");
            SalePriceTextBox.Text = expectedSalePrice > 0m
                ? expectedSalePrice.ToString("0.###", CultureInfo.InvariantCulture)
                : string.Empty;
            QuantityPerUnitTextBox.Text = expectedQuantityPerUnit > 0m
                ? expectedQuantityPerUnit.ToString("0.######", CultureInfo.InvariantCulture)
                : string.Empty;

            Loaded += (_, _) =>
            {
                SalePriceTextBox.Focus();
                SalePriceTextBox.SelectAll();
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseDecimal(SalePriceTextBox.Text, out var salePrice) || salePrice <= 0m)
            {
                ValidationTextBlock.Text = UiText.T(
                    "يرجى إدخال سعر بيع أكبر من صفر.",
                    "Enter a sale price greater than zero.");
                SalePriceTextBox.Focus();
                return;
            }

            if (!TryParseDecimal(QuantityPerUnitTextBox.Text, out var quantityPerUnit) || quantityPerUnit <= 0m)
            {
                ValidationTextBlock.Text = UiText.T(
                    "يرجى إدخال كمية لكل وحدة أكبر من صفر.",
                    "Enter a quantity per unit greater than zero.");
                QuantityPerUnitTextBox.Focus();
                return;
            }

            SalePrice = salePrice;
            QuantityPerUnit = quantityPerUnit;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private static bool TryParseDecimal(string? value, out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) ||
                   decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}
