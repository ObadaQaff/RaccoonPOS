using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Invoices
{
    public partial class CheckDetailsWindow : Window
    {
        public ObservableCollection<CheckWriteDto> Checks { get; } = new();

        private readonly decimal _expectedAmount;

        public CheckDetailsWindow(decimal expectedAmount, IEnumerable<CheckWriteDto>? existingChecks = null)
        {
            InitializeComponent();
            _expectedAmount = expectedAmount;

            if (existingChecks != null)
            {
                foreach (var check in existingChecks)
                {
                    Checks.Add(new CheckWriteDto
                    {
                        Id = check.Id,
                        CheckNumber = check.CheckNumber,
                        BankName = check.BankName,
                        DueDate = check.DueDate,
                        Amount = check.Amount,
                        Status = check.Status,
                        Notes = check.Notes,
                        VoucherId = check.VoucherId,
                        InvoiceId = check.InvoiceId,
                        CreatedDate = check.CreatedDate,
                        UpdatedDate = check.UpdatedDate
                    });
                }
            }

            DataContext = this;
            Loaded += CheckDetailsWindow_Loaded;
        }

        public IReadOnlyList<CheckWriteDto> ResultChecks => Checks.ToList();

        private void CheckDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);

            TitleText.Text = UiText.T("تفاصيل الشيكات", "Check Details");
            ExpectedAmountText.Text = UiText.T($"إجمالي الفاتورة المطلوب: {_expectedAmount:0.00000}", $"Invoice amount required: {_expectedAmount:0.00000}");

            CheckNumberLabel.Text = UiText.T("رقم الشيك", "Check Number");
            BankNameLabel.Text = UiText.T("اسم البنك", "Bank Name");
            DueDateLabel.Text = UiText.T("تاريخ الاستحقاق", "Due Date");
            AmountLabel.Text = UiText.T("المبلغ", "Amount");
            NotesLabel.Text = UiText.T("ملاحظات", "Notes");

            CheckNumberColumn.Header = UiText.T("رقم الشيك", "Check Number");
            BankNameColumn.Header = UiText.T("البنك", "Bank");
            DueDateColumn.Header = UiText.T("تاريخ الاستحقاق", "Due Date");
            AmountColumn.Header = UiText.T("المبلغ", "Amount");
            NotesColumn.Header = UiText.T("ملاحظات", "Notes");
            DeleteColumn.Header = UiText.T("حذف", "Delete");

            OkButton.Content = UiText.T("موافق", "OK");
            CancelButton.Content = UiText.T("إلغاء", "Cancel");
            AddCheckButton.Content = UiText.T("إضافة", "Add");

            ChecksGrid.Items.Refresh();
        }

        private void AddCheck_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CheckNumberBox.Text))
            {
                MessageBox.Show(
                    UiText.T("يرجى إدخال رقم الشيك.", "Please enter the check number."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(CheckAmountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) ||
                amount <= 0)
            {
                MessageBox.Show(
                    UiText.T("يرجى إدخال مبلغ صالح للشيك.", "Please enter a valid check amount."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var checkNumber = CheckNumberBox.Text.Trim();
            if (Checks.Any(c => string.Equals(c.CheckNumber, checkNumber, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    UiText.T("رقم الشيك مكرر.", "The check number is duplicated."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Checks.Add(new CheckWriteDto
            {
                CheckNumber = checkNumber,
                BankName = string.IsNullOrWhiteSpace(BankNameBox.Text) ? "-" : BankNameBox.Text.Trim(),
                DueDate = CheckDueDatePicker.SelectedDate ?? DateTime.Now,
                Amount = amount,
                Status = CheckStatus.Pending,
                Notes = string.IsNullOrWhiteSpace(CheckNotesBox.Text) ? null : CheckNotesBox.Text.Trim(),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

            ChecksGrid.Items.Refresh();
            CheckNumberBox.Clear();
            BankNameBox.Clear();
            CheckAmountBox.Clear();
            CheckNotesBox.Clear();
            CheckDueDatePicker.SelectedDate = null;
        }

        private void DeleteCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not CheckWriteDto check)
                return;

            Checks.Remove(check);
            ChecksGrid.Items.Refresh();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var total = Math.Round(Checks.Sum(c => c.Amount), 3);
            var expected = Math.Round(_expectedAmount, 3);

            if (Checks.Count == 0)
            {
                MessageBox.Show(
                    UiText.T("يرجى إضافة شيك واحد على الأقل.", "Please add at least one check."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (total != expected)
            {
                MessageBox.Show(
                    UiText.T($"مجموع الشيكات ({total:0.00000}) يجب أن يساوي إجمالي الفاتورة ({expected:0.00000}).", $"The total check amount ({total:0.00000}) must equal the invoice total ({expected:0.00000})."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
