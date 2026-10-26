using RaccoonWarehouse.Application.Service.PurchaseControl;
using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseControl.DTOs;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.PurchaseControl;

public partial class PurchaseControlDashboardWindow : Window
{
    private readonly IPurchaseControlService _controlService;
    private readonly IPurchaseOrderService _poService;
    private readonly IPurchaseRequestService _prService;
    private readonly ILoadingService _loading;
    private readonly ObservableCollection<SupplierSummary> _suppliers = new();

    public PurchaseControlDashboardWindow(IPurchaseControlService controlService, IPurchaseOrderService poService, IPurchaseRequestService prService, ILoadingService loading)
    {
        _controlService = controlService; _poService = poService; _prService = prService; _loading = loading;
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent(); SupplierGrid.ItemsSource = _suppliers;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading.Show();
        try
        {
            var pos = (await _poService.GetAllAsync()).Data ?? new();
            var prs = (await _prService.GetAllAsync()).Data ?? new();
            var controls = (await _controlService.GetAllAsync()).Data ?? new();
            PoCountText.Text = pos.Count.ToString();
            PrCountText.Text = prs.Count.ToString();
            ApprovedPrText.Text = prs.Count(x => x.Status == PurchaseRequestStatus.Approved).ToString();
            PurchaseTotalText.Text = $"{controls.Sum(x => x.PurchaseOrderAmount ?? 0m):N3} JOD";
            PaidTotalText.Text = $"{controls.Sum(x => x.PaidAmount ?? 0m):N3} JOD";
            OutstandingText.Text = $"{controls.Sum(x => x.DueAmount ?? 0m):N3} JOD";
            _suppliers.Clear();
            foreach (var group in controls.GroupBy(x => string.IsNullOrWhiteSpace(x.SupplierName) ? "Unspecified" : x.SupplierName!.Trim()).OrderBy(x => x.Key))
                _suppliers.Add(new SupplierSummary(group.Key, group.Sum(x => x.PurchaseOrderAmount ?? 0m), group.Sum(x => x.PaidAmount ?? 0m)));
        }
        finally { _loading.Hide(); }
    }

    private sealed class SupplierSummary
    {
        public string SupplierName { get; }
        public decimal PurchaseTotal { get; }
        public decimal PaidTotal { get; }
        public decimal Outstanding => PurchaseTotal - PaidTotal;
        public SupplierSummary(string name, decimal purchaseTotal, decimal paidTotal) { SupplierName = name; PurchaseTotal = purchaseTotal; PaidTotal = paidTotal; }
    }
}
