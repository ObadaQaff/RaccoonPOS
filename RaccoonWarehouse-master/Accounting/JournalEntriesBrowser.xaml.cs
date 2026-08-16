using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Accounting
{
    public partial class JournalEntriesBrowser : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

        public ObservableCollection<JournalEntryListItem> Entries { get; } = new();

        public JournalEntriesBrowser(
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
            JournalEntriesGrid.ItemsSource = Entries;
            Loaded += JournalEntriesBrowser_Loaded;
        }

        private async void JournalEntriesBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;
            await LoadEntriesAsync();
        }

        private async void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadEntriesAsync();
        }

        private async Task LoadEntriesAsync()
        {
            try
            {
                if (!await _featureService.IsEnabledAsync())
                {
                    MessageBox.Show(UiText.T("نظام المحاسبة متوقف حالياً.", "The accounting system is currently disabled."));
                    Close();
                    return;
                }

                _loadingService.Show();
                var result = await _accountingService.GetJournalEntriesAsync(new JournalEntryFilterDto
                {
                    From = FromDatePicker.SelectedDate?.Date,
                    To = ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1),
                    Status = ParseStatus(StatusComboBox.SelectedItem as ComboBoxItem),
                    ReferenceType = string.IsNullOrWhiteSpace(ReferenceTypeTextBox.Text) ? null : ReferenceTypeTextBox.Text.Trim()
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل سجل القيود.", "Failed to load journal entries."));
                    return;
                }

                Entries.Clear();
                foreach (var entry in result.Data)
                    Entries.Add(new JournalEntryListItem(entry));

                if (Entries.Count == 0)
                {
                    SelectionSummaryText.Text = UiText.T("لا توجد قيود ضمن المرشحات الحالية.", "No entries match the current filters.");
                    SelectedDebitText.Text = "0.00";
                    SelectedCreditText.Text = "0.00";
                    JournalLinesGrid.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل سجل القيود", "An error occurred while loading journal entries")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void ReverseSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (JournalEntriesGrid.SelectedItem is not JournalEntryListItem selected)
            {
                MessageBox.Show(UiText.T("يرجى اختيار قيد أولاً.", "Please select an entry first."));
                return;
            }

            var reason = ReverseReasonTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(UiText.T("يرجى كتابة سبب عكس القيد.", "Please enter a reason for reversing the entry."));
                return;
            }

            var confirmation = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Entry {selected.EntryNumber} will be reversed. Do you want to continue?"
                    : $"سيتم عكس القيد رقم {selected.EntryNumber}. هل تريد المتابعة؟",
                UiText.T("تأكيد عكس القيد", "Confirm Entry Reversal"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                _loadingService.Show();
                var result = await _accountingService.ReverseJournalEntryAsync(selected.Id, reason);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر عكس القيد.", "Failed to reverse the entry."));
                    return;
                }

                ReverseReasonTextBox.Text = string.Empty;
                await LoadEntriesAsync();
                MessageBox.Show(
                    UiText.IsEnglish
                        ? $"Reversing entry {result.Data.EntryNumber} was created successfully."
                        : $"تم إنشاء القيد العكسي رقم {result.Data.EntryNumber} بنجاح.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء عكس القيد", "An error occurred while reversing the entry")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void JournalEntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (JournalEntriesGrid.SelectedItem is not JournalEntryListItem selected)
            {
                SelectionSummaryText.Text = UiText.T("اختر قيداً لعرض تفاصيله", "Select an entry to view its details");
                SelectedDebitText.Text = "0.00";
                SelectedCreditText.Text = "0.00";
                JournalLinesGrid.ItemsSource = null;
                return;
            }

            SelectionSummaryText.Text = $"{selected.EntryNumber} | {selected.Description}";
            SelectedDebitText.Text = selected.TotalDebit.ToString("N2");
            SelectedCreditText.Text = selected.TotalCredit.ToString("N2");
            JournalLinesGrid.ItemsSource = selected.Lines;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void JournalEntriesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (JournalEntriesGrid.SelectedItem is not JournalEntryListItem entry ||
                !string.Equals(entry.ReferenceType, "Invoice", StringComparison.OrdinalIgnoreCase) ||
                !entry.ReferenceId.HasValue || entry.ReferenceId.Value <= 0)
            {
                return;
            }

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument(entry.ReferenceType, entry.ReferenceId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    UiText.T("تعذر فتح الفاتورة", "Could not open invoice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static JournalEntryStatus? ParseStatus(ComboBoxItem? item)
        {
            return item?.Tag?.ToString() switch
            {
                "Posted" => JournalEntryStatus.Posted,
                "Reversed" => JournalEntryStatus.Reversed,
                "Draft" => JournalEntryStatus.Draft,
                _ => null
            };
        }

        public class JournalEntryListItem : JournalEntryReadDto
        {
            public JournalEntryListItem(JournalEntryReadDto source)
            {
                Id = source.Id;
                EntryNumber = source.EntryNumber;
                EntryDate = source.EntryDate;
                Description = source.Description;
                Status = source.Status;
                ReferenceType = source.ReferenceType;
                ReferenceId = source.ReferenceId;
                TotalDebit = source.TotalDebit;
                TotalCredit = source.TotalCredit;
                Lines = source.Lines;
                CreatedDate = source.CreatedDate;
                UpdatedDate = source.UpdatedDate;
            }

            public string StatusLabel => Status switch
            {
                JournalEntryStatus.Posted => UiText.T("مرحّل", "Posted"),
                JournalEntryStatus.Reversed => UiText.T("معكوس", "Reversed"),
                _ => UiText.T("مسودة", "Draft")
            };
        }
    }
}
