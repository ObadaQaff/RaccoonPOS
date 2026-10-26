using ClosedXML.Excel;
using Microsoft.Win32;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using System.Windows.Media;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaccoonWarehouse.Stocks.Reports;

public partial class LowStockReport : Window
{
    private readonly IStockReportService _stockReportService;
    private readonly IUserService _userService;
    private readonly IProductService _productService;
    private readonly ObservableCollection<LowStockDto> _items = new();
    private readonly DispatcherTimer _productSuggestionTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private List<LowStockDto> _allItems = new();
    private List<UserReadDto> _suppliers = new();
    private List<string> _productNames = new();
    private bool _isApplyingProductSuggestion;
    private bool _initialized;
    private bool _busy;
    private bool _ready;
    private bool _openingProductCard;

    public LowStockReport(IStockReportService stockReportService, IUserService userService, IProductService productService)
    {
        _stockReportService = stockReportService;
        _userService = userService;
        _productService = productService;
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent();
        Resources["BarcodeCopyToolTip"] = UiText.T("نسخ الباركود", "Copy barcode");
        SupplierComboBoxSearch.Attach(SupplierComboBox);
        SupplierComboBox.AddHandler(TextBoxBase.TextChangedEvent,
            new TextChangedEventHandler((_, _) => ApplyFilters()), true);
        UiText.ApplyWindow(this);
        ProductSuggestionsPopup.FlowDirection = UiText.CurrentFlowDirection;
        ProductSuggestionsListBox.FlowDirection = UiText.CurrentFlowDirection;
        LowStockGrid.ItemsSource = _items;
        CountText.Text = "0";
        _productSuggestionTimer.Tick += ProductSuggestionTimer_Tick;
        Loaded += LowStockReport_Loaded;
    }

    private void CopyBarcode_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { DataContext: LowStockDto row }) return;
        if (string.IsNullOrWhiteSpace(row.ITEMCODE))
        {
            MessageBox.Show(this, UiText.T("لا يوجد باركود لنسخه.", "There is no barcode to copy."),
                UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try { Clipboard.SetText(row.ITEMCODE); }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(this, UiText.T("تعذر نسخ الباركود. حاول مرة أخرى.", "Could not copy the barcode. Please try again."),
                UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static bool IsReportButtonSource(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is ButtonBase) return true;
            if (source is DataGridRow) return false;
            source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }

    private void LowStockGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.OriginalSource is not DependencyObject source
            || IsReportButtonSource(source)) return;
        if (ItemsControl.ContainerFromElement(LowStockGrid, source) is not DataGridRow { Item: LowStockDto row }) return;
        e.Handled = true;
        OpenProductCard(row.ProductId);
    }

    private void LowStockGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.IsRepeat || Keyboard.Modifiers != ModifierKeys.None
            || e.OriginalSource is not DependencyObject source || IsReportButtonSource(source)) return;
        if (ItemsControl.ContainerFromElement(LowStockGrid, source) is not DataGridRow
            || LowStockGrid.CurrentCell.Item is not LowStockDto row) return;
        e.Handled = true;
        OpenProductCard(row.ProductId);
    }

    private void OpenProductCard(int productId)
    {
        if (_busy || _openingProductCard || productId <= 0) return;
        _openingProductCard = true;
        try
        {
            WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window =>
            {
                window.Owner = this;
                window.Initialize(productId);
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر فتح بطاقة الصنف.", "Could not open the product card.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"));
        }
        finally { _openingProductCard = false; }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || !_ready) return;
        var rows = LowStockGrid.Items.OfType<LowStockDto>().ToList();
        if (rows.Count == 0) return;
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true,
                FileName = $"LowStock_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            if (dialog.ShowDialog(this) != true) return;
            SetBusy(true);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Low Stock");
            sheet.RightToLeft = !UiText.IsEnglish;
            sheet.Cell(1, 1).Value = UiText.T("بضائع تحت الحد الأدنى", "Below minimum stock");
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(2, 1).Value = UiText.T("المورد", "Supplier");
            var supplierId = SelectedSupplierId();
            sheet.Cell(2, 2).Value = supplierId.HasValue
                ? _suppliers.FirstOrDefault(x => x.Id == supplierId)?.Name ?? SupplierComboBox.Text
                : UiText.T("الكل", "All suppliers");
            sheet.Cell(3, 1).Value = UiText.T("اسم الصنف", "Product name");
            sheet.Cell(3, 2).Value = ProductNameTextBox.Text.Trim();
            sheet.Cell(4, 1).Value = UiText.T("الباركود", "Barcode");
            sheet.Cell(4, 2).Style.NumberFormat.Format = "@";
            sheet.Cell(4, 2).Value = BarcodeTextBox.Text.Trim();
            sheet.Cell(5, 1).Value = UiText.T("تاريخ التصدير", "Exported at");
            sheet.Cell(5, 2).Value = DateTime.Now;
            sheet.Cell(5, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            string[] headers = {
                UiText.T("الباركود", "Barcode"), UiText.T("اسم الصنف", "Product name"),
                UiText.T("المورد", "Supplier"), UiText.T("الوحدة", "Unit"),
                UiText.T("الكمية الحالية", "Current quantity"), UiText.T("أقرب انتهاء", "Nearest expiry"),
                UiText.T("الحد الأدنى", "Minimum quantity"), UiText.T("النقص", "Shortage")
            };
            for (var col = 0; col < headers.Length; col++) sheet.Cell(7, col + 1).Value = headers[col];
            var number = 8;
            foreach (var row in rows)
            {
                sheet.Cell(number, 1).Style.NumberFormat.Format = "@";
                sheet.Cell(number, 1).Value = row.ITEMCODE ?? string.Empty;
                sheet.Cell(number, 2).Value = row.ProductName ?? string.Empty;
                sheet.Cell(number, 3).Value = row.SupplierName ?? string.Empty;
                sheet.Cell(number, 4).Value = row.UnitName ?? string.Empty;
                sheet.Cell(number, 5).Value = row.CurrentQuantity;
                if (row.NearestExpiryDate.HasValue) sheet.Cell(number, 6).Value = row.NearestExpiryDate.Value;
                sheet.Cell(number, 7).Value = row.MinimumQuantity;
                sheet.Cell(number, 8).Value = row.Shortage;
                number++;
            }
            sheet.Range(7, 1, number - 1, 8).CreateTable();
            sheet.Range(8, 5, number - 1, 5).Style.NumberFormat.Format = "#,##0.000";
            sheet.Range(8, 6, number - 1, 6).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Range(8, 7, number - 1, 8).Style.NumberFormat.Format = "#,##0.000";
            sheet.Columns(1, 8).AdjustToContents(7, number - 1);
            foreach (var column in sheet.Columns(1, 8)) column.Width = Math.Min(60, Math.Max(15, column.Width));
            sheet.SheetView.FreezeRows(7);
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show(this, UiText.T("تم تصدير ملف Excel.", "Excel file exported.")
                + Environment.NewLine + dialog.FileName, UiText.T("نجاح", "Success"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر تصدير ملف Excel.", "Failed to export the Excel file.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"));
        }
        finally { SetBusy(false); }
    }

    private async void LowStockReport_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        SetBusy(true);
        try
        {
            var result = await _userService.GetAllAsync();
            if (!result.Success) throw new InvalidOperationException(result.Message);
            _suppliers = (result.Data ?? new List<UserReadDto>())
                .Where(user => user.Role == UserRole.Supplier).OrderBy(user => user.Name).ToList();
            var options = new List<UserReadDto> { new() { Id = 0, Name = UiText.T("الكل", "All suppliers") } };
            options.AddRange(_suppliers);
            SupplierComboBoxSearch.SetItems(SupplierComboBox, options);
            SupplierComboBox.SelectedIndex = 0;
            SupplierComboBox.IsDropDownOpen = false;
        }
        catch (Exception ex) { ShowError(ex); }
        try
        {
            var result = await _productService.GetProductNameCatalogAsync();
            _productNames = result.Success ? result.Data ?? new List<string>() : new List<string>();
        }
        catch
        {
            // Optional name suggestions must not prevent the report from loading.
            _productNames = new List<string>();
        }
        finally { SetBusy(false); }
        if (IsVisible) await LoadDataAsync();
    }

    private async void GenerateReport_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private async Task LoadDataAsync()
    {
        if (_busy) return;
        SetBusy(true);
        try
        {
            var data = await _stockReportService.GetLowStockAsync();
            if (!IsVisible) return;
            _allItems = data;
            _ready = true;
            ApplyFilters();
        }
        catch (Exception ex) { ShowError(ex); }
        finally { SetBusy(false); }
    }

    private void ShowError(Exception exception)
    {
        if (IsVisible)
            MessageBox.Show(this, UiText.T("تعذر تحميل التقرير.", "Failed to load the report.")
                + Environment.NewLine + exception.Message, UiText.T("خطأ", "Error"));
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        if (busy)
        {
            CloseProductSuggestions();
            SupplierComboBox.IsDropDownOpen = false;
        }
        SupplierComboBox.IsEnabled = ProductNameTextBox.IsEnabled = BarcodeTextBox.IsEnabled = GenerateReportButton.IsEnabled = !busy;
        ExportExcelButton.IsEnabled = !busy && _ready && _items.Count > 0;
    }

    private int? SelectedSupplierId()
    {
        if (SupplierComboBox.SelectedValue is int id && id > 0) return id;
        var text = SupplierComboBox.Text.Trim();
        var matches = _suppliers.Where(user => string.Equals(user.Name, text, StringComparison.CurrentCultureIgnoreCase))
            .Take(2).ToList();
        return matches.Count == 1 ? matches[0].Id : null;
    }

    private void ApplyFilters()
    {
        if (!_ready) return;
        var supplierId = SelectedSupplierId();
        var name = ProductNameTextBox.Text.Trim();
        var barcode = BarcodeLookup.Normalize(BarcodeTextBox.Text);
        var filtered = _allItems.Where(item =>
            (!supplierId.HasValue || item.SupplierId == supplierId)
            && (name.Length == 0 || MatchesProductName(item.ProductName ?? string.Empty, name))
            && (barcode.Length == 0 ||
                BarcodeLookup.AreEquivalent(item.ITEMCODE, barcode) ||
                (item.ITEMCODE?.Contains(barcode, StringComparison.OrdinalIgnoreCase) ?? false)));
        _items.Clear();
        foreach (var item in filtered) _items.Add(item);
        CountText.Text = _items.Count.ToString();
        ExportExcelButton.IsEnabled = !_busy && _items.Count > 0;
    }

    private void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void BarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

    private void CloseProductSuggestions()
    {
        _productSuggestionTimer.Stop();
        ProductSuggestionsPopup.IsOpen = false;
        ProductSuggestionsListBox.ItemsSource = null;
    }

    private void ProductNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized || _isApplyingProductSuggestion) return;
        ApplyFilters();
        CloseProductSuggestions();
        if (!_busy && ProductNameTextBox.IsKeyboardFocusWithin && ProductNameTextBox.Text.Trim().Length >= 2)
            _productSuggestionTimer.Start();
    }

    private void ProductSuggestionTimer_Tick(object? sender, EventArgs e)
    {
        _productSuggestionTimer.Stop();
        var text = ProductNameTextBox.Text.Trim();
        if (_busy || !IsVisible || !ProductNameTextBox.IsKeyboardFocusWithin || text.Length < 2) return;
        var suggestions = _productNames.Where(name => MatchesProductName(name, text)).Take(15).ToList();
        ProductSuggestionsListBox.ItemsSource = suggestions;
        ProductSuggestionsPopup.IsOpen = suggestions.Count > 0;
    }

    private static bool MatchesProductName(string productName, string searchText)
    {
        var terms = searchText.Split(new[] { ' ', '\t', '\r', '\n', '%' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var start = 0;
        foreach (var term in terms)
        {
            var index = productName.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            start = index + term.Length;
        }
        return terms.Length > 0;
    }

    private void ProductNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && ProductSuggestionsPopup.IsOpen && ProductSuggestionsListBox.Items.Count > 0)
        {
            ProductSuggestionsListBox.SelectedIndex = 0;
            if (ProductSuggestionsListBox.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem item) item.Focus();
            else ProductSuggestionsListBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseProductSuggestions();
            e.Handled = true;
        }
    }

    private void ProductSuggestionsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ProductSuggestionsListBox.SelectedItem is string suggestion)
        {
            ApplyProductSuggestion(suggestion);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape || e.Key == Key.Tab)
        {
            CloseProductSuggestions();
            ProductNameTextBox.Focus();
            e.Handled = e.Key == Key.Escape;
        }
    }

    private void ProductSuggestionsListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(ProductSuggestionsListBox, source) is ListBoxItem item
            && item.Content is string suggestion)
        {
            ApplyProductSuggestion(suggestion);
            e.Handled = true;
        }
    }

    private void ApplyProductSuggestion(string suggestion)
    {
        _isApplyingProductSuggestion = true;
        try
        {
            ProductNameTextBox.Text = suggestion;
            ProductNameTextBox.CaretIndex = ProductNameTextBox.Text.Length;
        }
        finally { _isApplyingProductSuggestion = false; }
        CloseProductSuggestions();
        ProductNameTextBox.Focus();
        ApplyFilters();
    }

    protected override void OnClosed(EventArgs e)
    {
        CloseProductSuggestions();
        _productSuggestionTimer.Tick -= ProductSuggestionTimer_Tick;
        base.OnClosed(e);
    }
}
