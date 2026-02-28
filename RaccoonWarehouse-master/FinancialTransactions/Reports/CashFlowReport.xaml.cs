using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.FinancialTransactions.Reports
{
    public partial class CashFlowReport : Window
    {
        private readonly IFinancialTransactionService _service;

        public CashFlowReport(IFinancialTransactionService service)
        {
            InitializeComponent();
            _service = service;

            Loaded += CashFlowReport_Loaded;
        }

        private void CashFlowReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;

            // Direction
            DirectionComboBox.Items.Clear();
            DirectionComboBox.Items.Add(new ComboBoxItem { Content = "الكل", Tag = null });
            DirectionComboBox.Items.Add(new ComboBoxItem { Content = "داخل (قبض)", Tag = TransactionDirection.In });
            DirectionComboBox.Items.Add(new ComboBoxItem { Content = "خارج (صرف)", Tag = TransactionDirection.Out });
            DirectionComboBox.SelectedIndex = 0;

            // PaymentMethod
            PaymentMethodComboBox.Items.Clear();
            PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = "الكل", Tag = null });
            foreach (var v in Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>())
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = v.ToString(), Tag = v });
            PaymentMethodComboBox.SelectedIndex = 0;

            // SourceType
            SourceTypeComboBox.Items.Clear();
            SourceTypeComboBox.Items.Add(new ComboBoxItem { Content = "الكل", Tag = null });
            foreach (var v in Enum.GetValues(typeof(FinancialSourceType)).Cast<FinancialSourceType>())
                SourceTypeComboBox.Items.Add(new ComboBoxItem { Content = v.ToString(), Tag = v });
            SourceTypeComboBox.SelectedIndex = 0;
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("يرجى اختيار تاريخ البداية والنهاية.");
                    return;
                }

                var filter = new CashFlowFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date,
                };

                if (DirectionComboBox.SelectedItem is ComboBoxItem d && d.Tag is TransactionDirection dir)
                    filter.Direction = dir;

                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem pm && pm.Tag is PaymentMethod method)
                    filter.Method = method;

                if (SourceTypeComboBox.SelectedItem is ComboBoxItem st && st.Tag is FinancialSourceType src)
                    filter.SourceType = src;

                var (summary, rows) = await _service.GetCashFlowAsync(filter);

                CashFlowGrid.ItemsSource = rows ?? new List<CashFlowRowDto>();

                TotalInText.Text = summary.TotalIn.ToString("0.00");
                TotalOutText.Text = summary.TotalOut.ToString("0.00");
                NetText.Text = summary.Net.ToString("0.00");
                CountText.Text = summary.CountAll.ToString();
                CashNetText.Text = summary.CashNet.ToString("0.00");
                VisaNetText.Text = summary.VisaNet.ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}