using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class ExpiringStockReport : Window
    {
        private readonly IStockReportService _reports;
        private readonly IUserService _users;
        public ExpiringStockReport(IStockReportService reports, IUserService users)
        {
            InitializeComponent(); _reports = reports; _users = users; SupplierComboBoxSearch.Attach(SupplierComboBox); UiText.ApplyWindow(this); Loaded += LoadedAsync;
        }
        private async void LoadedAsync(object sender, RoutedEventArgs e)
        {
            var suppliers = (await _users.GetAllAsync()).Data?.Where(x => x.Role == UserRole.Supplier).OrderBy(x => x.Name).ToList() ?? new();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = "All suppliers" });
            SupplierComboBoxSearch.SetItems(SupplierComboBox, suppliers); SupplierComboBox.SelectedIndex = 0; await LoadAsync();
        }
        private async Task LoadAsync()
        {
            var days = ((DaysComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() is string value && int.TryParse(value, out var parsed)) ? parsed : 90;
            var supplier = SupplierComboBox.SelectedValue is int id && id > 0 ? id : (int?)null;
            var rows = await _reports.GetExpiringStockAsync(days, supplier); ResultsGrid.ItemsSource = rows; TotalCostText.Text = rows.Sum(x => x.TotalCost).ToString("N3");
        }
        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
        private void Back_Click(object sender, RoutedEventArgs e) => Close();
    }
}
