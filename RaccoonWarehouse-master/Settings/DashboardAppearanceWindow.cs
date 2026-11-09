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
    private readonly Image _logo = new() { Width = 110, Height = 70, Stretch = Stretch.Uniform };
    private DashboardAppearance _value = new();

    public DashboardAppearanceWindow(DashboardAppearanceService service)
    {
        _service = service;
        Title = "Dashboard Appearance";
        Width = 680;
        Height = 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FlowDirection = FlowDirection.RightToLeft;
        Background = new SolidColorBrush(Color.FromRgb(238, 244, 248));

        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Dashboard Appearance", FontSize = 26, FontWeight = FontWeights.Bold });
        panel.Children.Add(new TextBlock { Text = "Customize dashboard colors, title and logo. Colors support values such as #FFF or #145A4A.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 14) });

        AddColor(panel, "Sidebar color", nameof(DashboardAppearance.SidebarColor));
        AddColor(panel, "Sidebar hover color", nameof(DashboardAppearance.SidebarHoverColor));
        AddColor(panel, "Sidebar pressed color", nameof(DashboardAppearance.SidebarPressedColor));
        AddColor(panel, "Main button color", nameof(DashboardAppearance.PrimaryButtonColor));
        AddColor(panel, "Main button hover color", nameof(DashboardAppearance.PrimaryButtonHoverColor));
        AddColor(panel, "Header start color", nameof(DashboardAppearance.HeaderStartColor));
        AddColor(panel, "Header middle color", nameof(DashboardAppearance.HeaderMiddleColor));
        AddColor(panel, "Header end color", nameof(DashboardAppearance.HeaderEndColor));
        AddText(panel, "Sidebar title", nameof(DashboardAppearance.SidebarTitle));

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
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Loaded += async (_, _) => await LoadAsync();
    }

    private void AddColor(Panel panel, string label, string key) => AddText(panel, $"{label} (hex)", key);

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
}
