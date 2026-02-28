using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RaccoonWarehouse.FinancialTransactions
{
    public partial class ReceiptWindow : Window
    {
        private readonly IFinancialTransactionService _service;
        private readonly FinancialPostDto _dto;
        private DispatcherTimer _timer;

        private readonly int _cashierSessionId;
        private readonly int _cashierId;

        public ReceiptWindow(IFinancialTransactionService service, int cashierSessionId, int cashierId)
        {
            InitializeComponent();

            _service = service;
            _cashierSessionId = cashierSessionId;
            _cashierId = cashierId;

            _dto = new FinancialPostDto
            {
                Direction = TransactionDirection.In,            // ✅ قبض
                SourceType = FinancialSourceType.Manual,        // ✅ يدوي
                TransactionDate = DateTime.Now,
                CashierSessionId = _cashierSessionId,
                CashierId = _cashierId,
                Method = PaymentMethod.Cash,                    // افتراضي
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            DataContext = _dto;

            LoadPaymentMethods();
            StartClock();
        }

        private void LoadPaymentMethods()
        {
            PaymentMethodCombo.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        }

        private void StartClock()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                _dto.TransactionDate = DateTime.Now;
                // ما في داعي تعمل DataContext = null; خليها بسيطة
            };
            _timer.Start();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح");
                return;
            }

            if (PaymentMethodCombo.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار طريقة القبض");
                return;
            }

            _dto.Amount = amount;
            _dto.Method = (PaymentMethod)PaymentMethodCombo.SelectedItem;
            _dto.UpdatedDate = DateTime.Now;

            // ✅ Post (مش CreateAsync)
            var result = await _service.PostAsync(_dto);

            if (result.Success)
            {
                _timer.Stop();
                MessageBox.Show("تم تسجيل سند القبض بنجاح ✅");
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(result.Message ?? "حدث خطأ أثناء الحفظ");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            DialogResult = false;
            Close();
        }
    }
}
