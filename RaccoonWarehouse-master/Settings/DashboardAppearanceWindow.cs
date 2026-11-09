using Microsoft.Win32;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Settings;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RaccoonWarehouse.Settings;

public sealed class DashboardAppearanceWindow : Window
{
    private readonly DashboardAppearanceService _service;
    private readonly Dictionary<string, TextBox> _fields = new();
    private readonly Dictionary<string, Border> _colorPreviews = new();
    private readonly Image _logo = new() { Width = 110, Height = 70, Stretch = Stretch.Uniform };
    private DashboardAppearance _value = new();

    public DashboardAppearanceWindow(DashboardAppearanceService service)
    {
        _service = service;
        Title = "تخصيص لوحة التحكم / Dashboard Appearance";
        Width = 680;
        Height = 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FlowDirection = FlowDirection.RightToLeft;
        Background = new SolidColorBrush(Color.FromRgb(238, 244, 248));

        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "تخصيص لوحة التحكم / Dashboard Appearance", FontSize = 26, FontWeight = FontWeights.Bold });
        panel.Children.Add(new TextBlock
        {
            Text = "تخصيص الألوان والعنوان والشعار / Customize colors, title, and logo. Use values such as #FFF or #145A4A.",
            FlowDirection = FlowDirection.LeftToRight,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 14)
        });

        AddColor(panel, "لون القائمة / Sidebar color", nameof(DashboardAppearance.SidebarColor));
        AddColor(panel, "لون مرور الماوس / Sidebar hover color", nameof(DashboardAppearance.SidebarHoverColor));
        AddColor(panel, "لون الضغط / Sidebar pressed color", nameof(DashboardAppearance.SidebarPressedColor));
        AddColor(panel, "لون الأزرار / Main button color", nameof(DashboardAppearance.PrimaryButtonColor));
        AddColor(panel, "لون الأزرار عند المرور / Main button hover color", nameof(DashboardAppearance.PrimaryButtonHoverColor));
        AddColor(panel, "بداية تدرج العنوان / Header start color", nameof(DashboardAppearance.HeaderStartColor));
        AddColor(panel, "وسط تدرج العنوان / Header middle color", nameof(DashboardAppearance.HeaderMiddleColor));
        AddColor(panel, "نهاية تدرج العنوان / Header end color", nameof(DashboardAppearance.HeaderEndColor));
        AddText(panel, "عنوان القائمة / Sidebar title", nameof(DashboardAppearance.SidebarTitle));

        var logoRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
        logoRow.Children.Add(_logo);
        var choose = new Button { Content = "Choose logo", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(12, 0, 0, 0) };
        choose.Click += ChooseLogo_Click;
        logoRow.Children.Add(choose);
        var remove = new Button { Content = "Remove", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(8, 0, 0, 0) };
        remove.Click += (_, _) => { _value.LogoBase64 = null; _logo.Source = null; };
        logoRow.Children.Add(remove);
        panel.Children.Add(logoRow);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 18, 0, 0) };
        var save = new Button { Content = "Save", Padding = new Thickness(18, 8, 18, 8) };
        save.Click += async (_, _) => await SaveAsync(save);
        buttons.Children.Add(save);
        var restore = new Button { Content = "Restore defaults", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(8, 0, 0, 0) };
        restore.Click += RestoreDefaults_Click;
        buttons.Children.Add(restore);
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Loaded += async (_, _) => await LoadAsync();
    }

    private void AddColor(Panel panel, string label, string key)
    {
        panel.Children.Add(new TextBlock { Text = $"{label} (hex)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 7, 0, 3) });

        var row = new Grid { MinHeight = 32 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });

        var box = new TextBox
        {
            MinHeight = 32,
            MaxLength = 120,
            FlowDirection = FlowDirection.LeftToRight,
            TextAlignment = TextAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var preview = new Border
        {
            Margin = new Thickness(8, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Colors.Transparent),
            ToolTip = "Color preview"
        };

        _fields[key] = box;
        _colorPreviews[key] = preview;
        box.TextChanged += (_, _) => UpdateColorPreview(key);
        preview.MouseLeftButtonUp += (_, e) =>
        {
            ChooseColor(key);
            e.Handled = true;
        };

        Grid.SetColumn(box, 0);
        Grid.SetColumn(preview, 1);
        row.Children.Add(box);
        row.Children.Add(preview);
        panel.Children.Add(row);
    }

    private void AddText(Panel panel, string label, string key)
    {
        panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 7, 0, 3) });
        var box = new TextBox { MinHeight = 32, MaxLength = 120 };
        _fields[key] = box;
        panel.Children.Add(box);
    }

    private async Task LoadAsync()
    {
        try
        {
            _value = await _service.GetAsync();
            foreach (var property in typeof(DashboardAppearance).GetProperties())
                if (_fields.TryGetValue(property.Name, out var field))
                    field.Text = property.GetValue(_value)?.ToString() ?? string.Empty;
            foreach (var key in _colorPreviews.Keys)
                UpdateColorPreview(key);
            ShowLogo();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async Task SaveAsync(Button save)
    {
        save.IsEnabled = false;
        try
        {
            _value.SidebarColor = _fields[nameof(DashboardAppearance.SidebarColor)].Text;
            _value.SidebarHoverColor = _fields[nameof(DashboardAppearance.SidebarHoverColor)].Text;
            _value.SidebarPressedColor = _fields[nameof(DashboardAppearance.SidebarPressedColor)].Text;
            _value.PrimaryButtonColor = _fields[nameof(DashboardAppearance.PrimaryButtonColor)].Text;
            _value.PrimaryButtonHoverColor = _fields[nameof(DashboardAppearance.PrimaryButtonHoverColor)].Text;
            _value.HeaderStartColor = _fields[nameof(DashboardAppearance.HeaderStartColor)].Text;
            _value.HeaderMiddleColor = _fields[nameof(DashboardAppearance.HeaderMiddleColor)].Text;
            _value.HeaderEndColor = _fields[nameof(DashboardAppearance.HeaderEndColor)].Text;
            _value.SidebarTitle = _fields[nameof(DashboardAppearance.SidebarTitle)].Text;
            DashboardAppearanceService.NormalizeAndValidate(_value);
            await _service.SaveAsync(_value);
            foreach (Window window in System.Windows.Application.Current.Windows)
                if (window is Dashboard dashboard)
                    dashboard.ApplyAppearance(_value);
            MessageBox.Show("Dashboard appearance saved.");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
        finally { save.IsEnabled = true; }
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Restore the default dashboard colors, title, and logo? Click Save to apply them.",
            "Restore defaults",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _value = new DashboardAppearance();
        foreach (var property in typeof(DashboardAppearance).GetProperties())
            if (_fields.TryGetValue(property.Name, out var field))
                field.Text = property.GetValue(_value)?.ToString() ?? string.Empty;
        foreach (var key in _colorPreviews.Keys)
            UpdateColorPreview(key);
        ShowLogo();
    }

    private void ChooseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PNG / JPEG|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > 1024 * 1024)
                throw new ArgumentException("Logo must be 1 MB or smaller.");
            var bytes = File.ReadAllBytes(dialog.FileName);
            using var stream = new MemoryStream(bytes);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames[0].PixelWidth > 6000 || decoder.Frames[0].PixelHeight > 6000)
                throw new ArgumentException("Logo dimensions are too large.");
            _value.LogoBase64 = Convert.ToBase64String(bytes);
            ShowLogo();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private void ShowLogo()
    {
        _logo.Source = null;
        if (string.IsNullOrWhiteSpace(_value.LogoBase64)) return;
        using var stream = new MemoryStream(Convert.FromBase64String(_value.LogoBase64));
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
        _logo.Source = image;
    }

    private void UpdateColorPreview(string key)
    {
        if (!_fields.TryGetValue(key, out var field) || !_colorPreviews.TryGetValue(key, out var preview))
            return;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(field.Text)!;
            preview.Background = new SolidColorBrush(color);
        }
        catch
        {
            preview.Background = new SolidColorBrush(Colors.Transparent);
        }
    }

    private void ChooseColor(string key)
    {
        if (!_fields.TryGetValue(key, out var field))
            return;

        var initialColor = Colors.White;
        try
        {
            initialColor = (Color)ColorConverter.ConvertFromString(field.Text)!;
        }
        catch
        {
        }

        var picker = new Window
        {
            Title = "اختيار اللون / Choose color",
            Width = 360,
            Height = 500,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            FlowDirection = FlowDirection.LeftToRight,
            Background = new SolidColorBrush(Color.FromRgb(238, 244, 248))
        };
        var panel = new StackPanel { Margin = new Thickness(20) };
        var preview = new Border
        {
            Height = 54,
            Margin = new Thickness(0, 0, 0, 12),
            BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(initialColor)
        };
        panel.Children.Add(preview);

        var red = AddColorChannel(panel, "Red", initialColor.R);
        var green = AddColorChannel(panel, "Green", initialColor.G);
        var blue = AddColorChannel(panel, "Blue", initialColor.B);
        void UpdatePickerPreview()
        {
            preview.Background = new SolidColorBrush(Color.FromRgb((byte)red.Value, (byte)green.Value, (byte)blue.Value));
        }
        red.ValueChanged += (_, _) => UpdatePickerPreview();
        green.ValueChanged += (_, _) => UpdatePickerPreview();
        blue.ValueChanged += (_, _) => UpdatePickerPreview();

        var palette = new WrapPanel { Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var hex in new[]
        {
            "#000000", "#FFFFFF", "#808080", "#C0C0C0", "#800000", "#FF0000", "#800080", "#FF00FF",
            "#008000", "#00FF00", "#808000", "#FFFF00", "#000080", "#0000FF", "#008080", "#00FFFF",
            "#145A4A", "#0F4A3E", "#134B70", "#2D8DCF", "#F2C46D", "#FF8A65", "#9C27B0", "#607D8B"
        })
        {
            var swatchColor = (Color)ColorConverter.ConvertFromString(hex)!;
            var swatch = new Button
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(swatchColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Thickness(1),
                ToolTip = hex
            };
            swatch.Click += (_, _) =>
            {
                red.Value = swatchColor.R;
                green.Value = swatchColor.G;
                blue.Value = swatchColor.B;
            };
            palette.Children.Add(swatch);
        }
        panel.Children.Insert(1, palette);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var ok = new Button { Content = "OK", IsDefault = true, Padding = new Thickness(18, 7, 18, 7) };
        ok.Click += (_, _) => picker.DialogResult = true;
        var cancel = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(8, 0, 0, 0) };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        picker.Content = panel;

        if (picker.ShowDialog() != true)
            return;

        field.Text = $"#{(byte)red.Value:X2}{(byte)green.Value:X2}{(byte)blue.Value:X2}";
    }

    private static Slider AddColorChannel(Panel panel, string label, byte value)
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        var slider = new Slider { Minimum = 0, Maximum = 255, Value = value, TickFrequency = 1, IsSnapToTickEnabled = true };
        var number = new TextBlock { Text = value.ToString(), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        slider.ValueChanged += (_, _) => number.Text = ((byte)slider.Value).ToString();
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(number, 2);
        row.Children.Add(slider);
        row.Children.Add(number);
        panel.Children.Add(row);
        return slider;
    }
}
