using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.PurchaseRequests;

public partial class PurchaseRequestDetailsWindow : Window
{
    private readonly IPurchaseRequestService _service;
    private readonly ILoadingService _loading;
    private int _requestId;

    public PurchaseRequestDetailsWindow(IPurchaseRequestService service, ILoadingService loading)
    {
        _service = service;
        _loading = loading;
        InitializeComponent();
        UiText.ApplyWindow(this);
    }

    public void Initialize(int requestId) => _requestId = requestId;

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

            var request = result.Data;
            SummaryText.Text = $"{request.RequestNumber} | {request.Status}";
            RequestNumberText.Text = request.RequestNumber;
            StatusText.Text = request.Status.ToString();
            CategoryText.Text = request.Category.ToString();
            RequestDateText.Text = request.RequestDate.ToString("dd/MM/yyyy");
            RequesterText.Text = ValueOrDash(request.RequesterName);
            SubmittedText.Text = PersonAndDate(request.SubmittedByName ?? request.SubmittedBy?.ToString(), request.SubmittedDate);
            ApprovedText.Text = PersonAndDate(request.ApprovedByName ?? request.ApprovedBy?.ToString(), request.ApprovedDate);
            RejectedText.Text = PersonAndDate(request.RejectedByName ?? request.RejectedBy?.ToString(), request.RejectedDate);
            DepartmentText.Text = ValueOrDash(request.Department);
            PriorityText.Text = ValueOrDash(request.Priority);
            NotesText.Text = JoinNotes(request.Notes, request.RejectionReason);
            ReviewNotesText.Text = ValueOrDash(request.ReviewNotes);
            LinesGrid.ItemsSource = request.Lines ?? new List<PurchaseRequestLineWriteDto>();
            TotalAmountText.Text = request.TotalAmount.ToString("N3");
            ApprovedTotalText.Text = request.ApprovedTotalAmount.ToString("N3");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load purchase request details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
        finally
        {
            _loading.Hide();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static string ValueOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string PersonAndDate(string? person, DateTime? date)
    {
        var personText = ValueOrDash(person);
        return date.HasValue ? $"{personText} | {date:dd/MM/yyyy HH:mm}" : personText;
    }

    private static string JoinNotes(string? notes, string? rejectionReason)
    {
        var values = new[] { notes, rejectionReason }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();
        return values.Count == 0 ? "-" : string.Join(" | ", values);
    }
}
