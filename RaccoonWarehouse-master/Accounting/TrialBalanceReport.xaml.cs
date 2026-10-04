using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaccoonWarehouse.Accounting
{
    public partial class TrialBalanceReport : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

        public TrialBalanceReport(
            IAccountingService accountingService,
            IAccountingFeatureService featureService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += TrialBalanceReport_Loaded;
        }

        private void TrialBalanceReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await _featureService.IsEnabledAsync())
                {
                    MessageBox.Show(UiText.T("نظام المحاسبة متوقف حالياً.", "The accounting system is currently disabled."));
                    Close();
                    return;
                }

                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار الفترة.", "Please choose the period."));
                    return;
                }

                _loadingService.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                var result = await _accountingService.GetTrialBalanceAsync(new TrialBalanceFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1),
                    IncludeZeroBalances = IncludeZeroCheckBox.IsChecked == true
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل ميزان المراجعة.", "Failed to load the trial balance."));
                    return;
                }

                TrialBalanceTree.ItemsSource = BuildAccountTree(result.Data.rows);
                TotalDebitText.Text = result.Data.summary.TotalClosingDebit.ToString("N2");
                TotalCreditText.Text = result.Data.summary.TotalClosingCredit.ToString("N2");
                BalancedText.Text = result.Data.summary.IsBalanced ? UiText.T("متوازن", "Balanced") : UiText.T("غير متوازن", "Unbalanced");
                TrialBalanceTree.UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل ميزان المراجعة", "An error occurred while loading the trial balance")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TrialBalancePageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer || scrollViewer.ScrollableHeight <= 0)
                return;

            var targetOffset = Math.Clamp(
                scrollViewer.VerticalOffset - e.Delta,
                0,
                scrollViewer.ScrollableHeight);

            if (targetOffset == scrollViewer.VerticalOffset)
                return;

            scrollViewer.ScrollToVerticalOffset(targetOffset);
            e.Handled = true;
        }

        private void TrialBalanceTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TrialBalanceTree.SelectedItem is not TrialBalanceRowDto row)
                return;

            try
            {
                var report = new GeneralLedgerReport(
                    _accountingService,
                    _featureService,
                    _loadingService,
                    _sourceDocumentNavigationService);
                report.OpenForAccount(row.AccountId);
                report.Owner = this;
                report.Show();
                report.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر فتح كشف الحساب", "Failed to open the account statement")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
        }

        private static List<TrialBalanceRowDto> BuildAccountTree(IEnumerable<TrialBalanceRowDto> rows)
        {
            var rowList = rows.OrderBy(row => row.AccountCode).ToList();
            var rowsById = rowList.ToDictionary(row => row.AccountId);

            foreach (var row in rowList)
                row.Children.Clear();

            var roots = new List<TrialBalanceRowDto>();
            foreach (var row in rowList)
            {
                if (row.ParentAccountId.HasValue && rowsById.TryGetValue(row.ParentAccountId.Value, out var parent))
                    parent.Children.Add(row);
                else
                    roots.Add(row);
            }

            foreach (var row in rowList)
                row.Children.Sort((left, right) => string.Compare(left.AccountCode, right.AccountCode, StringComparison.Ordinal));

            return roots;
        }
    }
}
