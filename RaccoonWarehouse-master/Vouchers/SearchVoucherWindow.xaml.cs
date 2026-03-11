using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System.Windows;

namespace RaccoonWarehouse.Vouchers
{
    public partial class SearchVoucherWindow : Window
    {
        private readonly IVoucherService _voucherService;
        private readonly ILoadingService _loadingService;

        public VoucherReadDto? Result { get; private set; }
        public List<CheckReadDto> Checks { get; set; }
        public UserReadDto? Customer { get; set; }
        private readonly bool _isSale =false;
        public SearchVoucherWindow(IVoucherService voucherService, bool Sale)
        {
            InitializeComponent();
            _voucherService = voucherService;
            _isSale = Sale;
            _loadingService = new LoadingService();
        }

        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string number = DocNumberTxt.Text.Trim();
                string customer = CustomerTxt.Text.Trim();

                DateTime? from = DateFrom.SelectedDate;
                DateTime? to = DateTo.SelectedDate;
                if (from.HasValue && to.HasValue && from > to)
                {
                    MessageBox.Show("تاريخ البداية يجب أن يكون قبل تاريخ النهاية.", "تنبيه");
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
                MessageBox.Show($"حدث خطأ أثناء البحث عن السند:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }
        }


        private void ResultsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectVoucher();
        }

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectVoucher();
        }

        private void SelectVoucher()
        {
            Result = ResultsGrid.SelectedItem as VoucherReadDto;
            if (Result == null)
            {
                MessageBox.Show("يرجى اختيار سند.");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
