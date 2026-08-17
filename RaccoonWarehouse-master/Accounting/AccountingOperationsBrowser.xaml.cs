using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.Operations;
using RaccoonWarehouse.Helpers.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace RaccoonWarehouse.Accounting;

public partial class AccountingOperationsBrowser : Window
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AccountingOperationProcessor _operationProcessor;
    private readonly ILoadingService _loadingService;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isLoading;

    public ObservableCollection<AccountingOperationRow> Operations { get; } = new();

    public AccountingOperationsBrowser(
        AccountingOperationProcessor operationProcessor,
        ILoadingService loadingService,
        IServiceScopeFactory scopeFactory)
    {
        _operationProcessor = operationProcessor;
        _loadingService = loadingService;
        _scopeFactory = scopeFactory;
        InitializeComponent();
        UiText.ApplyWindow(this);
        OperationsGrid.ItemsSource = Operations;
        ApplyLabels();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += async (_, _) => await RefreshSilentlyAsync();
        Loaded += AccountingOperationsBrowser_Loaded;
        Closed += AccountingOperationsBrowser_Closed;
    }

    private void ApplyLabels()
    {
        Title = UiText.T("عمليات المحاسبة", "Accounting Operations");
        TitleText.Text = Title;
        SubtitleText.Text = UiText.T("متابعة عمليات المحاسبة وإعادة المحاولة عند الفشل.", "Monitor accounting operations and retry failures safely.");
        RefreshText.Text = UiText.T("تحديث", "Refresh");
        ProcessNowText.Text = UiText.T("تنفيذ الآن", "Process now");
        RetrySelectedText.Text = UiText.T("إعادة المحدد", "Retry selected");
        RetryFailedText.Text = UiText.T("إعادة الفاشلة", "Retry failed");
        CloseText.Text = UiText.T("إغلاق", "Close");
    }

    private async void AccountingOperationsBrowser_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
        _refreshTimer.Start();
    }

    private void AccountingOperationsBrowser_Closed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
    }

    private async Task RefreshSilentlyAsync()
    {
        if (_isLoading || !IsVisible)
            return;

        try
        {
            _isLoading = true;
            var records = await GetOperationsAsync();
            var selectedId = (OperationsGrid.SelectedItem as AccountingOperationRow)?.Id;
            Operations.Clear();
            foreach (var operation in records)
                Operations.Add(new AccountingOperationRow(operation));

            if (selectedId.HasValue)
                OperationsGrid.SelectedItem = Operations.FirstOrDefault(item => item.Id == selectedId.Value);

            var failed = Operations.Count(item => item.Status == AccountingOperationStatus.Failed);
            SummaryText.Text = UiText.T($"الإجمالي: {Operations.Count} | الفاشلة: {failed}", $"Total: {Operations.Count} | Failed: {failed}");
        }
        catch
        {
            // The next timer tick will retry without interrupting the user.
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _isLoading = true;
            _loadingService.Show();
            var records = await GetOperationsAsync();
            Operations.Clear();
            foreach (var operation in records)
                Operations.Add(new AccountingOperationRow(operation));

            var failed = Operations.Count(item => item.Status == AccountingOperationStatus.Failed);
            SummaryText.Text = UiText.T($"الإجمالي: {Operations.Count} | الفاشلة: {failed}", $"Total: {Operations.Count} | Failed: {failed}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiText.T("تعذر تحميل عمليات المحاسبة", "Could not load accounting operations"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadingService.Hide();
            _isLoading = false;
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void ProcessNowBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _loadingService.Show();
            var processed = await _operationProcessor.ProcessPendingAsync();
            await LoadAsync();
            MessageBox.Show(
                processed
                    ? UiText.T("تم تنفيذ عملية محاسبية. تحقق من الحالة في الجدول.", "One accounting operation was processed. Check its status in the table.")
                    : UiText.T("لا توجد عمليات معلقة جاهزة للتنفيذ.", "There are no pending operations ready to process."),
                UiText.T("عمليات المحاسبة", "Accounting Operations"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiText.T("تعذر تنفيذ العملية", "Could not process the operation"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    private async void RetrySelectedBtn_Click(object sender, RoutedEventArgs e)
    {
        if (OperationsGrid.SelectedItem is not AccountingOperationRow selected)
        {
            MessageBox.Show(UiText.T("يرجى اختيار عملية أولاً.", "Please select an operation first."));
            return;
        }

        if (selected.Status == AccountingOperationStatus.Processing)
        {
            MessageBox.Show(UiText.T("العملية قيد التنفيذ حالياً.", "This operation is currently processing."));
            return;
        }

        if (await RetryOperationAsync(selected.Id))
            await LoadAsync();
    }

    private async void RetryFailedBtn_Click(object sender, RoutedEventArgs e)
    {
        var failed = Operations.Where(item => item.Status == AccountingOperationStatus.Failed).ToList();
        foreach (var operation in failed)
            await RetryOperationAsync(operation.Id);

        if (failed.Count > 0)
            await LoadAsync();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private async Task<List<AccountingOperation>> GetOperationsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IAccountingOperationService>()
            .GetAllAsync();
    }

    private async Task<bool> RetryOperationAsync(int operationId)
    {
        using var scope = _scopeFactory.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IAccountingOperationService>()
            .RetryAsync(operationId);
    }

    public sealed class AccountingOperationRow
    {
        public int Id { get; }
        public string ReferenceNumber { get; }
        public string OperationType { get; }
        public int ReferenceId { get; }
        public int RetryCount { get; }
        public AccountingOperationStatus Status { get; }
        public DateTime? LastAttemptDate { get; }
        public DateTime UpdatedDate { get; }
        public string? LastError { get; }

        public AccountingOperationRow(AccountingOperation source)
        {
            Id = source.Id;
            ReferenceNumber = source.ReferenceNumber;
            OperationType = source.OperationType;
            ReferenceId = source.ReferenceId;
            RetryCount = source.RetryCount;
            Status = source.Status;
            LastAttemptDate = source.LastAttemptDate;
            UpdatedDate = source.UpdatedDate;
            LastError = source.LastError;
        }

        public string StatusLabel => Status switch
        {
            AccountingOperationStatus.Pending => UiText.T("معلق", "Pending"),
            AccountingOperationStatus.Processing => UiText.T("قيد التنفيذ", "Processing"),
            AccountingOperationStatus.Completed => UiText.T("مكتمل", "Completed"),
            AccountingOperationStatus.Failed => UiText.T("فاشل", "Failed"),
            _ => Status.ToString()
        };
    }
}
