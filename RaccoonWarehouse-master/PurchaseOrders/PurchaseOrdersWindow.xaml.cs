using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.PurchaseOrders
{
    public partial class PurchaseOrdersWindow : Window
    {
        private readonly IPurchaseOrderService _service;
        private readonly IPermissionService _permissionService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;
        private readonly ObservableCollection<PurchaseOrderReadDto> _orders = new();

        public PurchaseOrdersWindow(
            IPurchaseOrderService service,
            IPermissionService permissionService,
            IUserSession userSession,
            ILoadingService loadingService)
        {
            _service = service;
            _permissionService = permissionService;
            _userSession = userSession;
            _loadingService = loadingService;
            InitializeComponent();
            OrdersGrid.ItemsSource = _orders;
            UiText.ApplyWindow(this);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            _loadingService.Show();
            try
            {
                var result = await _service.GetAllAsync();
                if (!result.Success)
                {
                    MessageBox.Show(result.Message);
                    return;
                }

                _orders.Clear();
                foreach (var order in result.Data ?? new List<PurchaseOrderReadDto>())
                    _orders.Add(order);

                var canApprove = await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.Approve");
                ApproveButton.IsEnabled = canApprove;
                RejectButton.IsEnabled = canApprove;
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private async void OrdersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => await ShowDetailsAsync();

        private async void ViewDetails_Click(object sender, RoutedEventArgs e) => await ShowDetailsAsync();

        private async Task ShowDetailsAsync()
        {
            if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
                return;

            _loadingService.Show();
            Result<PurchaseOrderWriteDto>? result = null;
            try
            {
                result = await _service.GetByIdAsync(selected.Id);
            }
            finally
            {
                _loadingService.Hide();
            }

            if (result == null || !result.Success || result.Data == null)
            {
                MessageBox.Show(result?.Message ?? UiText.T("تعذر تحميل تفاصيل أمر الشراء.", "Purchase order details could not be loaded."));
                return;
            }

            var detailsWindow = new PurchaseOrderDetailsWindow(result.Data) { Owner = this };
            detailsWindow.ShowDialog();
        }

        private async void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
                return;
            if (selected.Status != PurchaseOrderStatus.Submitted)
            {
                MessageBox.Show(UiText.T("يجب اختيار أمر شراء مرسل للاعتماد.", "Select a submitted purchase order."));
                return;
            }
            if (MessageBox.Show(
                    UiText.T("سيتم تحويل الأمر إلى فاتورة مشتريات وتسجيل المخزون والمحاسبة. هل تريد المتابعة؟", "This will convert the order into a purchase invoice and post stock and accounting. Continue?"),
                    UiText.T("تأكيد الاعتماد", "Confirm approval"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _loadingService.Show();
            try
            {
                var result = await _service.ApproveAsync(selected.Id);
                _loadingService.Hide();
                MessageBox.Show(result.Message);
                if (result.Success)
                    await LoadAsync();
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
                return;

            _loadingService.Show();
            try
            {
                var result = await _service.RejectAsync(selected.Id, "Rejected by approver.");
                _loadingService.Hide();
                MessageBox.Show(result.Message);
                if (result.Success)
                    await LoadAsync();
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
