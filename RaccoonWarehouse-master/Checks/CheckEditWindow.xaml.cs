using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Invoices
{
    public partial class CheckEditWindow : Window
    {
        private readonly CheckWriteDto _check;

        public CheckEditWindow(CheckWriteDto check)
        {
            InitializeComponent();
            _check = check;
            Loaded += CheckEditWindow_Loaded;
        }

        public CheckWriteDto EditedCheck => _check;

        private void CheckEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);

            TitleText.Text = UiText.T("تعديل الشيك", "Edit Check");
            SubtitleText.Text = UiText.T(
                $"رقم الشيك: {_check.CheckNumber}",
                $"Check number: {_check.CheckNumber}");

            CheckNumberLabel.Text = UiText.T("رقم الشيك", "Check Number");
            BankNameLabel.Text = UiText.T("اسم البنك", "Bank Name");
            DueDateLabel.Text = UiText.T("تاريخ الاستحقاق", "Due Date");
            AmountLabel.Text = UiText.T("المبلغ", "Amount");
            NotesLabel.Text = UiText.T("ملاحظات", "Notes");

            SaveButton.Content = UiText.T("حفظ", "Save");
            CancelButton.Content = UiText.T("إلغاء", "Cancel");

            CheckNumberBox.Text = _check.CheckNumber;
            BankNameBox.Text = _check.BankName;
            CheckDueDatePicker.SelectedDate = _check.DueDate;
            CheckAmountBox.Text = _check.Amount.ToString("0.###", CultureInfo.CurrentCulture);
            CheckNotesBox.Text = _check.Notes ?? string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
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

            _check.CheckNumber = CheckNumberBox.Text.Trim();
            _check.BankName = string.IsNullOrWhiteSpace(BankNameBox.Text) ? "-" : BankNameBox.Text.Trim();
            _check.DueDate = CheckDueDatePicker.SelectedDate ?? DateTime.Now;
            _check.Amount = amount;
            _check.Notes = string.IsNullOrWhiteSpace(CheckNotesBox.Text) ? null : CheckNotesBox.Text.Trim();
            _check.UpdatedDate = DateTime.Now;

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
