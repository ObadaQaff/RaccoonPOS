using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using ClosedXML.Excel;
using Microsoft.Win32;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers;
using RaccoonWarehouse.Domain.Reports.Products.Dtos;
using RaccoonWarehouse.Domain.Reports.Products.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;

namespace RaccoonWarehouse.Reports;

public sealed class ProductSalesAverageReport : Window
{
    private readonly ProductSalesAverageService _service;
    private readonly IProductService _productService;
    private readonly IUserService _userService;
    private readonly ComboBox _supplier = new()
    {
        Width = 250, IsEditable = true, IsTextSearchEnabled = false,
        StaysOpenOnEdit = true, DisplayMemberPath = "Name", SelectedValuePath = "Id"
    };
    private List<UserReadDto> _suppliers = new();
    private string _loadedSupplierName = string.Empty;
    private readonly ComboBox _months = new() { ItemsSource = Enumerable.Range(1, 12), SelectedIndex = 0, Width = 90 };
    private readonly TextBox _name = new() { Width = 280 };
    private readonly Popup _productSuggestionsPopup = new()
    {
        Placement = PlacementMode.Bottom, StaysOpen = false,
        AllowsTransparency = true, Focusable = false
    };
    private readonly ListBox _productSuggestionsListBox = new();
    private readonly DispatcherTimer _productSuggestionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(180)
    };
    private readonly TextBox _code = new() { Width = 170 };
    private readonly Button _generate = new();
    private readonly Button _export = new() { IsEnabled = false };
    private readonly Button _back = new();
    private readonly TextBlock _status = new() { Margin = new Thickness(12) };
    private readonly DataGrid _grid = new()
    {
        AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false,
        EnableRowVirtualization = true,
        SelectionUnit = DataGridSelectionUnit.Cell,
        ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader
    };
    private List<ProductSalesAverageRowDto> _rows = new();
    private ProductSalesAverageFilterDto? _loadedFilter;
    private DateTime _endExclusive;
    private bool _busy;
    private List<string> _productNames = new();
    private bool _isApplyingProductSuggestion;
    private bool _initialized;
    private bool _openingProductCard;

    public ProductSalesAverageReport(ProductSalesAverageService service, IProductService productService, IUserService userService)
    {
        _service = service;
        _productService = productService;
        _userService = userService;
        Title = "متوسط مبيعات الأصناف";
        Width = 1400;
        Height = 800;
        MinWidth = 800;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(242, 246, 250));
        AddReportStyles();

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = root;
        var header = new Border { Background = Brush("#2D8DCF") };
        header.Child = new TextBlock
        {
            Text = Title, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(header);

        var content = new Grid { Margin = new Thickness(14, 14, 14, 0) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        var filterCard = Card();
        filterCard.Padding = new Thickness(12);
        filterCard.Margin = new Thickness(0, 0, 0, 10);
        var filterContent = new StackPanel();
        var fields = new WrapPanel();
        _months.Width = 140;
        _code.Width = 200;
        AddField(fields, "عدد الأشهر", _months);
        AddField(fields, "اسم الصنف", _name);
        AddField(fields, "رمز الصنف", _code);
        AddField(fields, "المورد", _supplier);
        SupplierComboBoxSearch.Attach(_supplier);
        filterContent.Children.Add(fields);
        var actions = new WrapPanel { Margin = new Thickness(8, 6, 8, 0) };
        _generate.Content = "عرض التقرير";
        _export.Content = "تصدير إلى Excel";
        _back.Content = "رجوع";
        _generate.Style = (Style)Resources["ReportPrimaryButton"];
        _export.Style = _back.Style = (Style)Resources["ReportSecondaryButton"];
        foreach (var button in new[] { _generate, _export, _back })
        {
            button.MinWidth = 150;
            button.Margin = new Thickness(0, 0, 10, 6);
            actions.Children.Add(button);
        }
        filterContent.Children.Add(actions);
        filterCard.Child = filterContent;
        content.Children.Add(filterCard);

        var note = new TextBlock
        {
            Text = "الكميات حسب وحدة البيع. المبيعات المكتملة فقط؛ دون المرتجعات أو الكميات المجانية. المبلغ بعد خصم السطر وقبل الضريبة وخصم الفاتورة.",
            Foreground = Brush("#52677A"), FontSize = 13,
            Margin = new Thickness(4, 0, 4, 10), TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(note, 1);
        content.Children.Add(note);
        var tableCard = Card();
        tableCard.Padding = new Thickness(10);
        _grid.Style = (Style)Resources["ReportGrid"];
        tableCard.Child = _grid;
        Grid.SetRow(tableCard, 2);
        content.Children.Add(tableCard);

        var footer = new Border { Padding = new Thickness(14, 8, 14, 10) };
        _status.Margin = new Thickness(0);
        _status.Foreground = Brush("#52677A");
        _status.TextWrapping = TextWrapping.Wrap;
        footer.Child = _status;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        AddBarcodeColumn();
        AddColumn("اسم الصنف", nameof(ProductSalesAverageRowDto.ProductName));
        AddColumn("الوحدة", nameof(ProductSalesAverageRowDto.UnitName));
        AddColumn("إجمالي الكمية المباعة", nameof(ProductSalesAverageRowDto.TotalSoldQuantity), "N3");
        AddColumn("متوسط الكمية الشهري", nameof(ProductSalesAverageRowDto.AverageMonthlyQuantity), "N3");
        AddColumn("قيمة المبيعات", nameof(ProductSalesAverageRowDto.TotalSalesAmount), "N3");
        AddColumn("آخر بيع", nameof(ProductSalesAverageRowDto.LastSaleDate), "yyyy-MM-dd");
        _generate.Click += async (_, _) => await LoadAsync();
        _export.Click += (_, _) => Export();
        _grid.MouseDoubleClick += ReportGrid_MouseDoubleClick;
        _grid.PreviewKeyDown += ReportGrid_PreviewKeyDown;
        _months.SelectionChanged += (_, _) => Invalidate();
        ConfigureProductSuggestions();
        _name.TextChanged += NameTextChanged;
        _name.PreviewKeyDown += NamePreviewKeyDown;
        _productSuggestionTimer.Tick += ProductSuggestionTimerTick;
        _productSuggestionsListBox.PreviewKeyDown += ProductSuggestionsPreviewKeyDown;
        _productSuggestionsListBox.PreviewMouseLeftButtonUp += ProductSuggestionsPreviewMouseLeftButtonUp;
        _back.Click += (_, _) => Close();
        _code.TextChanged += (_, _) => Invalidate();
        _supplier.SelectionChanged += (_, _) => Invalidate();
        _supplier.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => Invalidate()), true);
        Loaded += async (_, _) => await InitializeAsync();
        UiText.ApplyWindow(this);
    }

    private void AddField(Panel panel, string label, Control control)
    {
        var field = new StackPanel { Margin = new Thickness(8, 0, 8, 8) };
        field.Children.Add(new TextBlock
        {
            Text = label, Margin = new Thickness(0, 0, 0, 4),
            FontWeight = FontWeights.SemiBold, Foreground = Brush("#333333")
        });
        control.Style = (Style)Resources[control is ComboBox ? "ReportCombo" : "ReportInput"];
        field.Children.Add(control);
        if (ReferenceEquals(control, _name))
            field.Children.Add(_productSuggestionsPopup);
        panel.Children.Add(field);
    }

    private void AddColumn(string header, string property, string? format = null) =>
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = header, Binding = new Binding(property) { StringFormat = format },
            MinWidth = 120,
            Width = new DataGridLength(property == nameof(ProductSalesAverageRowDto.ProductName) ? 2 : 1,
                DataGridLengthUnitType.Star)
        });

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

    private void ReportGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.OriginalSource is not DependencyObject source
            || IsReportButtonSource(source)) return;
        if (ItemsControl.ContainerFromElement(_grid, source) is not DataGridRow { Item: ProductSalesAverageRowDto row }) return;
        e.Handled = true;
        OpenProductCard(row.ProductId);
    }

    private void ReportGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.IsRepeat || Keyboard.Modifiers != ModifierKeys.None
            || e.OriginalSource is not DependencyObject source || IsReportButtonSource(source)) return;
        if (ItemsControl.ContainerFromElement(_grid, source) is not DataGridRow
            || _grid.CurrentCell.Item is not ProductSalesAverageRowDto row) return;
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

    private void AddBarcodeColumn()
    {
        var cell = new FrameworkElementFactory(typeof(DockPanel));
        cell.SetValue(DockPanel.LastChildFillProperty, true);
        var copy = new FrameworkElementFactory(typeof(Button));
        copy.SetValue(DockPanel.DockProperty, Dock.Right);
        copy.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
        copy.SetValue(Control.PaddingProperty, new Thickness(6));
        copy.SetValue(FrameworkElement.ToolTipProperty, UiText.T("نسخ الباركود", "Copy barcode"));
        copy.SetValue(Control.BackgroundProperty, Brushes.Transparent);
        copy.SetValue(Control.BorderBrushProperty, Brushes.Transparent);
        copy.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
        copy.AddHandler(Button.ClickEvent, new RoutedEventHandler(CopyBarcode_Click));
        var icon = new FrameworkElementFactory(typeof(FontAwesome.WPF.ImageAwesome));
        icon.SetValue(FontAwesome.WPF.ImageAwesome.IconProperty, FontAwesome.WPF.FontAwesomeIcon.Copy);
        icon.SetValue(FrameworkElement.WidthProperty, 14d);
        icon.SetValue(FrameworkElement.HeightProperty, 14d);
        icon.SetValue(FontAwesome.WPF.ImageAwesome.ForegroundProperty, Brush("#27445D"));
        copy.AppendChild(icon);
        cell.AppendChild(copy);
        var barcode = new FrameworkElementFactory(typeof(TextBlock));
        barcode.SetBinding(TextBlock.TextProperty, new Binding(nameof(ProductSalesAverageRowDto.ItemCode)));
        barcode.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight);
        barcode.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        barcode.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        cell.AppendChild(barcode);
        _grid.Columns.Add(new DataGridTemplateColumn
        {
            Header = UiText.T("رمز الصنف", "Item code"),
            SortMemberPath = nameof(ProductSalesAverageRowDto.ItemCode),
            ClipboardContentBinding = new Binding(nameof(ProductSalesAverageRowDto.ItemCode)),
            MinWidth = 180, Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = new DataTemplate { VisualTree = cell }
        });
    }

    private void CopyBarcode_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { DataContext: ProductSalesAverageRowDto row }) return;
        var barcode = row.ItemCode?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(barcode))
        {
            MessageBox.Show(this, UiText.T("لا يوجد باركود لنسخه.", "There is no barcode to copy."),
                UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try { Clipboard.SetText(barcode); }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(this, UiText.T("تعذر نسخ الباركود. حاول مرة أخرى.", "Could not copy the barcode. Please try again."),
                UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private static Border Card() => new()
    {
        Background = Brushes.White, CornerRadius = new CornerRadius(12),
        BorderBrush = Brush("#D0D7DD"), BorderThickness = new Thickness(1)
    };

    private void AddReportStyles()
    {
        // Match the existing Sales Report styles without changing application-wide resources.
        Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse("""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style x:Key="ReportPrimaryButton" TargetType="Button">
                <Setter Property="Background" Value="#2D8DCF"/>
                <Setter Property="Foreground" Value="White"/>
                <Setter Property="FontWeight" Value="Bold"/>
                <Setter Property="FontSize" Value="16"/>
                <Setter Property="Cursor" Value="Hand"/>
                <Setter Property="BorderThickness" Value="0"/>
                <Setter Property="Padding" Value="14,8"/>
                <Setter Property="MinHeight" Value="38"/>
                <Setter Property="Template">
                  <Setter.Value>
                    <ControlTemplate TargetType="Button">
                      <Border Background="{TemplateBinding Background}" CornerRadius="10"
                              Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                      </Border>
                    </ControlTemplate>
                  </Setter.Value>
                </Setter>
                <Style.Triggers>
                  <Trigger Property="IsMouseOver" Value="True"><Setter Property="Opacity" Value="0.88"/></Trigger>
                  <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.5"/></Trigger>
                </Style.Triggers>
              </Style>
              <Style x:Key="ReportSecondaryButton" TargetType="Button" BasedOn="{StaticResource ReportPrimaryButton}">
                <Setter Property="Background" Value="#E3EAF0"/>
                <Setter Property="Foreground" Value="#2D8DCF"/>
              </Style>
              <Style x:Key="ReportInput" TargetType="TextBox">
                <Setter Property="Height" Value="38"/>
                <Setter Property="FontSize" Value="15"/>
                <Setter Property="Padding" Value="8,4"/>
                <Setter Property="Background" Value="White"/>
                <Setter Property="BorderBrush" Value="#CBD5DF"/>
                <Setter Property="BorderThickness" Value="1"/>
                <Setter Property="VerticalContentAlignment" Value="Center"/>
              </Style>
              <Style x:Key="ReportCombo" TargetType="ComboBox">
                <Setter Property="Height" Value="38"/>
                <Setter Property="FontSize" Value="15"/>
                <Setter Property="Padding" Value="6,0"/>
                <Setter Property="Background" Value="White"/>
                <Setter Property="BorderBrush" Value="#CBD5DF"/>
                <Setter Property="BorderThickness" Value="1"/>
                <Setter Property="VerticalContentAlignment" Value="Center"/>
              </Style>
              <Style x:Key="ReportGrid" TargetType="DataGrid">
                <Setter Property="RowHeight" Value="40"/>
                <Setter Property="ColumnHeaderHeight" Value="44"/>
                <Setter Property="GridLinesVisibility" Value="None"/>
                <Setter Property="HeadersVisibility" Value="Column"/>
                <Setter Property="BorderThickness" Value="0"/>
                <Setter Property="Background" Value="White"/>
                <Setter Property="RowBackground" Value="White"/>
                <Setter Property="FontSize" Value="15"/>
                <Setter Property="AlternatingRowBackground" Value="#F7FAFD"/>
              </Style>
            </ResourceDictionary>
            """));
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        SetBusy(true);
        _status.Text = UiText.T("جار تحميل التقرير...", "Loading report...");
        try
        {
            var result = await _productService.GetProductNameCatalogAsync();
            _productNames = result.Success ? result.Data ?? new List<string>() : new List<string>();
        }
        catch
        {
            // Optional suggestions must not prevent the report from loading.
            _productNames = new List<string>();
        }
        finally { SetBusy(false); }
        SetBusy(true);
        try
        {
            var result = await _userService.GetAllAsync();
            if (!result.Success) throw new InvalidOperationException(result.Message);
            _suppliers = (result.Data ?? new List<UserReadDto>())
                .Where(user => user.Role == UserRole.Supplier).OrderBy(user => user.Name).ToList();
            var options = new List<UserReadDto> { new() { Id = 0, Name = UiText.T("الكل", "All suppliers") } };
            options.AddRange(_suppliers);
            SupplierComboBoxSearch.SetItems(_supplier, options);
            _supplier.SelectedIndex = 0;
            _supplier.IsDropDownOpen = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر تحميل التقرير.", "Failed to load the report.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"));
        }
        finally { SetBusy(false); }
        if (IsVisible) await LoadAsync();
    }

    private void ConfigureProductSuggestions()
    {
        _productSuggestionsPopup.PlacementTarget = _name;
        _productSuggestionsPopup.FlowDirection = UiText.CurrentFlowDirection;
        _productSuggestionsListBox.FlowDirection = UiText.CurrentFlowDirection;
        _productSuggestionsListBox.DisplayMemberPath = ".";
        _productSuggestionsListBox.FontSize = 15;
        _productSuggestionsListBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        _productSuggestionsPopup.Child = new Border
        {
            Width = _name.Width, MaxHeight = 230, Background = Brushes.White,
            BorderBrush = Brush("#B7CAD8"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(2),
            Child = _productSuggestionsListBox
        };
    }

    private void CloseProductSuggestions()
    {
        _productSuggestionTimer.Stop();
        _productSuggestionsPopup.IsOpen = false;
        _productSuggestionsListBox.ItemsSource = null;
    }

    private void NameTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isApplyingProductSuggestion) return;
        Invalidate();
        CloseProductSuggestions();
        if (!_busy && _name.IsKeyboardFocusWithin && _name.Text.Trim().Length >= 2)
            _productSuggestionTimer.Start();
    }

    private void ProductSuggestionTimerTick(object? sender, EventArgs e)
    {
        _productSuggestionTimer.Stop();
        var searchText = _name.Text.Trim();
        if (_busy || !IsVisible || !_name.IsKeyboardFocusWithin || searchText.Length < 2) return;
        var suggestions = _productNames
            .Where(name => MatchesProductName(name, searchText))
            .Take(15).ToList();
        _productSuggestionsListBox.ItemsSource = suggestions;
        _productSuggestionsPopup.IsOpen = suggestions.Count > 0;
    }

    private static bool MatchesProductName(string productName, string searchText)
    {
        var terms = searchText.Split(new[] { ' ', '\t', '\r', '\n', '%' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var searchStart = 0;
        foreach (var term in terms)
        {
            var index = productName.IndexOf(term, searchStart, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            searchStart = index + term.Length;
        }
        return terms.Length > 0;
    }

    private void NamePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && _productSuggestionsPopup.IsOpen && _productSuggestionsListBox.Items.Count > 0)
        {
            _productSuggestionsListBox.SelectedIndex = 0;
            if (_productSuggestionsListBox.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem item)
                item.Focus();
            else _productSuggestionsListBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseProductSuggestions();
            e.Handled = true;
        }
    }

    private void ProductSuggestionsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _productSuggestionsListBox.SelectedItem is string suggestion)
        {
            ApplyProductSuggestion(suggestion);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape || e.Key == Key.Tab)
        {
            CloseProductSuggestions();
            _name.Focus();
            e.Handled = e.Key == Key.Escape;
        }
    }

    private void ProductSuggestionsPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Ignore scrollbar clicks; only an actual suggestion row should be accepted.
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(_productSuggestionsListBox, source) is ListBoxItem item
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
            _name.Text = suggestion;
            _name.CaretIndex = _name.Text.Length;
        }
        finally { _isApplyingProductSuggestion = false; }
        CloseProductSuggestions();
        _name.Focus();
        Invalidate();
    }

    protected override void OnClosed(EventArgs e)
    {
        CloseProductSuggestions();
        _productSuggestionTimer.Tick -= ProductSuggestionTimerTick;
        base.OnClosed(e);
    }

    private int? SelectedSupplierId()
    {
        if (_supplier.SelectedValue is int id && id > 0) return id;
        // The existing searchable combo can retain the name while clearing SelectedValue.
        var text = _supplier.Text.Trim();
        var matches = _suppliers.Where(user => string.Equals(user.Name, text, StringComparison.CurrentCultureIgnoreCase))
            .Take(2).ToList();
        return matches.Count == 1 ? matches[0].Id : null;
    }

    private void Invalidate()
    {
        if (_busy) return;
        _loadedFilter = null;
        _rows.Clear();
        _grid.ItemsSource = null;
        _export.IsEnabled = false;
        _status.Text = UiText.T("اضغط عرض التقرير لتطبيق الفلاتر.", "Click Generate report to apply the filters.");
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        if (busy) CloseProductSuggestions();
        if (busy) _supplier.IsDropDownOpen = false;
        _months.IsEnabled = _name.IsEnabled = _code.IsEnabled = _supplier.IsEnabled = _generate.IsEnabled = !busy;
        _export.IsEnabled = !busy && _loadedFilter != null && _rows.Count > 0;
    }

    private async Task LoadAsync()
    {
        if (_busy) return;
        SetBusy(true);
        _loadedFilter = null;
        _rows = new();
        _grid.ItemsSource = null;
        _status.Text = UiText.T("جار تحميل التقرير...", "Loading report...");
        try
        {
            var filter = new ProductSalesAverageFilterDto
            {
                Months = _months.SelectedItem is int months ? months : 1,
                ProductName = _name.Text.Trim(), ItemCode = _code.Text.Trim(),
                SupplierId = SelectedSupplierId()
            };
            _endExclusive = DateTime.Today.AddDays(1);
            _rows = await _service.GetAsync(filter, _endExclusive);
            _loadedFilter = filter;
            _loadedSupplierName = filter.SupplierId.HasValue
                ? _suppliers.FirstOrDefault(user => user.Id == filter.SupplierId)?.Name ?? string.Empty
                : UiText.T("الكل", "All suppliers");
            _grid.ItemsSource = _rows;
            _status.Text = $"{_endExclusive.AddMonths(-filter.Months):yyyy-MM-dd} — {_endExclusive.AddDays(-1):yyyy-MM-dd} | "
                + UiText.T("عدد الصفوف: ", "Rows: ") + _rows.Count;
        }
        catch (Exception ex)
        {
            _status.Text = UiText.T("تعذر تحميل التقرير.", "Failed to load the report.");
            MessageBox.Show(this, _status.Text + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"));
        }
        finally { SetBusy(false); }
    }

    private void Export()
    {
        if (_busy || _loadedFilter == null || _rows.Count == 0) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true,
            FileName = $"ProductSalesAverage_{DateTime.Today:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog(this) != true) return;
        SetBusy(true);
        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Sales Average");
            sheet.RightToLeft = !UiText.IsEnglish;
            sheet.Cell(1, 1).Value = UiText.T("متوسط مبيعات الأصناف", "Product Sales Average");
            sheet.Cell(2, 1).Value = UiText.T("من تاريخ", "From date");
            sheet.Cell(2, 2).Value = _endExclusive.AddMonths(-_loadedFilter.Months);
            sheet.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(3, 1).Value = UiText.T("إلى تاريخ", "To date");
            sheet.Cell(3, 2).Value = _endExclusive.AddDays(-1);
            sheet.Cell(3, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(4, 1).Value = UiText.T("عدد الأشهر", "Months");
            sheet.Cell(4, 2).Value = _loadedFilter.Months;
            sheet.Cell(5, 1).Value = UiText.T("اسم الصنف", "Product name");
            sheet.Cell(5, 2).Value = _loadedFilter.ProductName ?? "";
            sheet.Cell(6, 1).Value = "ITEMCODE";
            sheet.Cell(6, 2).Value = _loadedFilter.ItemCode ?? "";
            sheet.Cell(7, 1).Value = UiText.T(
                "المبيعات المكتملة فقط؛ دون المرتجعات أو الكميات المجانية. المبلغ بعد خصم السطر وقبل الضريبة وخصم الفاتورة.",
                "Finalized sales only; excludes returns and free quantities. Amount is after line discount, before tax and invoice discount.");
            sheet.Cell(8, 1).Value = UiText.T("المورد", "Supplier");
            sheet.Cell(8, 2).Value = _loadedSupplierName;
            string[] headers =
            [
                "ITEMCODE", UiText.T("اسم الصنف", "Product name"), UiText.T("الوحدة", "Unit"),
                UiText.T("إجمالي الكمية المباعة", "Total sold quantity"),
                UiText.T("متوسط الكمية الشهري", "Average monthly quantity"),
                UiText.T("قيمة المبيعات", "Sales amount"), UiText.T("آخر بيع", "Last sale")
            ];
            for (var col = 0; col < headers.Length; col++) sheet.Cell(9, col + 1).Value = headers[col];
            var row = 10;
            // Match the current grid order, including user sorting.
            foreach (var item in _grid.Items.OfType<ProductSalesAverageRowDto>())
            {
                sheet.Cell(row, 1).Value = item.ItemCode?.ToString() ?? "";
                sheet.Cell(row, 2).Value = item.ProductName ?? "";
                sheet.Cell(row, 3).Value = item.UnitName ?? "";
                sheet.Cell(row, 4).Value = item.TotalSoldQuantity;
                sheet.Cell(row, 5).Value = item.AverageMonthlyQuantity;
                sheet.Cell(row, 6).Value = item.TotalSalesAmount;
                if (item.LastSaleDate.HasValue) sheet.Cell(row, 7).Value = item.LastSaleDate.Value;
                row++;
            }
            sheet.Range(9, 1, row - 1, 7).CreateTable();
            sheet.Range(10, 4, row - 1, 6).Style.NumberFormat.Format = "#,##0.000";
            sheet.Range(10, 7, row - 1, 7).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Columns(1, 7).AdjustToContents(9, row - 1);
            sheet.SheetView.FreezeRows(9);
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show(this, UiText.T("تم تصدير ملف Excel.", "Excel file exported.") + Environment.NewLine + dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر تصدير ملف Excel.", "Failed to export the Excel file.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"));
        }
        finally { SetBusy(false); }
    }
}
