using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace RaccoonWarehouse.FinancialTransactions
{
    public partial class PaymentWindow : Window, INotifyPropertyChanged
    {
        private readonly IFinancialTransactionService _service;
        private readonly DispatcherTimer _timer;
        private readonly int _cashierSessionId;
        private readonly int _cashierId;
        private readonly FinancialPostDto _dto;
        private readonly string _transactionNumber;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string TransactionNumber => _transactionNumber;
        public DateTime TransactionDate => _dto.TransactionDate;

        public PaymentWindow(IFinancialTransactionService service, int cashierSessionId, int cashierId)
        {
            _service = service;
            _cashierSessionId = cashierSessionId;
            _cashierId = cashierId;
            _transactionNumber = GenerateTransactionNumber();

            _dto = new FinancialPostDto
            {
                Direction = TransactionDirection.Out,
                SourceType = FinancialSourceType.Manual,
                TransactionDate = DateTime.Now,
                CashierSessionId = _cashierSessionId,
                CashierId = _cashierId,
                Method = PaymentMethod.Cash,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            };

            InitializeComponent();

            DataContext = this;
            LoadPaymentMethods();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (_, _) =>
            {
                _dto.TransactionDate = DateTime.Now;
                OnPropertyChanged(nameof(TransactionDate));
            };

            _timer.Start();
            Loaded += (_, _) => AmountTextBox.Focus();
        }

        private void LoadPaymentMethods()
        {
            PaymentMethodCombo.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
            PaymentMethodCombo.SelectedItem = _dto.Method;
        }

        private string GenerateTransactionNumber()
        {
            return $"PAY-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح");
                    return;
                }

                if (PaymentMethodCombo.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار طريقة الدفع");
                    return;
                }

                _dto.Amount = amount;
                _dto.Method = (PaymentMethod)PaymentMethodCombo.SelectedItem;
                _dto.TransactionDate = DateTime.Now;
                _dto.UpdatedDate = DateTime.Now;
                OnPropertyChanged(nameof(TransactionDate));

                var result = await _service.PostAsync(_dto);

                if (result.Success)
                {
                    _timer.Stop();
                    MessageBox.Show("تم تسجيل سند الدفع بنجاح ✅");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(result.Message ?? "حدث خطأ أثناء الحفظ", "خطأ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تسجيل سند الدفع: {ex.Message}", "خطأ");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            DialogResult = false;
            Close();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
