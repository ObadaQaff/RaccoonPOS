using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Windows;
using System.Windows.Input;

namespace RaccoonWarehouse
{
    public partial class UserStatementWindow : Window
    {
        private readonly UserStatementService _statementService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private int _userId;
        private bool _isInitialized;

        public UserStatementWindow(
            UserStatementService statementService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            _statementService = statementService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            OpenReferenceColumn.Header = UiText.T("فتح", "Open");
            Loaded += UserStatementWindow_Loaded;
        }

        public void Initialize(int userId)
        {
            _userId = userId;
            _isInitialized = true;
        }

        private void UserStatementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;

            if (_isInitialized)
                _ = GenerateAsync();
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            await GenerateAsync();
        }

        private async System.Threading.Tasks.Task GenerateAsync()
        {
            try
            {
                if (_userId <= 0)
                {
                    MessageBox.Show(UiText.T("لم يتم تحديد المستخدم.", "No user was selected."));
                    return;
                }

                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار الفترة.", "Please choose the period."));
                    return;
                }

                _loadingService.Show();
                var result = await _statementService.GetAsync(new UserStatementFilterDto
                {
                    UserId = _userId,
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1)
                });

                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل كشف الحساب.", "Failed to load the statement."));
                    return;
                }

                var report = result.Data;
                StatementGrid.ItemsSource = report.Rows;
                HeaderText.Text = $"{report.UserName} ({report.Role})";
                UserSummaryText.Text = report.UserName;
                OpeningText.Text = report.OpeningBalance.ToString("N2");
                DebitText.Text = report.TotalDebit.ToString("N2");
                CreditText.Text = report.TotalCredit.ToString("N2");
                ClosingText.Text = report.ClosingBalance.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("حدث خطأ أثناء تحميل كشف الحساب", "An error occurred while loading the statement")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void StatementGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StatementGrid.SelectedItem is not UserStatementRowDto row)
                return;

            await OpenRowReferenceAsync(row);
        }

        private async void OpenReference_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserStatementRowDto row)
                return;

            await OpenRowReferenceAsync(row);
        }

        private async System.Threading.Tasks.Task OpenRowReferenceAsync(UserStatementRowDto row)
        {
            if (string.IsNullOrWhiteSpace(row.ReferenceType) || !row.ReferenceId.HasValue)
            {
                MessageBox.Show(
                    UiText.T("لا يوجد مرجع مرتبط بهذه الحركة.", "No source document is linked to this row."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument(row.ReferenceType, row.ReferenceId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
