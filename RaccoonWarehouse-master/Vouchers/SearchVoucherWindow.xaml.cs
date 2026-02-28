using RaccoonWarehouse.Application.Service.Vouchers;
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

        public VoucherReadDto? Result { get; private set; }
        public List<CheckReadDto> Checks { get; set; }
        public UserReadDto? Customer { get; set; }
        private readonly bool _isSale =false;
        public SearchVoucherWindow(IVoucherService voucherService, bool Sale)
        {
            InitializeComponent();
            _voucherService = voucherService;
            _isSale = Sale;
        }

        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            string number = DocNumberTxt.Text.Trim();
            string customer = CustomerTxt.Text.Trim();

            DateTime? from = DateFrom.SelectedDate;
            DateTime? to = DateTo.SelectedDate;

            var results= new List<VoucherReadDto>();
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
            );}

            ResultsGrid.ItemsSource = results;
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
