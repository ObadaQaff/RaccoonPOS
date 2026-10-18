using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class LowStockReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private readonly IUserService _userService;
        private ObservableCollection<LowStockDto> _items = new();

        public LowStockReport(IStockReportService stockReportService, IUserService userService)
        {
            _stockReportService = stockReportService;
            _userService = userService;
            InitializeComponent();
            SupplierComboBoxSearch.Attach(SupplierComboBox);
            UiText.ApplyWindow(this);
            LowStockGrid.ItemsSource = _items;
            Loaded += LowStockReport_Loaded;
        }

        private async void LowStockReport_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSupplierFilterAsync();
            await LoadDataAsync();
        }

        private async Task LoadSupplierFilterAsync()
        {
            var suppliers = (await _userService.GetAllAsync()).Data?
                .Where(user => user.Role == UserRole.Supplier)
                .OrderBy(user => user.Name)
                .ToList() ?? new();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = UiText.T("الكل", "All suppliers") });
            SupplierComboBoxSearch.SetItems(SupplierComboBox, suppliers);
            SupplierComboBox.SelectedIndex = 0;
        }

        private async Task LoadDataAsync()
        {
            var supplierId = SupplierComboBox.SelectedValue is int selectedSupplierId && selectedSupplierId > 0
                ? selectedSupplierId
                : (int?)null;
            var data = await _stockReportService.GetLowStockAsync(supplierId);

            _items.Clear();
            foreach (var item in data)
                _items.Add(item);

            CountText.Text = _items.Count.ToString();
        }
    }
}
