using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Accounting
{
    public partial class JournalEntriesBrowser : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private int? _initialEntryId;

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

        public void OpenForEntry(int entryId)
        {
            _initialEntryId = entryId;
        }

        private async void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadEntriesAsync();
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await LoadEntriesAsync();
                return;
            }

            if (e.Key == Key.Down && Entries.Count > 0)
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    JournalEntriesGrid.Focus();
                    JournalEntriesGrid.SelectedItem = Entries[0];
                    JournalEntriesGrid.CurrentCell = new DataGridCellInfo(Entries[0], JournalEntriesGrid.Columns[0]);
                    JournalEntriesGrid.ScrollIntoView(Entries[0]);
                    JournalEntriesGrid.UpdateLayout();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
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
                var initialEntryId = _initialEntryId;
                var result = await _accountingService.GetJournalEntriesAsync(new JournalEntryFilterDto
                {
                    EntryId = initialEntryId,
                    From = initialEntryId.HasValue ? null : FromDatePicker.SelectedDate?.Date,
                    To = initialEntryId.HasValue ? null : ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1),
                    Status = ParseStatus(StatusComboBox.SelectedItem as ComboBoxItem),
                    ReferenceSearch = string.IsNullOrWhiteSpace(ReferenceSearchTextBox.Text) ? null : ReferenceSearchTextBox.Text.Trim(),
                    AccountSearch = string.IsNullOrWhiteSpace(AccountSearchTextBox.Text) ? null : AccountSearchTextBox.Text.Trim()
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل سجل القيود.", "Failed to load journal entries."));
                    return;
                }

                Entries.Clear();
                foreach (var entry in result.Data)
                    Entries.Add(new JournalEntryListItem(entry));

                if (_initialEntryId.HasValue)
                {
                    var initialEntry = Entries.FirstOrDefault(x => x.Id == _initialEntryId.Value);
                    _initialEntryId = null;

                    if (initialEntry != null)
                    {
                        JournalEntriesGrid.SelectedItem = initialEntry;
                        ShowEntryDetailsForSelectedEntry();
                    }
                }

                ResultCountText.Text = string.Format(
                    UiText.T("عدد النتائج: {0}", "Results: {0}"),
                    Entries.Count);

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
                ShowEntryDetailsButton.IsEnabled = false;
                EntryDetailsBorder.Visibility = Visibility.Collapsed;
                SelectionSummaryText.Text = UiText.T("اختر قيداً لعرض تفاصيله", "Select an entry to view its details");
                SelectedDebitText.Text = "0.00";
                SelectedCreditText.Text = "0.00";
                JournalLinesGrid.ItemsSource = null;
                return;
            }

            SelectionSummaryText.Text = $"{selected.EntryNumber} | {selected.Description}";
            ShowEntryDetailsButton.IsEnabled = true;
            EntryDetailsBorder.Visibility = Visibility.Collapsed;
            SelectedDebitText.Text = selected.TotalDebit.ToString("N2");
            SelectedCreditText.Text = selected.TotalCredit.ToString("N2");
            JournalLinesGrid.ItemsSource = selected.Lines;
        }

        private void ShowEntryDetailsBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowEntryDetailsForSelectedEntry();
        }

        private void ShowEntryDetailsForSelectedEntry()
        {
            if (JournalEntriesGrid.SelectedItem is not JournalEntryListItem selected)
                return;

            EntryCreatedDateText.Text = selected.CreatedDate.ToString("yyyy-MM-dd HH:mm");
            EntryPaymentMethodText.Text = selected.PaymentDetails.Count == 0
                ? UiText.T("غير متوفر", "Not available")
                : string.Join("، ", selected.PaymentDetails
                    .Select(x => x.PaymentType)
                    .Distinct()
                    .Select(GetPaymentTypeLabel));
            EntryPaymentBreakdownText.Text = selected.PaymentDetails.Count == 0
                ? UiText.T("لا يوجد دفع مقسّم", "No split payment")
                : string.Join("\n", selected.PaymentDetails.Select(payment =>
                    $"{GetPaymentTypeLabel(payment.PaymentType)}: {payment.Amount:N2}"));
            EntryTaxText.Text = selected.TotalTax.HasValue
                ? selected.TotalTax.Value.ToString("N2")
                : UiText.T("غير منطبق", "Not applicable");
            EntryTotalsText.Text = string.Join("\n", new[]
            {
                $"{UiText.T("قبل الضريبة", "Before tax")}: {selected.Subtotal?.ToString("N2") ?? "-"}",
                $"{UiText.T("الإجمالي النهائي", "Final total")}: {selected.FinalTotal?.ToString("N2") ?? "-"}"
            });
            EntryCheckAndNotesText.Text = string.Join("\n", new[]
            {
                selected.CheckDetails,
                string.IsNullOrWhiteSpace(selected.Notes) ? null : $"{UiText.T("ملاحظات", "Notes")}: {selected.Notes}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
            EntryCreatedByText.Text = string.IsNullOrWhiteSpace(selected.CreatedByName)
                ? string.Empty
                : $"{UiText.T("أنشأه", "Created by")}: {selected.CreatedByName}";
            EntryDetailsBorder.Visibility = Visibility.Visible;
        }

        private static string GetPaymentTypeLabel(PaymentType paymentType) => paymentType switch
        {
            PaymentType.Cash => UiText.T("نقدي", "Cash"),
            PaymentType.Visa => UiText.T("بطاقة فيزا", "Visa"),
            PaymentType.Master => UiText.T("بطاقة ماستر", "Mastercard"),
            PaymentType.Debit => UiText.T("تحويل بنكي", "Bank transfer"),
            PaymentType.Check => UiText.T("شيك", "Check"),
            PaymentType.MobilePayment => UiText.T("دفع إلكتروني", "Mobile payment"),
            PaymentType.Credit => UiText.T("آجل", "Credit"),
            _ => paymentType.ToString()
        };

        public void SetReferenceFilter(string reference)
        {
            ReferenceSearchTextBox.Text = reference ?? string.Empty;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void JournalEntriesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (JournalEntriesGrid.SelectedItem is not JournalEntryListItem entry ||
                string.IsNullOrWhiteSpace(entry.ReferenceType) ||
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
                    UiText.T("تعذر فتح المستند المصدر", "Could not open the source document"),
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
                Description = AccountingTextLocalizer.ToArabic(source.Description);
                Status = source.Status;
                ReferenceType = source.ReferenceType;
                ReferenceId = source.ReferenceId;
                CreatedBy = source.CreatedBy;
                TotalDebit = source.TotalDebit;
                TotalCredit = source.TotalCredit;
                PaymentDetails = source.PaymentDetails;
                Subtotal = source.Subtotal;
                TotalTax = source.TotalTax;
                FinalTotal = source.FinalTotal;
                CheckDetails = source.CheckDetails;
                Notes = source.Notes;
                CreatedByName = source.CreatedByName;
                foreach (var line in source.Lines)
                    line.Description = AccountingTextLocalizer.ToArabic(line.Description);
                Lines = source.Lines;
                CreatedDate = source.CreatedDate;
                UpdatedDate = source.UpdatedDate;
            }

            public string ReferenceLabel => AccountingTextLocalizer.ReferenceLabel(ReferenceType, ReferenceId);

            public string StatusLabel => Status switch
            {
                JournalEntryStatus.Posted => UiText.T("مرحّل", "Posted"),
                JournalEntryStatus.Reversed => UiText.T("معكوس", "Reversed"),
                _ => UiText.T("مسودة", "Draft")
            };
        }
    }
}
