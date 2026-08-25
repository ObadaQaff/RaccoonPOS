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
        DeleteFailedText.Text = UiText.T("حذف الفاشل", "Delete failed");
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
        ApplyBatchText.Text = UiText.T("تنفيذ 15 عملية", "Apply 15 operations");
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

    private async void ApplyBatchBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _loadingService.Show();
            var result = await _operationProcessor.ProcessPendingBatchAsync(15);
            await LoadAsync();
            MessageBox.Show(
                UiText.T(
                    $"تم تنفيذ {result.Processed} عملية. الفاشلة: {result.Failed}.",
                    $"Processed {result.Processed} operations. Failed: {result.Failed}."),
                UiText.T("عمليات المحاسبة", "Accounting Operations"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiText.T("تعذر تنفيذ العمليات", "Could not apply accounting operations"), MessageBoxButton.OK, MessageBoxImage.Error);
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
        try
        {
            _loadingService.Show();
            using var scope = _scopeFactory.CreateScope();
            var retriedCount = await scope.ServiceProvider
                .GetRequiredService<IAccountingOperationService>()
                .RetryFailedAsync();

            await LoadAsync();
            MessageBox.Show(
                retriedCount > 0
                    ? UiText.T($"تمت إعادة {retriedCount} عملية فاشلة إلى قائمة الانتظار.", $"{retriedCount} failed operations were queued for retry.")
                    : UiText.T("لا توجد عمليات فاشلة لإعادة المحاولة.", "There are no failed operations to retry."),
                UiText.T("إعادة المحاولة", "Retry"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiText.T("تعذر إعادة المحاولة", "Could not retry failed operations"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    private async void DeleteFailedBtn_Click(object sender, RoutedEventArgs e)
    {
        if (OperationsGrid.SelectedItem is not AccountingOperationRow selected)
        {
            MessageBox.Show(
                UiText.T("يرجى اختيار عملية فاشلة أولاً.", "Please select a failed operation first."),
                UiText.T("تنبيه", "Notice"));
            return;
        }

        if (selected.Status != AccountingOperationStatus.Failed)
        {
            MessageBox.Show(
                UiText.T("يمكن حذف العمليات الفاشلة فقط.", "Only failed operations can be deleted."),
                UiText.T("تنبيه", "Notice"));
            return;
        }

        if (MessageBox.Show(
                UiText.T($"هل تريد حذف العملية الفاشلة #{selected.Id}؟", $"Delete failed operation #{selected.Id}?"),
                UiText.T("تأكيد", "Confirm"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            _loadingService.Show();
            using var scope = _scopeFactory.CreateScope();
            var deleted = await scope.ServiceProvider
                .GetRequiredService<IAccountingOperationService>()
                .DeleteFailedAsync(selected.Id);

            await LoadAsync();
            _loadingService.Hide();
            MessageBox.Show(
                deleted
                    ? UiText.T("تم حذف العملية الفاشلة.", "The failed operation was deleted.")
                    : UiText.T("تعذر حذف العملية؛ ربما تغيرت حالتها.", "The operation could not be deleted; its status may have changed."),
                UiText.T("عمليات المحاسبة", "Accounting Operations"));
        }
        catch (Exception ex)
        {
            _loadingService.Hide();
            MessageBox.Show(ex.Message, UiText.T("تعذر حذف العملية", "Could not delete operation"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadingService.Hide();
        }
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

        public string OperationTypeLabel => AccountingTextLocalizer.ToArabic(OperationType);
        public string ReferenceLabel => $"مرجع #{ReferenceId}";
    }
}
