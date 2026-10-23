using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.PurchaseRequests;

public partial class PurchaseRequestApprovalWindow : Window
{
    private readonly IPurchaseRequestService _service;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILoadingService _loading;
    private readonly ObservableCollection<ApprovalLine> _lines = new();
    private int _requestId;
    private bool _isSaving;

    public PurchaseRequestApprovalWindow(
        IPurchaseRequestService service,
        IPurchaseOrderService purchaseOrderService,
        ILoadingService loading)
    {
        _service = service;
        _purchaseOrderService = purchaseOrderService;
        _loading = loading;
        InitializeComponent();
        LinesGrid.ItemsSource = _lines;
        UiText.ApplyWindow(this);
    }

    public void InitializeForApproval(int requestId) => _requestId = requestId;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_requestId <= 0)
        {
            MessageBox.Show("A valid purchase request is required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        _loading.Show();
        try
        {
            var result = await _service.GetByIdAsync(_requestId);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            if (result.Data.Status != PurchaseRequestStatus.Submitted)
            {
                MessageBox.Show("Only submitted purchase requests can be reviewed.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            RequestSummaryText.Text = $"{result.Data.RequestNumber} | {result.Data.Status}";
            RequestNumberText.Text = result.Data.RequestNumber;
            RequestDateText.Text = result.Data.RequestDate.ToString("dd/MM/yyyy");
            RequesterText.Text = result.Data.RequesterName ?? "-";
            SubmittedByText.Text = result.Data.SubmittedByName ?? result.Data.SubmittedBy?.ToString() ?? "-";
            SubmittedDateText.Text = result.Data.SubmittedDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";
            CategoryText.Text = result.Data.Category.ToString();
            DepartmentText.Text = string.IsNullOrWhiteSpace(result.Data.Department) ? "-" : result.Data.Department;
            PriorityText.Text = string.IsNullOrWhiteSpace(result.Data.Priority) ? "-" : result.Data.Priority;
            RequestNotesText.Text = string.IsNullOrWhiteSpace(result.Data.Notes) ? "-" : result.Data.Notes;
            ReviewNotesTextBox.Text = result.Data.ReviewNotes ?? string.Empty;
            _lines.Clear();
            foreach (var line in result.Data.Lines ?? new List<PurchaseRequestLineWriteDto>())
            {
                var approvalLine = ApprovalLine.FromDto(line);
                approvalLine.PropertyChanged += ApprovalLine_PropertyChanged;
                _lines.Add(approvalLine);
            }
            UpdateTotals();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load the purchase request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (_isSaving || _lines.Count == 0)
            return;

        LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var reviewNotes = ReviewNotesTextBox.Text.Trim();
        if (_lines.Any(x => x.IsRejected && string.IsNullOrWhiteSpace(x.ApprovalNotes)))
        {
            MessageBox.Show("Enter a rejection note for every rejected line.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_lines.All(x => x.IsRejected))
        {
            if (string.IsNullOrWhiteSpace(reviewNotes))
            {
                MessageBox.Show("Enter an overall review note when rejecting the purchase request.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("All lines are rejected. The purchase request will be saved as rejected. Continue?", "Confirm rejection", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
        }

        _isSaving = true;
        _loading.Show();
        try
        {
            var result = await _service.ReviewAsync(new PurchaseRequestApprovalDto
            {
                RequestId = _requestId,
                ReviewNotes = reviewNotes,
                Lines = _lines.Select(x => x.ToDto()).ToList()
            });

            if (result.Success)
            {
                if (result.Data?.Status == PurchaseRequestStatus.Approved)
                {
                    var purchaseOrderResult = await _purchaseOrderService.CreateFromPurchaseRequestAsync(_requestId);
                    _loading.Hide();
                    var message = purchaseOrderResult.Success
                        ? $"{result.Message}\n{purchaseOrderResult.Message}"
                        : $"{result.Message}\nPurchase orders were not created: {purchaseOrderResult.Message}";
                    MessageBox.Show(message, purchaseOrderResult.Success ? "Completed" : "Warning", MessageBoxButton.OK,
                        purchaseOrderResult.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                else
                {
                    _loading.Hide();
                    MessageBox.Show(result.Message, "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Close();
            }
            else
            {
                _loading.Hide();
                MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _loading.Hide();
            MessageBox.Show($"Could not save approval: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loading.Hide();
            _isSaving = false;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void ApprovalLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ApprovalLine.ApprovedQuantity) ||
            e.PropertyName == nameof(ApprovalLine.ApprovedUnitPrice) ||
            e.PropertyName == nameof(ApprovalLine.IsRejected))
            UpdateTotals();
    }

    private void UpdateTotals()
    {
        if (RequestedTotalText != null)
            RequestedTotalText.Text = _lines.Sum(x => x.RequestedQuantity * x.RequestedUnitPrice).ToString("N3");

        if (ApprovedTotalText != null)
            ApprovedTotalText.Text = _lines
                .Where(x => !x.IsRejected)
                .Sum(x => x.ApprovedQuantity * x.ApprovedUnitPrice)
                .ToString("N3");
    }

    private sealed class ApprovalLine : INotifyPropertyChanged
    {
        private decimal _approvedQuantity;
        private decimal _approvedUnitPrice;
        private bool _isRejected;
        private string? _approvalNotes;

        public int LineId { get; init; }
        public string? Barcode { get; init; }
        public string ItemName { get; init; } = string.Empty;
        public decimal RequestedQuantity { get; init; }
        public decimal RequestedUnitPrice { get; init; }
        public decimal ApprovedQuantity
        {
            get => _approvedQuantity;
            set { _approvedQuantity = value; OnPropertyChanged(); }
        }
        public decimal ApprovedUnitPrice
        {
            get => _approvedUnitPrice;
            set { _approvedUnitPrice = value; OnPropertyChanged(); }
        }
        public bool IsRejected
        {
            get => _isRejected;
            set
            {
                _isRejected = value;
                OnPropertyChanged();
            }
        }
        public string? ApprovalNotes
        {
            get => _approvalNotes;
            set { _approvalNotes = value; OnPropertyChanged(); }
        }

        public PurchaseRequestApprovalLineDto ToDto() => new()
        {
            LineId = LineId,
            Decision = IsRejected ? PurchaseRequestLineDecision.Rejected : PurchaseRequestLineDecision.Approved,
            ApprovedQuantity = IsRejected ? 0m : ApprovedQuantity,
            ApprovedUnitPrice = IsRejected ? 0m : ApprovedUnitPrice,
            ApprovalNotes = ApprovalNotes
        };

        public static ApprovalLine FromDto(PurchaseRequestLineWriteDto line) => new()
        {
            LineId = line.Id,
            Barcode = line.Barcode,
            ItemName = line.ItemName,
            RequestedQuantity = line.RequestedQuantity,
            RequestedUnitPrice = line.UnitPrice,
            ApprovedQuantity = line.RequestedQuantity,
            ApprovedUnitPrice = line.UnitPrice,
            IsRejected = false
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
    }
}
