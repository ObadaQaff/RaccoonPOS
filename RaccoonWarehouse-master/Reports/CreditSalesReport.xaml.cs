using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Reports
{
    public partial class CreditSalesReport : Window
    {
        private readonly IFinancialTransactionService _financialTransactionService;
        private readonly IUserService _userService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

        public CreditSalesReport(IFinancialTransactionService financialTransactionService, IUserService userService, SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            _financialTransactionService = financialTransactionService;
            _userService = userService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += CreditSalesReport_Loaded;
        }

        private async void CreditSalesReportGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CreditSalesReportGrid.SelectedItem is not RaccoonWarehouse.Domain.Reports.Financial.Dtos.CreditSalesReportRowDto row || row.InvoiceId <= 0)
                return;

            await _sourceDocumentNavigationService.OpenSourceDocument("Invoice", row.InvoiceId);
        }

        private async void CreditSalesReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;

            StatusComboBox.Items.Clear();
            StatusComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = "all" });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("غير مسدد", "Unpaid"), Tag = "unpaid" });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مسدد جزئي", "Partially Paid"), Tag = "partial" });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مسدد بالكامل", "Fully Paid"), Tag = "paid" });
            StatusComboBox.SelectedIndex = 0;

            var usersRes = await _userService.GetAllAsync();
            var users = usersRes?.Data ?? new List<UserReadDto>();
            var customerList = new List<UserReadDto>
            {
                new UserReadDto { Id = 0, Name = UiText.T("الكل", "All") }
            };
            customerList.AddRange(users);

            CustomerComboBox.ItemsSource = customerList;
            CustomerComboBox.DisplayMemberPath = "Name";
            CustomerComboBox.SelectedValuePath = "Id";
            CustomerComboBox.SelectedValue = 0;

            await LoadReportAsync();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async System.Threading.Tasks.Task LoadReportAsync()
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار تاريخ البداية والنهاية.", "Please choose the start and end dates."));
                    return;
                }

                int? customerId = null;
                if (CustomerComboBox.SelectedValue is int selectedCustomerId && selectedCustomerId != 0)
                    customerId = selectedCustomerId;

                var status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                var rows = await _financialTransactionService.GetCreditSalesReportAsync(
                    FromDatePicker.SelectedDate.Value.Date,
                    ToDatePicker.SelectedDate.Value.Date,
                    customerId,
                    status);

                CreditSalesReportGrid.ItemsSource = rows;
                TotalInvoicesText.Text = rows.Count.ToString();
                TotalDueText.Text = rows.Sum(x => x.InvoiceTotal).ToString("0.00000");
                TotalPaidText.Text = rows.Sum(x => x.AmountPaid).ToString("0.00000");
                TotalRemainingText.Text = rows.Sum(x => x.RemainingAmount).ToString("0.00000");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
        }
    }
}
