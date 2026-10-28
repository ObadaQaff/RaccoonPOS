using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices;

public partial class FractionalSaleConfigurationWindow : Window
{
    private readonly decimal _packagePrice;
    private readonly decimal _packageCost;
    public decimal PartsPerUnit { get; private set; }
    public decimal FractionalSalePrice { get; private set; }
    public decimal FractionalSaleCost { get; private set; }

    public FractionalSaleConfigurationWindow(string productName, string unitName, decimal packagePrice, decimal packageCost,
        decimal suggestedParts = 0m)
    {
        _packagePrice = packagePrice;
        _packageCost = packageCost;
        
        InitializeComponent();
        UiText.ApplyWindow(this);
        PackageTextBlock.Text = string.Format(CultureInfo.CurrentCulture,
            UiText.T("الصنف: {0}\nالوحدة: {1}\nسعر العبوة: {2:0.000}",
                "Product: {0}\nUnit: {1}\nPackage price: {2:0.000}"), productName, unitName, packagePrice);
        if (suggestedParts >= 2m && suggestedParts == decimal.Truncate(suggestedParts))
            PartsTextBox.Text = suggestedParts.ToString("0", CultureInfo.CurrentCulture);
        Loaded += (_, _) => { PartsTextBox.Focus(); PartsTextBox.SelectAll(); };
    }

    private static bool TryRead(string? text, out decimal value)
        => decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
           || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private void PartsTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PartialSalePriceTextBox == null || PartialSaleCostTextBox == null)
            return;
        if (TryRead(PartsTextBox.Text, out var parts) && parts >= 2m && parts == decimal.Truncate(parts))
        {
            PartialSalePriceTextBox.Text = Math.Round(_packagePrice / parts, 3, MidpointRounding.AwayFromZero)
                .ToString("0.000", CultureInfo.CurrentCulture);
            PartialSaleCostTextBox.Text = Math.Round(_packageCost / parts, 3, MidpointRounding.AwayFromZero)
                .ToString("0.000", CultureInfo.CurrentCulture);
        }
        else
        {
            PartialSalePriceTextBox.Clear();
            PartialSaleCostTextBox.Clear();
        }
        ValidationTextBlock.Text = string.Empty;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryRead(PartsTextBox.Text, out var parts) || parts < 2m || parts != decimal.Truncate(parts) ||
            parts > 999_999_999_999m)
        {
            ValidationTextBlock.Text = UiText.T("عدد الأشرطة يجب أن يكون عدداً صحيحاً لا يقل عن 2.",
                "The tape count must be a whole number of at least 2.");
            PartsTextBox.Focus();
            return;
        }
        if (!TryRead(PartialSalePriceTextBox.Text, out var price) || price <= 0m || price > 999_999_999_999m ||
            price != Math.Round(price, 3, MidpointRounding.AwayFromZero))
        {
            ValidationTextBlock.Text = UiText.T("أدخل سعر شريط أكبر من صفر وبحد أقصى ثلاث منازل عشرية.",
                "Enter a tape price greater than zero with at most three decimal places.");
            PartialSalePriceTextBox.Focus();
            return;
        }
        if (!TryRead(PartialSaleCostTextBox.Text, out var cost) || cost < 0m || cost > 999_999_999_999m ||
            cost != Math.Round(cost, 3, MidpointRounding.AwayFromZero))
        {
            ValidationTextBlock.Text = UiText.T("Ø£Ø¯Ø®Ù„ ØªÙƒÙ„ÙØ© Ø§Ù„Ø¬Ø²Ø¡ Ø¨Ø­Ø¯ Ø£Ù‚ØµÙ‰ Ø«Ù„Ø§Ø« Ù…Ù†Ø§Ø²Ù„ Ø¹Ø´Ø±ÙŠØ©.",
                "Enter a partial-sale cost with at most three decimal places.");
            PartialSaleCostTextBox.Focus();
            return;
        }
        PartsPerUnit = parts;
        FractionalSalePrice = price;
        FractionalSaleCost = cost;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
