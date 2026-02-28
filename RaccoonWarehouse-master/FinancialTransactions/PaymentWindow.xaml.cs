using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RaccoonWarehouse.FinancialTransactions
{
    public partial class PaymentWindow : Window
    {
        private readonly IFinancialTransactionService _service;
        private readonly DispatcherTimer _timer;
        private readonly int _cashierSessionId;
        private readonly int _cashierId;
        private readonly FinancialPostDto _dto;


        public PaymentWindow(IFinancialTransactionService service, int cashierSessionId, int cashierId)
        {
            InitializeComponent();

            _service = service;

            // 🔹 Init DTO

            _service = service;
            _cashierSessionId = cashierSessionId;
            _cashierId = cashierId;

            _dto = new FinancialPostDto
            {
                Direction = TransactionDirection.Out,            // ✅ دفع
                SourceType = FinancialSourceType.Manual,        // ✅ يدوي
                TransactionDate = DateTime.Now,
                CashierSessionId = _cashierSessionId,
                CashierId = _cashierId,
                Method = PaymentMethod.Cash,                    // افتراضي
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            };

            DataContext = _dto;
            LoadPaymentMethods();

            // ⏱ تحديث الوقت تلقائياً
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (_, _) =>
            {
/*                _dto.Date = DateTime.Now;
*/            };

            _timer.Start();

            // 🎯 Focus مباشرة على المبلغ
            Loaded += (_, _) => AmountTextBox.Focus();

        }
        private void LoadPaymentMethods()
        {
            PaymentMethodCombo.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        }
        // 🔢 Generate transaction number
        private string GenerateTransactionNumber()
        {
            return $"PAY-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        // 💾 Save
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

            var result = await _service.PostAsync(_dto);

            if (result.Success)
            {
                _timer.Stop();
                MessageBox.Show("تم تسجيل سند الدفع بنجاح ✅");
                DialogResult = true; // مهم عند ShowDialog
                Close();
            }
            else
            {
                MessageBox.Show(result.Message ?? "حدث خطأ أثناء الحفظ", "خطأ");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            DialogResult = false;
            Close();
        }
    }
}
