using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Domain.POS.VM
{
    public class DailySalesReportViewModel : INotifyPropertyChanged
    {
        private DateTime _reportDate = DateTime.Today;
        private string _cashierName = string.Empty;
        private string _scopeLabel = string.Empty;
        private int _sessionId;
        private string _sessionStatus = string.Empty;
        private decimal _openingBalance;
        private decimal _closingBalance;
        private int _totalInvoices;
        private int _totalVouchers;
        private int _totalFinancialTransactions;
        private int _totalDocuments;
        private decimal _totalSales;
        private decimal _totalDiscount;
        private decimal _totalIn;
        private decimal _totalOut;

        public DateTime ReportDate
        {
            get => _reportDate;
            set { _reportDate = value; OnPropertyChanged(); }
        }

        public string CashierName
        {
            get => _cashierName;
            set { _cashierName = value; OnPropertyChanged(); }
        }

        public string ScopeLabel
        {
            get => _scopeLabel;
            set { _scopeLabel = value; OnPropertyChanged(); }
        }

        public int SessionId
        {
            get => _sessionId;
            set { _sessionId = value; OnPropertyChanged(); }
        }

        public string SessionStatus
        {
            get => _sessionStatus;
            set { _sessionStatus = value; OnPropertyChanged(); }
        }

        public decimal OpeningBalance
        {
            get => _openingBalance;
            set { _openingBalance = value; OnPropertyChanged(); OnPropertyChanged(nameof(SessionNet)); }
        }

        public decimal ClosingBalance
        {
            get => _closingBalance;
            set { _closingBalance = value; OnPropertyChanged(); OnPropertyChanged(nameof(SessionNet)); }
        }

        public decimal SessionNet => ClosingBalance - OpeningBalance;

        public int TotalInvoices
        {
            get => _totalInvoices;
            set { _totalInvoices = value; OnPropertyChanged(); }
        }

        public int TotalVouchers
        {
            get => _totalVouchers;
            set { _totalVouchers = value; OnPropertyChanged(); }
        }

        public int TotalFinancialTransactions
        {
            get => _totalFinancialTransactions;
            set { _totalFinancialTransactions = value; OnPropertyChanged(); }
        }

        public int TotalDocuments
        {
            get => _totalDocuments;
            set { _totalDocuments = value; OnPropertyChanged(); }
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

        public decimal TotalIn
        {
            get => _totalIn;
            set { _totalIn = value; OnPropertyChanged(); OnPropertyChanged(nameof(NetMovement)); }
        }

        public decimal TotalOut
        {
            get => _totalOut;
            set { _totalOut = value; OnPropertyChanged(); OnPropertyChanged(nameof(NetMovement)); }
        }

        public decimal NetMovement => TotalIn - TotalOut;

        public ObservableCollection<InvoiceReadDto> Invoices { get; } = new();
        public ObservableCollection<CashierSessionReadDto> Sessions { get; } = new();
        public ObservableCollection<SessionTransactionRowDto> Transactions { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
