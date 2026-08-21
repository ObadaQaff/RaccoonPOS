using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RaccoonWarehouse.Accounting
{
    public partial class CreateJournalEntry : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;
        private readonly IUserService _userService;

        public ObservableCollection<AccountLookupItem> AccountsSource { get; } = new();
        public ObservableCollection<UserLookupItem> UsersSource { get; } = new();
        private ObservableCollection<JournalEntryLineEditorRow> Lines { get; } = new();
        private bool _isFilteringAccounts;
        private bool _isFilteringUsers;
        private bool _lookupsLoaded;

        public CreateJournalEntry(IAccountingService accountingService, IAccountingFeatureService featureService, ILoadingService loadingService, IUserService userService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            _userService = userService;
            InitializeComponent();
            UiText.ApplyWindow(this);

            EntryDatePicker.SelectedDate = DateTime.Today;
            Lines.CollectionChanged += (_, _) => UpdateTotals();
            LinesGrid.ItemsSource = Lines;
            AddLine();
            AddLine();
            Loaded += CreateJournalEntry_Loaded;
        }

        private async void CreateJournalEntry_Loaded(object sender, RoutedEventArgs e)
        {
            if (_lookupsLoaded)
                return;

            _lookupsLoaded = true;
            try
            {
                _loadingService.Show();
                await LoadAccountsAsync();
                await LoadUsersAsync();
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var result = await _userService.GetAllAsync();
                if (!result.Success)
                    return;

                UsersSource.Clear();
                foreach (var user in result.Data ?? new List<UserReadDto>())
                {
                    UsersSource.Add(new UserLookupItem
                    {
                        Id = user.Id,
                        DisplayLabel = user.Name ?? user.Id.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر تحميل المستخدمين", "Could not load users")}: {ex.Message}",
                    UiText.T("تنبيه", "Notice"));
            }
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
            }
        }

        private JournalEntryLineEditorRow AddLine()
        {
            var row = new JournalEntryLineEditorRow();
            row.PropertyChanged += Row_PropertyChanged;
            Lines.Add(row);
            UpdateTotals();
            return row;
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
            var row = AddLine();
            FocusFirstEditableCell(row);
        }

        private void RemoveLineBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = LinesGrid.SelectedItem as JournalEntryLineEditorRow
                ?? LinesGrid.CurrentItem as JournalEntryLineEditorRow;

            if (selected != null)
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
                    PartyUserId = x.PartyUserId,
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
                    _loadingService.Hide();
                MessageBox.Show(result.Message ?? UiText.T("فشل ترحيل قيد اليومية.", "Failed to post the journal entry."));
                    return;
                }

                _loadingService.Hide();
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
            }
        }

        private void AccountComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                combo.ItemsSource = AccountsSource;
                combo.IsDropDownOpen = false;
                if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox textBox)
                {
                    textBox.TextChanged -= AccountEditableTextBox_TextChanged;
                    textBox.TextChanged += AccountEditableTextBox_TextChanged;
                }
            }
        }

        private void AccountComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox combo)
            {
                RestoreAccountChoices(combo);
                combo.IsDropDownOpen = AccountsSource.Count > 0;
            }
        }

        private void AccountEditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringAccounts || sender is not TextBox textBox || textBox.TemplatedParent is not ComboBox combo)
                return;

            if (!textBox.IsKeyboardFocusWithin)
                return;

            var searchText = textBox.Text ?? string.Empty;
            FilterAccountChoices(combo, searchText);
        }

        private void AccountComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            if (e.Key == Key.Enter && combo.SelectedItem is AccountLookupItem selected)
            {
                SelectAccount(combo, selected);
                MoveToNextGridCellAfterCombo();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                RestoreAccountChoices(combo);
                combo.IsDropDownOpen = false;
            }
        }

        private void AccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isFilteringAccounts && sender is ComboBox combo && combo.SelectedItem is AccountLookupItem selected)
                SelectAccount(combo, selected);
        }

        private void AccountComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
                RestoreAccountChoices(combo);
        }

        private void PartyUserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isFilteringUsers && sender is ComboBox combo &&
                combo.SelectedItem is UserLookupItem user)
                SelectUser(combo, user);
        }

        private void PartyUserComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                combo.ItemsSource = UsersSource;
                combo.IsDropDownOpen = false;
                if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox textBox)
                {
                    textBox.TextChanged -= UserEditableTextBox_TextChanged;
                    textBox.TextChanged += UserEditableTextBox_TextChanged;
                }
            }
        }

        private void PartyUserComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox combo)
            {
                RestoreUserChoices(combo);
                combo.IsDropDownOpen = UsersSource.Count > 0;
            }
        }

        private void UserEditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringUsers || sender is not TextBox textBox || textBox.TemplatedParent is not ComboBox combo)
                return;

            if (!textBox.IsKeyboardFocusWithin)
                return;

            var searchText = textBox.Text ?? string.Empty;
            FilterUserChoices(combo, searchText);
        }

        private void PartyUserComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                Dispatcher.BeginInvoke(new Action(() => FilterUserChoices(combo, combo.Text)), DispatcherPriority.Input);
            }
        }

        private void PartyUserComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox combo && e.Key is Key.Back or Key.Delete)
                FilterUserChoices(combo, combo.Text);
        }

        private void PartyUserComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            if (combo.Template.FindName("PART_EditableTextBox", combo) is not TextBox textBox)
                return;

            if (e.Key == Key.Enter)
            {
                var selected = combo.SelectedItem as UserLookupItem
                    ?? combo.Items.OfType<UserLookupItem>().FirstOrDefault();

                if (selected != null)
                    SelectUser(combo, selected);
                else if (combo.DataContext is JournalEntryLineEditorRow row)
                {
                    row.PartyUserId = null;
                    row.PartyUserLabel = null;
                    combo.Text = string.Empty;
                    combo.IsDropDownOpen = false;
                    RestoreUserChoices(combo);
                }

                MoveToNextGridCellAfterCombo();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                RestoreUserChoices(combo);
                combo.IsDropDownOpen = false;
                e.Handled = true;
                return;
            }

            if (e.Key is not (Key.Up or Key.Down))
                return;

            var typedText = textBox.Text ?? string.Empty;
            if (!combo.IsDropDownOpen)
                combo.IsDropDownOpen = true;

            var nextIndex = combo.SelectedIndex;
            nextIndex = e.Key == Key.Down
                ? Math.Min(nextIndex + 1, combo.Items.Count - 1)
                : Math.Max(nextIndex - 1, 0);

            if (combo.Items.Count > 0)
            {
                _isFilteringUsers = true;
                try { combo.SelectedIndex = nextIndex; }
                finally { _isFilteringUsers = false; }

                textBox.Text = typedText;
                textBox.CaretIndex = textBox.Text.Length;
            }

            e.Handled = true;
        }

        private void PartyUserComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
                RestoreUserChoices(combo);
        }

        private void FilterUserChoices(ComboBox combo, string? searchText)
        {
            if (_isFilteringUsers)
                return;

            var query = searchText?.Trim() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(query)
                ? UsersSource.ToList()
                : UsersSource
                    .Where(user => user.DisplayLabel.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();

            _isFilteringUsers = true;
            try
            {
                combo.SelectedItem = null;
                combo.SelectedValue = null;
                combo.ItemsSource = filtered;
                combo.SelectedIndex = -1;
                combo.IsDropDownOpen = filtered.Count > 0;
                combo.Text = searchText ?? string.Empty;
                if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox textBox)
                    textBox.CaretIndex = textBox.Text.Length;
            }
            finally
            {
                _isFilteringUsers = false;
            }
        }

        private void RestoreUserChoices(ComboBox combo)
        {
            _isFilteringUsers = true;
            try { combo.ItemsSource = UsersSource; }
            finally { _isFilteringUsers = false; }
        }

        private void SelectUser(ComboBox combo, UserLookupItem user)
        {
            _isFilteringUsers = true;
            try
            {
                if (combo.DataContext is JournalEntryLineEditorRow row)
                {
                    row.PartyUserId = user.Id;
                    row.PartyUserLabel = user.DisplayLabel;
                }

                combo.ItemsSource = UsersSource;
                combo.SelectedValue = user.Id;
                combo.Text = user.DisplayLabel;
                combo.IsDropDownOpen = false;
            }
            finally
            {
                _isFilteringUsers = false;
            }
        }

        private void MoveToNextGridCellAfterCombo()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LinesGrid.CurrentItem is not JournalEntryLineEditorRow row || LinesGrid.CurrentCell.Column == null)
                    return;

                var columns = LinesGrid.Columns
                    .Where(column => column.Visibility == Visibility.Visible)
                    .OrderBy(column => column.DisplayIndex)
                    .ToList();
                var columnIndex = columns.IndexOf(LinesGrid.CurrentCell.Column);
                if (columnIndex < 0)
                    return;

                var nextRowIndex = Lines.IndexOf(row);
                var nextColumnIndex = columnIndex + 1;
                if (nextColumnIndex >= columns.Count)
                {
                    nextColumnIndex = 0;
                    nextRowIndex++;
                }

                if (nextRowIndex >= Lines.Count)
                    AddLine();

                LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                MoveGridToCell(Lines[nextRowIndex], columns[nextColumnIndex]);
            }), DispatcherPriority.Input);
        }

        private void LinesGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(FocusCurrentCellEditor), DispatcherPriority.Input);
        }

        private void LinesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Enter or Key.Up or Key.Down))
                return;

            if (LinesGrid.CurrentItem is not JournalEntryLineEditorRow row || LinesGrid.CurrentCell.Column == null)
                return;

            LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var columns = LinesGrid.Columns
                .Where(column => column.Visibility == Visibility.Visible)
                .OrderBy(column => column.DisplayIndex)
                .ToList();
            var columnIndex = columns.IndexOf(LinesGrid.CurrentCell.Column);
            if (columnIndex < 0)
                return;

            var rowIndex = Lines.IndexOf(row);
            var nextRowIndex = rowIndex;
            var nextColumnIndex = columnIndex;

            if (e.Key == Key.Enter)
                nextColumnIndex++;
            else if (e.Key == Key.Up)
                nextRowIndex--;
            else if (e.Key == Key.Down)
                nextRowIndex++;

            if (e.Key == Key.Enter && nextColumnIndex >= columns.Count)
            {
                nextColumnIndex = 0;
                nextRowIndex++;
            }

            if ((e.Key == Key.Up && nextRowIndex < 0) || (e.Key == Key.Down && nextRowIndex >= Lines.Count))
            {
                e.Handled = true;
                return;
            }

            if (nextRowIndex >= Lines.Count)
                AddLine();

            MoveGridToCell(Lines[nextRowIndex], columns[nextColumnIndex]);
            e.Handled = true;
        }

        private void MoveGridToCell(JournalEntryLineEditorRow row, DataGridColumn column)
        {
            LinesGrid.CurrentCell = new DataGridCellInfo(row, column);
            LinesGrid.ScrollIntoView(row, column);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LinesGrid.Focus();
                LinesGrid.BeginEdit();
                FocusCurrentCellEditor();
            }), DispatcherPriority.Input);
        }

        private void FocusCurrentCellEditor()
        {
            if (LinesGrid.CurrentCell.Column == null || LinesGrid.CurrentItem == null)
                return;

            var row = LinesGrid.ItemContainerGenerator.ContainerFromItem(LinesGrid.CurrentItem) as DataGridRow;
            if (row == null)
                return;

            var cell = FindVisualChild<DataGridCell>(row, candidate => candidate.Column == LinesGrid.CurrentCell.Column);
            if (cell == null)
                return;

            var combo = FindVisualChild<ComboBox>(cell);
            if (combo != null)
            {
                combo.Focus();
                if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox textBox)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                return;
            }

            var editor = FindVisualChild<TextBox>(cell);
            if (editor != null)
            {
                editor.Focus();
                editor.SelectAll();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed && (predicate == null || predicate(typed)))
                    return typed;

                var nested = FindVisualChild(child, predicate);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void FilterAccountChoices(ComboBox combo, string? searchText)
        {
            if (_isFilteringAccounts)
                return;

            var query = searchText?.Trim() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(query)
                ? AccountsSource.ToList()
                : AccountsSource.Where(account => account.DisplayLabel.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();

            _isFilteringAccounts = true;
            try
            {
                combo.SelectedItem = null;
                combo.SelectedValue = null;
                combo.ItemsSource = filtered;
                combo.IsDropDownOpen = filtered.Count > 0;
                combo.Text = searchText ?? string.Empty;
                if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox textBox)
                    textBox.CaretIndex = textBox.Text.Length;
            }
            finally
            {
                _isFilteringAccounts = false;
            }
        }

        private void RestoreAccountChoices(ComboBox combo)
        {
            _isFilteringAccounts = true;
            try { combo.ItemsSource = AccountsSource; }
            finally { _isFilteringAccounts = false; }
        }

        private void SelectAccount(ComboBox combo, AccountLookupItem account)
        {
            _isFilteringAccounts = true;
            try
            {
                if (combo.DataContext is JournalEntryLineEditorRow row)
                {
                    row.AccountId = account.Id;
                    row.AccountLabel = account.DisplayLabel;
                }

                combo.ItemsSource = AccountsSource;
                combo.SelectedValue = account.Id;
                combo.Text = account.DisplayLabel;
                combo.IsDropDownOpen = false;
            }
            finally
            {
                _isFilteringAccounts = false;
            }
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FocusFirstEditableCell(JournalEntryLineEditorRow row)
        {
            LinesGrid.CurrentCell = new DataGridCellInfo(row, LinesGrid.Columns[0]);
            LinesGrid.ScrollIntoView(row);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LinesGrid.Focus();
                LinesGrid.BeginEdit();
            }), DispatcherPriority.Background);
        }

        public class AccountLookupItem
        {
            public int Id { get; set; }
            public string DisplayLabel { get; set; } = string.Empty;
        }

        public class UserLookupItem
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
            private int? _partyUserId;

            public string? AccountLabel { get; set; }
            public string? PartyUserLabel { get; set; }

            public int? PartyUserId
            {
                get => _partyUserId;
                set
                {
                    _partyUserId = value;
                    OnPropertyChanged(nameof(PartyUserId));
                }
            }

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
