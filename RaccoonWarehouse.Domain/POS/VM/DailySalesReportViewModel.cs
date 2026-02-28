using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Domain.POS.VM
{
    public class DailySalesReportViewModel : INotifyPropertyChanged
    {
        private DateTime _reportDate = DateTime.Today;
        private int _totalInvoices;
        private decimal _totalSales;
        private decimal _totalDiscount;
        public DateTime ReportDate
        {
            get => _reportDate;
            set { _reportDate = value; OnPropertyChanged(); }
        }

        public int TotalInvoices
        {
            get => _totalInvoices;
            set { _totalInvoices = value; OnPropertyChanged(); }
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set { _totalSales = value; OnPropertyChanged(); OnPropertyChanged(nameof(NetSales)); }
        }

        public decimal TotalDiscount
        {
            get => _totalDiscount;
            set { _totalDiscount = value; OnPropertyChanged(); OnPropertyChanged(nameof(NetSales)); }
        }

        public decimal NetSales => TotalSales - TotalDiscount;

        public ObservableCollection<InvoiceReadDto> Invoices { get; }
            = new ObservableCollection<InvoiceReadDto>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
