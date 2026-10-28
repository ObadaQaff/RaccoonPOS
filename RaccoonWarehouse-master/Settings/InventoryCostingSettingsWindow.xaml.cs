using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Enums;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Settings;

public partial class InventoryCostingSettingsWindow : Window
{
    private readonly IInventoryCostingSettingsService _settingsService;
    private readonly App _app;
    private InventoryCostingMethod _currentMethod;

    public InventoryCostingSettingsWindow(IInventoryCostingSettingsService settingsService)
    {
        
        InitializeComponent();
        _settingsService = settingsService;
        _app = (App)System.Windows.Application.Current;
        Loaded += InventoryCostingSettingsWindow_Loaded;
    }

    private async void InventoryCostingSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowTexts();

        _currentMethod = await _settingsService.GetCurrentMethodAsync();
        MethodComboBox.ItemsSource = new[]
        {
            new CostingOption(InventoryCostingMethod.WeightedAverage, "Weighted average", "المتوسط المرجح"),
            new CostingOption(InventoryCostingMethod.FIFO, "FIFO", "الوارد أولاً يصرف أولاً"),
            new CostingOption(InventoryCostingMethod.FEFO, "FEFO", "الأقرب انتهاءً يصرف أولاً")
        };
        MethodComboBox.DisplayMemberPath = _app.IsEnglish
            ? nameof(CostingOption.EnglishTitle)
            : nameof(CostingOption.ArabicTitle);
        MethodComboBox.SelectedValuePath = nameof(CostingOption.Value);
        MethodComboBox.SelectedValue = _currentMethod;
        SaveButton.IsEnabled = false;
    }

    private void ApplyWindowTexts()
    {
        if (_app.IsEnglish)
        {
            Title = "Inventory Costing";
            TitleTextBlock.Text = "Inventory Costing";
            DescriptionTextBlock.Text = "Choose the costing method used for future inventory issues and cost of goods sold.";
            MethodLabelTextBlock.Text = "Costing method";
            WarningTextBlock.Text = "Changing this setting does not recalculate historical invoices or transactions.";
            SaveButton.Content = "Save";
            CancelButton.Content = "Cancel";
            return;
        }

        Title = "طريقة تقييم المخزون";
        TitleTextBlock.Text = "طريقة تقييم المخزون";
        DescriptionTextBlock.Text = "اختر طريقة احتساب تكلفة الصرف المستقبلي وتكلفة البضاعة المباعة.";
        MethodLabelTextBlock.Text = "طريقة التقييم";
        WarningTextBlock.Text = "تغيير هذا الإعداد لا يعيد احتساب الفواتير أو الحركات التاريخية.";
        SaveButton.Content = "حفظ";
        CancelButton.Content = "إلغاء";
    }

    private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SaveButton.IsEnabled = MethodComboBox.SelectedValue is InventoryCostingMethod selectedMethod
            && selectedMethod != _currentMethod;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (MethodComboBox.SelectedValue is not InventoryCostingMethod selectedMethod)
            return;

        SaveButton.IsEnabled = false;
        var result = await _settingsService.SetCurrentMethodAsync(selectedMethod);

        if (!result.Success)
        {
            MessageBox.Show(
                result.Message,
                _app.IsEnglish ? "Unable to save" : "تعذر الحفظ",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            SaveButton.IsEnabled = true;
            return;
        }

        MessageBox.Show(
            _app.IsEnglish ? "Inventory costing method saved." : "تم حفظ طريقة تقييم المخزون.",
            _app.IsEnglish ? "Saved" : "تم الحفظ",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed record CostingOption(
        InventoryCostingMethod Value,
        string EnglishTitle,
        string ArabicTitle);
}
