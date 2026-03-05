using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
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

        public CreditSalesReport(IFinancialTransactionService financialTransactionService, IUserService userService)
        {
            _financialTransactionService = financialTransactionService;
            _userService = userService;
            InitializeComponent();
            Loaded += CreditSalesReport_Loaded;
        }

        private async void CreditSalesReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;

            StatusComboBox.Items.Clear();
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "الكل" });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "غير مسدد" });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "مسدد جزئي" });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "مسدد بالكامل" });
            StatusComboBox.SelectedIndex = 0;

            var usersRes = await _userService.GetAllAsync();
            var users = usersRes?.Data ?? new List<UserReadDto>();
            var customerList = new List<UserReadDto> { new UserReadDto { Id = 0, Name = "الكل" } };
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
                    MessageBox.Show("يرجى اختيار تاريخ البداية والنهاية.");
                    return;
                }

                int? customerId = null;
                if (CustomerComboBox.SelectedValue is int selectedCustomerId && selectedCustomerId != 0)
                    customerId = selectedCustomerId;

                var status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

                var rows = await _financialTransactionService.GetCreditSalesReportAsync(
                    FromDatePicker.SelectedDate.Value.Date,
                    ToDatePicker.SelectedDate.Value.Date,
                    customerId,
                    status);

                CreditSalesReportGrid.ItemsSource = rows;
                TotalInvoicesText.Text = rows.Count.ToString();
                TotalDueText.Text = rows.Sum(x => x.InvoiceTotal).ToString("0.00");
                TotalPaidText.Text = rows.Sum(x => x.AmountPaid).ToString("0.00");
                TotalRemainingText.Text = rows.Sum(x => x.RemainingAmount).ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }
    }
}
