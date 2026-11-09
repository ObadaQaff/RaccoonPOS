using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Settings;

public partial class PosRowColorSettingsWindow : Window
{
    private readonly IPosRowColorRuleService _service;
    private readonly IProductService _productService;
    private readonly ObservableCollection<PosRowColorRule> _rules = new();
    private readonly ObservableCollection<ProductColorSelectorItem> _productSearchResults = new();
    private readonly ObservableCollection<ProductColorSelectorItem> _selectedProducts = new();
    private bool _loading;
    private string _backgroundColor = "#FFFFFF";
    private string _foregroundColor = "#263238";
    private string _accentColor = "#90A4AE";
    private static readonly string[] BackgroundPalette =
    {
        "#FFFFFF", "#FDE2E2", "#FCE7F3", "#FFF1D6", "#E2F7E9", "#F0FDF4",
        "#FEF3C7", "#E7EEF5", "#E6D9F7", "#D9F2E6", "#D9EAF7", "#F7DEC6"
    };
    private static readonly string[] ForegroundPalette =
    {
        "#263238", "#1F2937", "#1F4568", "#166534", "#9B1C1C", "#9D174D",
        "#92400E", "#374151", "#0F172A", "#FFFFFF"
    };
    private static readonly string[] AccentPalette =
    {
        "#90A4AE", "#DC2626", "#DB2777", "#F59E0B", "#16A34A", "#65A30D",
        "#D97706", "#7B9AB4", "#7C3AED", "#0F766E", "#2563EB", "#475569"
    };
    private static readonly ConditionOption[] Conditions =
    {
        new("Expired", "Expired"), new("ExpiryWithinDays", "Expiry within days"),
        new("DiscountMaximum", "Maximum discount %"), new("DefaultDiscount", "Default discount %"),
        new("DiscountBlocked", "Discount blocked"),
        new("LowStock", "Low stock"), new("Category", "Product category"), new("Product", "Specific products")
    };
    private static readonly Option[] Scopes =
    {
        new("Both", "POS and Product Search"),
        new("POS", "POS only"),
        new("ProductSearch", "Product Search only")
    };
    private static readonly Option[] SaleActions =
    {
        new(PosRowColorActions.None, "Display only"),
        new(PosRowColorActions.Warn, "Warn"),
        new(PosRowColorActions.RequireConfirmation, "Require confirmation"),
        new(PosRowColorActions.BlockSale, "Block sale"),
        new(PosRowColorActions.BlockDiscount, "Block discount")
    };

    public PosRowColorSettingsWindow(IPosRowColorRuleService service, IProductService productService)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent(); _service = service; _productService = productService; RulesGrid.ItemsSource = _rules; ConditionComboBox.ItemsSource = Conditions;
        ScopeComboBox.ItemsSource = Scopes; SaleActionComboBox.ItemsSource = SaleActions;
        ProductSearchResultsGrid.ItemsSource = _productSearchResults;
        SelectedProductsGrid.ItemsSource = _selectedProducts;
        Loaded += PosRowColorSettingsWindow_Loaded;
    }

    private async void PosRowColorSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _loading = true;
        foreach (var rule in await _service.GetRulesAsync()) _rules.Add(Clone(rule));
        if (_rules.Count > 0) RulesGrid.SelectedIndex = 0;
        _loading = false; ApplyTexts();
    }

    private void ApplyTexts()
    {
        if (((App)System.Windows.Application.Current).IsEnglish) return;
        ConditionHintTextBlock.Text = "\u0627\u062e\u062a\u0631 \u0627\u0644\u0645\u0646\u062a\u062c\u0627\u062a \u0627\u0644\u0645\u062d\u062f\u062f\u0629 \u0644\u062a\u062d\u062f\u064a\u062f \u0627\u0644\u0645\u0646\u062a\u062c\u0627\u062a.";
        ProductLabel.Text = "المنتجات";
        ProductSearchHintTextBlock.Text = "ابحث باسم المنتج أو الباركود";
        ProductSearchButton.Content = "بحث";
        Title = "ألوان صفوف نقطة البيع"; TitleTextBlock.Text = "ألوان صفوف نقطة البيع";
        DescriptionTextBlock.Text = "إدارة قواعد تلوين الصفوف في نقطة البيع ونافذة البحث. الأولوية الأعلى تطبق أولاً.";
        EditorTitleTextBlock.Text = "محرر القاعدة"; NameLabel.Text = "اسم القاعدة"; ConditionLabel.Text = "الشرط"; ValueLabel.Text = "القيمة";
        CategoryLabel.Text = "رقم أو اسم التصنيف (اختياري)"; PriorityLabel.Text = "الأولوية"; ActiveCheckBox.Content = "مفعلة";
        BackgroundLabel.Text = "لون الخلفية"; ForegroundLabel.Text = "لون النص"; AccentLabel.Text = "لون الحد"; RowPreviewLabel.Text = "المعاينة";
        AddButton.Content = "إضافة"; DeleteButton.Content = "حذف"; DefaultsButton.Content = "استعادة الافتراضي"; CancelButton.Content = "إلغاء"; SaveButton.Content = "حفظ";
    }

    private async void RulesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RulesGrid.SelectedItem is not PosRowColorRule rule) return;
        _loading = true; NameTextBox.Text = rule.Name; ConditionComboBox.SelectedValue = rule.ConditionType;
        ScopeComboBox.SelectedValue = string.IsNullOrWhiteSpace(rule.Scope) ? "Both" : rule.Scope;
        SaleActionComboBox.SelectedValue = PosRowColorActions.All.Contains(rule.SaleAction, StringComparer.OrdinalIgnoreCase)
            ? rule.SaleAction : PosRowColorActions.None;
        ValueTextBox.Text = rule.NumericValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        CategoryTextBox.Text = rule.CategoryId?.ToString(CultureInfo.InvariantCulture) ?? rule.CategoryName ?? string.Empty;
        PriorityTextBox.Text = rule.Priority.ToString(CultureInfo.InvariantCulture); ActiveCheckBox.IsChecked = rule.IsActive;
        _backgroundColor = rule.BackgroundColor; _foregroundColor = rule.ForegroundColor; _accentColor = rule.AccentColor;
        RefreshColorPanels();
        await LoadSelectedProductsAsync(rule.ProductIds);
        _loading = false; UpdatePreview();
    }

    private void ConditionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var condition = ConditionComboBox.SelectedValue?.ToString();
        CategoryTextBox.IsEnabled = condition == "Category";
        var supportsProductTargets = condition is "Product" or "DiscountMaximum" or "DefaultDiscount";
        ProductSelectorPanel.Visibility = supportsProductTargets ? Visibility.Visible : Visibility.Collapsed;
        ConditionHintTextBlock.Visibility = supportsProductTargets ? Visibility.Collapsed : Visibility.Visible;

        if (supportsProductTargets && !_loading)
        {
            Dispatcher.BeginInvoke(() =>
            {
                EditorScrollViewer.ScrollToEnd();
                ProductSearchTextBox.Focus();
            }, DispatcherPriority.Background);
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var rule = PosRowColorRuleService.DefaultRules().FirstOrDefault() ?? new PosRowColorRule();
        rule.Id = Guid.NewGuid().ToString("N"); rule.Name = "New rule"; rule.Priority = 50; _rules.Add(rule); RulesGrid.SelectedItem = rule;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e) { if (RulesGrid.SelectedItem is PosRowColorRule rule) _rules.Remove(rule); }

    private void DefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        _rules.Clear(); foreach (var rule in PosRowColorRuleService.DefaultRules()) _rules.Add(Clone(rule)); RulesGrid.SelectedIndex = 0;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesGrid.SelectedItem is PosRowColorRule selected && !ReadEditorIntoRule(selected)) return;
        if (_rules.Any(rule => string.IsNullOrWhiteSpace(rule.Name) || !IsValidColor(rule.BackgroundColor) || !IsValidColor(rule.ForegroundColor) || !IsValidColor(rule.AccentColor)))
        { MessageBox.Show("Please provide a name and choose valid colors.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        await _service.SaveRulesAsync(_rules.ToList()); MessageBox.Show("POS row color rules saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information); Close();
    }

    private bool ReadEditorIntoRule(PosRowColorRule rule)
    {
        var type = ConditionComboBox.SelectedValue?.ToString() ?? "None";
        var needsValue = type is "ExpiryWithinDays" or "DiscountMaximum" or "DefaultDiscount" or "LowStock";
        decimal value = 0m;
        if (!int.TryParse(PriorityTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority) ||
            (needsValue && !decimal.TryParse(ValueTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)))
        { MessageBox.Show("Priority and condition value must be valid numbers.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
        if (type is "DiscountMaximum" or "DefaultDiscount" && (value < 0m || value > 100m))
        { MessageBox.Show("Discount percentage must be between 0 and 100.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
        if (type == "Product" && _selectedProducts.Count == 0)
        { MessageBox.Show("Select at least one product for this rule.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
        decimal.TryParse(ValueTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue);
        rule.Name = NameTextBox.Text.Trim(); rule.Label = rule.Name; rule.ConditionType = type; rule.NumericValue = needsValue ? parsedValue : null; rule.Priority = priority; rule.IsActive = ActiveCheckBox.IsChecked == true;
        rule.Scope = ScopeComboBox.SelectedValue?.ToString() ?? "Both";
        rule.SaleAction = SaleActionComboBox.SelectedValue?.ToString() ?? PosRowColorActions.None;
        rule.ProductIds = type is "Product" or "DiscountMaximum" or "DefaultDiscount"
            ? _selectedProducts.Select(product => product.ProductId).Distinct().ToList()
            : new List<int>();
        var category = CategoryTextBox.Text.Trim(); rule.CategoryId = int.TryParse(category, out var categoryId) ? categoryId : null; rule.CategoryName = rule.CategoryId.HasValue ? null : category;
        rule.BackgroundColor = _backgroundColor; rule.ForegroundColor = _foregroundColor; rule.AccentColor = _accentColor; return true;
    }

    private async Task LoadSelectedProductsAsync(IEnumerable<int> productIds)
    {
        _selectedProducts.Clear();
        var ids = productIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return;

        try
        {
            var result = await _productService.GetProductColorSelectorItemsAsync(ids);
            foreach (var product in result.Data ?? new List<ProductColorSelectorItem>())
                _selectedProducts.Add(product);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Selected products could not be loaded.\n\n{ex.Message}", "Product selector", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ProductSearchButton_Click(object sender, RoutedEventArgs e)
    {
        var searchText = ProductSearchTextBox.Text.Trim();
        if (searchText.Length < 2)
        {
            MessageBox.Show("Type at least two characters of a product name or barcode.", "Product selector", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            ProductSearchButton.IsEnabled = false;
            _productSearchResults.Clear();
            var result = await _productService.SearchProductColorSelectorAsync(searchText);
            foreach (var product in result.Data ?? new List<ProductColorSelectorItem>())
            {
                if (_selectedProducts.All(selected => selected.ProductId != product.ProductId))
                    _productSearchResults.Add(product);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Products could not be searched.\n\n{ex.Message}", "Product selector", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            ProductSearchButton.IsEnabled = true;
        }
    }

    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ProductColorSelectorItem product)
            return;

        if (_selectedProducts.All(selected => selected.ProductId != product.ProductId))
            _selectedProducts.Add(product);
        _productSearchResults.Remove(product);
    }

    private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ProductColorSelectorItem product)
            return;

        _selectedProducts.Remove(product);
    }

    private void ProductSearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            ProductSearchButton_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void RefreshColorPanels()
    {
        BuildColorPanel(BackgroundColorPanel, BackgroundPalette, _backgroundColor, color =>
        {
            _backgroundColor = color;
            UpdatePreview();
        });
        BuildColorPanel(ForegroundColorPanel, ForegroundPalette, _foregroundColor, color =>
        {
            _foregroundColor = color;
            UpdatePreview();
        });
        BuildColorPanel(AccentColorPanel, AccentPalette, _accentColor, color =>
        {
            _accentColor = color;
            UpdatePreview();
        });
    }

    private static void BuildColorPanel(Panel panel, IEnumerable<string> palette, string selectedColor, Action<string> onSelected)
    {
        panel.Children.Clear();
        var colors = palette.ToList();
        if (!colors.Contains(selectedColor, StringComparer.OrdinalIgnoreCase))
            colors.Insert(0, selectedColor);

        foreach (var color in colors)
        {
            var isSelected = string.Equals(color, selectedColor, StringComparison.OrdinalIgnoreCase);
            var button = new Button
            {
                Tag = color,
                Content = isSelected ? "✓" : string.Empty,
                Width = 30,
                Height = 26,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = NewBrush(color),
                Foreground = NewBrush(GetContrastColor(color)),
                BorderBrush = isSelected ? NewBrush("#1F4568") : NewBrush("#B8C6D3"),
                BorderThickness = new Thickness(isSelected ? 3 : 1),
                ToolTip = isSelected ? "Current color" : "Choose color"
            };
            button.Click += (_, _) => onSelected((string)button.Tag);
            panel.Children.Add(button);
        }
    }

    private void UpdatePreview()
    {
        SetBackground(BackgroundPreview, _backgroundColor); SetBackground(ForegroundPreview, _foregroundColor); SetBackground(AccentPreview, _accentColor); SetBackground(RowPreview, _backgroundColor);
        if (IsValidColor(_foregroundColor)) RowPreviewText.Foreground = NewBrush(_foregroundColor); if (IsValidColor(_accentColor)) RowPreview.BorderBrush = NewBrush(_accentColor);
        RefreshColorPanels();
    }
    private static void SetBackground(Border target, string value) { if (IsValidColor(value)) target.Background = NewBrush(value); }
    private static bool IsValidColor(string value) { try { _ = NewBrush(value); return true; } catch { return false; } }
    private static SolidColorBrush NewBrush(string value) { var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(value)!; brush.Freeze(); return brush; }
    private static string GetContrastColor(string value)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(value)!;
            var brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
            return brightness < 150 ? "#FFFFFF" : "#1F2937";
        }
        catch
        {
            return "#1F2937";
        }
    }
    private static PosRowColorRule Clone(PosRowColorRule rule) => new() { Id = rule.Id, Name = rule.Name, IsActive = rule.IsActive, Priority = rule.Priority, Scope = rule.Scope, ConditionType = rule.ConditionType, NumericValue = rule.NumericValue, CategoryId = rule.CategoryId, CategoryName = rule.CategoryName, ProductIds = rule.ProductIds?.ToList() ?? new List<int>(), BackgroundColor = rule.BackgroundColor, ForegroundColor = rule.ForegroundColor, AccentColor = rule.AccentColor, Label = rule.Label, SaleAction = rule.SaleAction };
    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    private sealed record ConditionOption(string Value, string Display);
    private sealed record Option(string Value, string Display);
}
