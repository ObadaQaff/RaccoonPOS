using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class CreateJournalEntry : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;

        public ObservableCollection<AccountLookupItem> AccountsSource { get; } = new();
        private ObservableCollection<JournalEntryLineEditorRow> Lines { get; } = new();

        public CreateJournalEntry(IAccountingService accountingService, IAccountingFeatureService featureService, ILoadingService loadingService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);

            EntryDatePicker.SelectedDate = DateTime.Today;
            Lines.CollectionChanged += (_, _) => UpdateTotals();
            LinesGrid.ItemsSource = Lines;
            AddLine();
            AddLine();
            _ = LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
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
                var result = await _accountingService.GetAccountsAsync(activeOnly: true);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الحسابات.", "Failed to load accounts."));
                    return;
                }

                AccountsSource.Clear();
                foreach (var account in result.Data.Where(x => x.IsPosting))
                {
                    AccountsSource.Add(new AccountLookupItem
                    {
                        Id = account.Id,
                        DisplayLabel = $"{account.Code} - {account.Name}"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء تحميل الحسابات", "An unexpected error occurred while loading accounts")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void AddLine()
        {
            var row = new JournalEntryLineEditorRow();
            row.PropertyChanged += Row_PropertyChanged;
            Lines.Add(row);
            UpdateTotals();
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            var debit = Lines.Sum(x => x.Debit);
            var credit = Lines.Sum(x => x.Credit);
            TotalsTextBlock.Text = UiText.IsEnglish
                ? $"Debit: {debit:N2}    Credit: {credit:N2}"
                : $"مدين: {debit:N2}    دائن: {credit:N2}";
        }

        private void AddLineBtn_Click(object sender, RoutedEventArgs e)
        {
            AddLine();
        }

        private void RemoveLineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LinesGrid.SelectedItem is JournalEntryLineEditorRow selected)
            {
                selected.PropertyChanged -= Row_PropertyChanged;
                Lines.Remove(selected);
                UpdateTotals();
            }
        }

        private async void PostBtn_Click(object sender, RoutedEventArgs e)
        {
            var dto = new JournalEntryWriteDto
            {
                Description = DescriptionTextBox.Text?.Trim() ?? string.Empty,
                EntryDate = EntryDatePicker.SelectedDate ?? DateTime.Today,
                Lines = Lines.Select(x => new JournalEntryLineWriteDto
                {
                    AccountId = x.AccountId,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    Description = x.Description
                }).ToList()
            };

            try
            {
                _loadingService.Show();
                var result = await _accountingService.PostJournalEntryAsync(dto);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل ترحيل قيد اليومية.", "Failed to post the journal entry."));
                    return;
                }

                MessageBox.Show(
                    UiText.IsEnglish
                        ? $"Journal entry posted successfully.\nEntry Number: {result.Data.EntryNumber}"
                        : $"تم ترحيل القيد بنجاح.\nرقم القيد: {result.Data.EntryNumber}");
                DescriptionTextBox.Text = string.Empty;
                EntryDatePicker.SelectedDate = DateTime.Today;
                Lines.Clear();
                AddLine();
                AddLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء ترحيل القيد", "An unexpected error occurred while posting the entry")}: {ex.Message}");
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

        public class AccountLookupItem
        {
            public int Id { get; set; }
            public string DisplayLabel { get; set; } = string.Empty;
        }

        public class JournalEntryLineEditorRow : INotifyPropertyChanged
        {
            private int _accountId;
            private decimal _debit;
            private decimal _credit;
            private string? _description;

            public int AccountId
            {
                get => _accountId;
                set
                {
                    _accountId = value;
                    OnPropertyChanged(nameof(AccountId));
                }
            }

            public decimal Debit
            {
                get => _debit;
                set
                {
                    _debit = value;
                    OnPropertyChanged(nameof(Debit));
                }
            }

            public decimal Credit
            {
                get => _credit;
                set
                {
                    _credit = value;
                    OnPropertyChanged(nameof(Credit));
                }
            }

            public string? Description
            {
                get => _description;
                set
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
