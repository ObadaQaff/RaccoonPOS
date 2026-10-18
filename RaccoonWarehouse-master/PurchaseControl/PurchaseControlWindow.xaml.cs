using RaccoonWarehouse.Application.Service.PurchaseControl;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.PurchaseControl.DTOs;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.PurchaseControl;

public partial class PurchaseControlWindow : Window
{
    private readonly IPurchaseControlService _service;
    private readonly ILoadingService _loading;
    private readonly ObservableCollection<PurchaseControlEntryDto> _entries = new();

    public PurchaseControlWindow(IPurchaseControlService service, ILoadingService loading)
    {
        _service = service;
        _loading = loading;
        InitializeComponent();
        EntriesGrid.ItemsSource = _entries;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var entry = new PurchaseControlEntryDto { PurchaseOrderDate = DateTime.Today, PaymentStatus = "Pending" };
        _entries.Insert(0, entry);
        EntriesGrid.SelectedItem = entry;
        EntriesGrid.ScrollIntoView(entry);
        EntriesGrid.BeginEdit();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is not PurchaseControlEntryDto entry)
        {
            MessageBox.Show("Select a line first.");
            return;
        }
        EntriesGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
        EntriesGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        if (entry.Id > 0)
        {
            MessageBox.Show("This existing line is read-only for now. Add a new line for a correction.");
            return;
        }
        _loading.Show();
        try
        {
            var result = await _service.CreateAsync(entry);
            MessageBox.Show(result.Message);
            if (result.Success) await LoadAsync();
        }
        finally { _loading.Hide(); }
    }

    private async Task LoadAsync()
    {
        _loading.Show();
        try
        {
            var result = await _service.GetAllAsync();
            if (!result.Success) { MessageBox.Show(result.Message); return; }
            _entries.Clear();
            foreach (var entry in result.Data ?? new List<PurchaseControlEntryDto>()) _entries.Add(entry);
        }
        finally { _loading.Hide(); }
    }
}
