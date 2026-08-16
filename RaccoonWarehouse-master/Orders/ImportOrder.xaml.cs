using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Orders.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.Orders
{
    public partial class ImportOrder : Window
    {
        private readonly IBoxCartApiService _boxCartApiService;
        private readonly IBoxOrderImportService _boxOrderImportService;
        private readonly ILoadingService _loadingService;
        private readonly ObservableCollection<PendingBoxOrderRow> _pendingOrders = new();
        private readonly ObservableCollection<string> _errors = new();
        private bool _isBusy;

        public ImportOrder(
            IBoxCartApiService boxCartApiService,
            IBoxOrderImportService boxOrderImportService,
            ILoadingService loadingService)
        {
            InitializeComponent();
            _boxCartApiService = boxCartApiService;
            _boxOrderImportService = boxOrderImportService;
            _loadingService = loadingService;
            PendingOrdersGrid.ItemsSource = _pendingOrders;
            ErrorsList.ItemsSource = _errors;
            Loaded += ImportOrder_Loaded;
        }

        private async void ImportOrder_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);
            SetResultSummary(null);
            await LoadPendingOrdersAsync();
        }

        private async void RefreshPending_Click(object sender, RoutedEventArgs e)
        {
            await LoadPendingOrdersAsync();
        }

        private async void ImportPending_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingOrders.Count == 0)
            {
                var confirmation = MessageBox.Show(
                    UiText.T(
                        "\u0644\u0627 \u062A\u0648\u062C\u062F \u0637\u0644\u0628\u0627\u062A \u0645\u0639\u0644\u0642\u0629 \u0645\u0639\u0631\u0648\u0636\u0629. \u0647\u0644 \u062A\u0631\u064A\u062F \u0627\u0644\u0645\u062A\u0627\u0628\u0639\u0629 \u0648\u0645\u062D\u0627\u0648\u0644\u0629 \u0627\u0644\u0627\u0633\u062A\u064A\u0631\u0627\u062F\u061F",
                        "No pending orders are currently displayed. Continue and try importing anyway?"),
                    UiText.T("\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u0627\u0633\u062A\u064A\u0631\u0627\u062F", "Confirm Import"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                    return;
            }

            await ImportPendingOrdersAsync();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenOrders_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<OrdersTable>(WindowSizeType.LargeRectangle);
        }

        private async Task LoadPendingOrdersAsync()
        {
            if (_isBusy)
                return;

            SetBusy(true);
            _loadingService.Show();
            try
            {
                _pendingOrders.Clear();
                _errors.Clear();
                PendingCountText.Text = "0";
                ReceivedCountText.Text = "0";
                ImportedCountText.Text = "0";
                SkippedCountText.Text = "0";
                ResultSummaryText.Text = UiText.T(
                    "\u0644\u0645 \u064A\u062A\u0645 \u062A\u0634\u063A\u064A\u0644 \u0639\u0645\u0644\u064A\u0629 \u0627\u0633\u062A\u064A\u0631\u0627\u062F \u0628\u0639\u062F.",
                    "No import has been run yet.");
                StatusText.Text = UiText.T(
                    "\u062C\u0627\u0631\u064A \u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0637\u0644\u0628\u0627\u062A \u0627\u0644\u0645\u0639\u0644\u0642\u0629...",
                    "Loading pending orders...");

                var result = await _boxCartApiService.GetPendingOrdersAsync();
                if (!result.Success || result.Data == null)
                {
                    StatusText.Text = result.Message ?? UiText.T(
                        "\u062A\u0639\u0630\u0631 \u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0637\u0644\u0628\u0627\u062A \u0627\u0644\u0645\u0639\u0644\u0642\u0629.",
                        "Failed to load pending orders.");
                    foreach (var error in result.Errors)
                        AddError(error);
                    return;
                }

                foreach (var order in result.Data.Orders)
                    _pendingOrders.Add(PendingBoxOrderRow.FromDto(order));

                PendingCountText.Text = _pendingOrders.Count.ToString();
                StatusText.Text = _pendingOrders.Count == 0
                    ? UiText.T("\u0644\u0627 \u062A\u0648\u062C\u062F \u0637\u0644\u0628\u0627\u062A \u0645\u0639\u0644\u0642\u0629.", "No pending orders were found.")
                    : string.Format(
                        UiText.T("\u0639\u062F\u062F \u0627\u0644\u0637\u0644\u0628\u0627\u062A \u0627\u0644\u0645\u0639\u0644\u0642\u0629: {0}", "Pending orders count: {0}"),
                        _pendingOrders.Count);
            }
            catch (Exception ex)
            {
                StatusText.Text = UiText.T(
                    "\u062D\u062F\u062B \u062E\u0637\u0623 \u0623\u062B\u0646\u0627\u0621 \u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0628\u0627\u0646\u0627\u062A.",
                    "An error occurred while loading the page.");
                AddError(ex.Message);
            }
            finally
            {
                _loadingService.Hide();
                SetBusy(false);
            }
        }

        private async Task ImportPendingOrdersAsync()
        {
            if (_isBusy)
                return;

            SetBusy(true);
            _loadingService.Show();
            try
            {
                SetResultSummary(null);
                _errors.Clear();
                StatusText.Text = UiText.T(
                    "\u062C\u0627\u0631\u064A \u0627\u0633\u062A\u064A\u0631\u0627\u062F \u0627\u0644\u0637\u0644\u0628\u0627\u062A...",
                    "Importing pending orders...");

                var result = await _boxOrderImportService.ImportPendingAsync();
                if (result.Data != null)
                    SetResultSummary(result.Data);

                if (!result.Success)
                {
                    StatusText.Text = result.Message ?? UiText.T(
                        "\u062A\u0639\u0630\u0631 \u0627\u0633\u062A\u064A\u0631\u0627\u062F \u0627\u0644\u0637\u0644\u0628\u0627\u062A.",
                        "Failed to import pending orders.");
                    foreach (var error in result.Errors)
                        AddError(error);
                    return;
                }

                foreach (var error in result.Data?.Errors ?? new List<string>())
                    AddError(error);

                StatusText.Text = result.Message ?? UiText.T(
                    "\u0627\u0643\u062A\u0645\u0644 \u0627\u0633\u062A\u064A\u0631\u0627\u062F \u0627\u0644\u0637\u0644\u0628\u0627\u062A.",
                    "Pending order import completed.");
            }
            catch (Exception ex)
            {
                StatusText.Text = UiText.T(
                    "\u062D\u062F\u062B \u062E\u0637\u0623 \u0623\u062B\u0646\u0627\u0621 \u062A\u062D\u0636\u064A\u0631 \u0627\u0644\u0635\u0641\u062D\u0629.",
                    "An error occurred while preparing the page.");
                AddError(ex.Message);
            }
            finally
            {
                _loadingService.Hide();
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            _isBusy = isBusy;
            RefreshButton.IsEnabled = !isBusy;
            ImportButton.IsEnabled = !isBusy;
            OpenOrdersButton.IsEnabled = !isBusy;
        }

        private void SetResultSummary(BoxOrderImportResultDto? result)
        {
            ReceivedCountText.Text = (result?.ReceivedCount ?? 0).ToString();
            ImportedCountText.Text = (result?.ImportedCount ?? 0).ToString();
            SkippedCountText.Text = (result?.SkippedCount ?? 0).ToString();

            ResultSummaryText.Text = result == null
                ? UiText.T(
                    "\u0644\u0645 \u064A\u062A\u0645 \u062A\u0634\u063A\u064A\u0644 \u0639\u0645\u0644\u064A\u0629 \u0627\u0633\u062A\u064A\u0631\u0627\u062F \u0628\u0639\u062F.",
                    "No import has been run yet.")
                : string.Format(
                    UiText.T(
                        "\u0627\u0644\u0645\u0633\u062A\u0644\u0645\u0629: {0}\n\u0627\u0644\u0645\u0633\u062A\u0648\u0631\u062F\u0629: {1}\n\u0627\u0644\u0645\u0648\u062C\u0648\u062F\u0629 \u0645\u0633\u0628\u0642\u0627\u064B: {2}\n\u0627\u0644\u0645\u062A\u062C\u0627\u0648\u0632\u0629: {3}",
                        "Received: {0}\nImported: {1}\nAlready existing: {2}\nSkipped: {3}"),
                    result.ReceivedCount,
                    result.ImportedCount,
                    result.ExistingCount,
                    result.SkippedCount);
        }

        private void AddError(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _errors.Add(message);
        }

        private sealed class PendingBoxOrderRow
        {
            public int CartId { get; init; }
            public string CustomerName { get; init; } = string.Empty;
            public string CustomerPhone { get; init; } = string.Empty;
            public string ShopName { get; init; } = string.Empty;
            public int ItemsCount { get; init; }
            public decimal TotalPrice { get; init; }
            public DateTime CreatedDate { get; init; }

            public static PendingBoxOrderRow FromDto(BoxOrderExportDto order)
            {
                return new PendingBoxOrderRow
                {
                    CartId = order.CartId,
                    CustomerName = order.CustomerName ?? string.Empty,
                    CustomerPhone = order.CustomerPhone ?? string.Empty,
                    ShopName = order.ShopName ?? string.Empty,
                    ItemsCount = order.Items.Count,
                    TotalPrice = order.TotalPrice,
                    CreatedDate = order.CreatedDate
                };
            }
        }
    }
}
