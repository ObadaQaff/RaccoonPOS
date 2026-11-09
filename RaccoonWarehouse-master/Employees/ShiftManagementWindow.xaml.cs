using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Domain.Employees.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Employees;

public partial class ShiftManagementWindow : Window
{
    private readonly IShiftService _service;
    private readonly ObservableCollection<ShiftTemplateListItemDto> _items = new();

    public ShiftManagementWindow(IShiftService service)
    {
        _service = service;
        InitializeComponent();
        WorkingDay.ItemsSource = Enum.GetValues<DayOfWeek>();
        WorkingDay.SelectedItems.Add(DayOfWeek.Sunday);
        ShiftsGrid.ItemsSource = _items;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var data = await _service.GetTemplatesAsync();
        _items.Clear();
        foreach (var item in data)
            _items.Add(item);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseShiftTime(StartTime.Text, out var start) || !TryParseShiftTime(EndTime.Text, out var end))
        {
            MessageBox.Show("أدخل الوقت بصيغة 23 أو 23:00 أو 23:30.", "وقت غير صحيح", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var days = WorkingDay.SelectedItems.Cast<DayOfWeek>().ToList();
        var result = await _service.SaveTemplateAsync(new ShiftTemplateEditDto
        {
            Name = ShiftName.Text,
            StartTime = start,
            EndTime = end,
            IsOvernight = Overnight.IsChecked == true,
            WorkingDays = days
        });
        if (!result.Success)
            MessageBox.Show(string.Join(Environment.NewLine, result.Errors));
        else
        {
            ShiftName.Clear();
            StartTime.Text = "08:00";
            EndTime.Text = "16:00";
            Overnight.IsChecked = false;
            WorkingDay.SelectedItems.Clear();
            WorkingDay.SelectedItems.Add(DayOfWeek.Sunday);
            await LoadAsync();
        }
    }

    private static bool TryParseShiftTime(string? text, out TimeSpan time)
    {
        time = default;
        var value = text?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var hourOnly))
        {
            if (hourOnly is >= 0 and <= 23)
            {
                time = TimeSpan.FromHours(hourOnly);
                return true;
            }

            return false;
        }

        return TimeSpan.TryParseExact(
            value,
            new[] { "h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss" },
            CultureInfo.InvariantCulture,
            out time)
            && time >= TimeSpan.Zero
            && time < TimeSpan.FromDays(1);
    }
}
