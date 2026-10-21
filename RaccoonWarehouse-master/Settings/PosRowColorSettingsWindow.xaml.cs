using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Settings;

public partial class PosRowColorSettingsWindow : Window
{
    private readonly IPosRowColorRuleService _service;
    private readonly ObservableCollection<PosRowColorRule> _rules = new();
    private bool _loading;
    private static readonly ConditionOption[] Conditions =
    {
        new("Expired", "Expired"), new("ExpiryWithinDays", "Expiry within days"),
        new("DiscountAtLeast", "Discount at least %"), new("DiscountBlocked", "Discount blocked"),
        new("LowStock", "Low stock"), new("Category", "Product category")
    };

    public PosRowColorSettingsWindow(IPosRowColorRuleService service)
    {
        InitializeComponent(); _service = service; RulesGrid.ItemsSource = _rules; ConditionComboBox.ItemsSource = Conditions;
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
        Title = "ألوان صفوف نقطة البيع"; TitleTextBlock.Text = "ألوان صفوف نقطة البيع";
        DescriptionTextBlock.Text = "إدارة قواعد تلوين الصفوف في نقطة البيع ونافذة البحث. الأولوية الأعلى تطبق أولاً.";
        EditorTitleTextBlock.Text = "محرر القاعدة"; NameLabel.Text = "اسم القاعدة"; ConditionLabel.Text = "الشرط"; ValueLabel.Text = "القيمة";
        CategoryLabel.Text = "رقم أو اسم التصنيف (اختياري)"; PriorityLabel.Text = "الأولوية"; ActiveCheckBox.Content = "مفعلة";
        BackgroundLabel.Text = "لون الخلفية"; ForegroundLabel.Text = "لون النص"; AccentLabel.Text = "لون الحد"; RowPreviewLabel.Text = "المعاينة";
        AddButton.Content = "إضافة"; DeleteButton.Content = "حذف"; DefaultsButton.Content = "استعادة الافتراضي"; CancelButton.Content = "إلغاء"; SaveButton.Content = "حفظ";
    }

    private void RulesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RulesGrid.SelectedItem is not PosRowColorRule rule) return;
        _loading = true; NameTextBox.Text = rule.Name; ConditionComboBox.SelectedValue = rule.ConditionType;
        ValueTextBox.Text = rule.NumericValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        CategoryTextBox.Text = rule.CategoryId?.ToString(CultureInfo.InvariantCulture) ?? rule.CategoryName ?? string.Empty;
        PriorityTextBox.Text = rule.Priority.ToString(CultureInfo.InvariantCulture); ActiveCheckBox.IsChecked = rule.IsActive;
        BackgroundTextBox.Text = rule.BackgroundColor; ForegroundTextBox.Text = rule.ForegroundColor; AccentTextBox.Text = rule.AccentColor;
        _loading = false; UpdatePreview();
    }

    private void ConditionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => CategoryTextBox.IsEnabled = ConditionComboBox.SelectedValue?.ToString() == "Category";

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
        { MessageBox.Show("Please provide a name and valid colors such as #E2F7E9.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        await _service.SaveRulesAsync(_rules.ToList()); MessageBox.Show("POS row color rules saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information); Close();
    }

    private bool ReadEditorIntoRule(PosRowColorRule rule)
    {
        var type = ConditionComboBox.SelectedValue?.ToString() ?? "None";
        var needsValue = type is "ExpiryWithinDays" or "DiscountAtLeast" or "LowStock";
        if (!int.TryParse(PriorityTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority) ||
            (needsValue && !decimal.TryParse(ValueTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)))
        { MessageBox.Show("Priority and condition value must be valid numbers.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
        decimal.TryParse(ValueTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue);
        rule.Name = NameTextBox.Text.Trim(); rule.ConditionType = type; rule.NumericValue = needsValue ? parsedValue : null; rule.Priority = priority; rule.IsActive = ActiveCheckBox.IsChecked == true;
        var category = CategoryTextBox.Text.Trim(); rule.CategoryId = int.TryParse(category, out var categoryId) ? categoryId : null; rule.CategoryName = rule.CategoryId.HasValue ? null : category;
        rule.BackgroundColor = BackgroundTextBox.Text.Trim(); rule.ForegroundColor = ForegroundTextBox.Text.Trim(); rule.AccentColor = AccentTextBox.Text.Trim(); return true;
    }

    private void ColorTextChanged(object sender, TextChangedEventArgs e) { if (!_loading) UpdatePreview(); }
    private void UpdatePreview()
    {
        SetBackground(BackgroundPreview, BackgroundTextBox.Text); SetBackground(ForegroundPreview, ForegroundTextBox.Text); SetBackground(AccentPreview, AccentTextBox.Text); SetBackground(RowPreview, BackgroundTextBox.Text);
        if (IsValidColor(ForegroundTextBox.Text)) RowPreviewText.Foreground = NewBrush(ForegroundTextBox.Text); if (IsValidColor(AccentTextBox.Text)) RowPreview.BorderBrush = NewBrush(AccentTextBox.Text);
    }
    private static void SetBackground(Border target, string value) { if (IsValidColor(value)) target.Background = NewBrush(value); }
    private static bool IsValidColor(string value) { try { _ = NewBrush(value); return true; } catch { return false; } }
    private static SolidColorBrush NewBrush(string value) { var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(value)!; brush.Freeze(); return brush; }
    private static PosRowColorRule Clone(PosRowColorRule rule) => new() { Id = rule.Id, Name = rule.Name, IsActive = rule.IsActive, Priority = rule.Priority, Scope = rule.Scope, ConditionType = rule.ConditionType, NumericValue = rule.NumericValue, CategoryId = rule.CategoryId, CategoryName = rule.CategoryName, BackgroundColor = rule.BackgroundColor, ForegroundColor = rule.ForegroundColor, AccentColor = rule.AccentColor, Label = rule.Label };
    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    private sealed record ConditionOption(string Value, string Display);
}
