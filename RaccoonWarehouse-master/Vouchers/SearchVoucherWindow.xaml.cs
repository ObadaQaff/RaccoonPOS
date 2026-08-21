using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RaccoonWarehouse.Vouchers
{
    public partial class SearchVoucherWindow : Window
    {
        private readonly IVoucherService _voucherService;
        private List<UserReadDto> _allUsers = new();
        private bool _isFilteringUsers;
        private bool _isNavigatingUserChoices;
        private readonly ILoadingService _loadingService;
        private bool _isBusy;

        public VoucherReadDto? Result { get; private set; }
        public List<CheckReadDto> Checks { get; set; }
        public UserReadDto? Customer { get; set; }
        private readonly bool _isSale =false;
        public SearchVoucherWindow(IVoucherService voucherService, IEnumerable<UserReadDto> users, bool Sale)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _voucherService = voucherService;
            _allUsers = users?.GroupBy(x => x.Id).Select(x => x.First()).ToList() ?? new();
            CustomerTxt.ItemsSource = _allUsers;

            _isSale = Sale;
            _loadingService = new LoadingService();
        }
        private void CustomerTxt_Loaded(object sender, RoutedEventArgs e)
        {
            CustomerTxt.DisplayMemberPath = "Name";
            CustomerTxt.SelectedValuePath = "Id";
        }

        private void CustomerTxt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            CustomerTxt.SelectedItem = null;
            Dispatcher.BeginInvoke(new Action(() => FilterUserList(CustomerTxt.Text)), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CustomerTxt_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                CustomerTxt.SelectedItem = null;
                FilterUserList(CustomerTxt.Text);
            }
        }

        private void FilterUserList(string text)
        {
            var filtered = _allUsers
                .Where(user => !string.IsNullOrEmpty(user.Name) && user.Name.Contains(text ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .ToList();

            _isFilteringUsers = true;
            try
            {
                CustomerTxt.ItemsSource = filtered;
                CustomerTxt.SelectedItem = null;
                CustomerTxt.SelectedIndex = -1;
                CustomerTxt.Text = text ?? string.Empty;
                CustomerTxt.IsDropDownOpen = filtered.Count > 0;
            }
            finally { _isFilteringUsers = false; }
        }
        private void CustomerTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Down or Key.Up or Key.Enter))
                return;

            if (CustomerTxt.Template.FindName("PART_EditableTextBox", CustomerTxt) is not TextBox textBox)
                return;

            if (e.Key == Key.Enter)
            {
                if (CustomerTxt.SelectedItem is UserReadDto selected)
                {
                    _isNavigatingUserChoices = true;
                    try { textBox.Text = selected.Name; }
                    finally { _isNavigatingUserChoices = false; }
                    textBox.CaretIndex = textBox.Text.Length;
                }

                CustomerTxt.IsDropDownOpen = false;
                e.Handled = true;
                return;
            }

            var typedText = textBox.Text ?? string.Empty;
            if (!CustomerTxt.IsDropDownOpen)
                CustomerTxt.IsDropDownOpen = true;

            var nextIndex = CustomerTxt.SelectedIndex;
            nextIndex = e.Key == Key.Down
                ? Math.Min(nextIndex + 1, CustomerTxt.Items.Count - 1)
                : Math.Max(nextIndex - 1, 0);

            if (CustomerTxt.Items.Count > 0)
            {
                _isNavigatingUserChoices = true;
                try { CustomerTxt.SelectedIndex = nextIndex; }
                finally { _isNavigatingUserChoices = false; }
                textBox.Text = typedText;
                textBox.CaretIndex = textBox.Text.Length;
            }

            e.Handled = true;
        }
        private void CustomerTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringUsers || _isNavigatingUserChoices || CustomerTxt.Template.FindName("PART_EditableTextBox", CustomerTxt) is not TextBox textBox)
                return;

            var typedText = textBox.Text ?? string.Empty;
            var filterText = typedText.Trim();
            _isFilteringUsers = true;
            try
            {
                CustomerTxt.SelectedItem = null;
                CustomerTxt.ItemsSource = string.IsNullOrWhiteSpace(filterText)
                    ? _allUsers.ToList()
                    : _allUsers.Where(x => x.Name.Contains(filterText, StringComparison.CurrentCultureIgnoreCase)).ToList();
                CustomerTxt.IsDropDownOpen = true;
                textBox.Text = typedText;
                textBox.CaretIndex = textBox.Text.Length;
            }
            finally { _isFilteringUsers = false; }
        }
        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy)
                return;

            _isBusy = true;
            try
            {
                string number = DocNumberTxt.Text.Trim();
                string customer = CustomerTxt.Text.Trim();

                DateTime? from = DateFrom.SelectedDate;
                DateTime? to = DateTo.SelectedDate;
                if (from.HasValue && to.HasValue && from > to)
                {
                    MessageBox.Show(UiText.T("تاريخ البداية يجب أن يكون قبل تاريخ النهاية.", "The start date must be before the end date."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                _loadingService.Show();
                var results = new List<VoucherReadDto>();
                if (_isSale)
                {
                    results = await _voucherService.SearchVouchersAsync(
                    voucherNumber: number,
                    customerName: customer,
                    dateFrom: from,
                    dateTo: to,
                    paymentType: null,
                    type: VoucherType.Receipt
                    );
                }
                else
                {
                    results = await _voucherService.SearchVouchersAsync(
                    voucherNumber: number,
                    customerName: customer,
                    dateFrom: from,
                    dateTo: to,
                    paymentType: null,
                    type: VoucherType.Payment
                );
                }

                ResultsGrid.ItemsSource = results;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء البحث عن السند", "An error occurred while searching for the voucher")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
                _isBusy = false;
            }
        }


        private async void ResultsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await SelectVoucherAsync();
        }

        private async void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            await SelectVoucherAsync();
        }

        private async Task SelectVoucherAsync()
        {
            if (_isBusy)
                return;

            Result = ResultsGrid.SelectedItem as VoucherReadDto;
            if (Result == null)
            {
                MessageBox.Show(UiText.T("يرجى اختيار سند.", "Please select a voucher."));
                return;
            }

            _isBusy = true;
            _loadingService.Show();
            try
            {
                await Task.Yield();
            }
            finally
            {
                _loadingService.Hide();
                _isBusy = false;
            }

            DialogResult = true;
            Close();
        }
    }
}
