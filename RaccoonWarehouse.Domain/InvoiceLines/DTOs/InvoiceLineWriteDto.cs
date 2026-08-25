using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.InvoiceLines.DTOs
{
    public class InvoiceLineWriteDto : IBaseDto, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private decimal _quantity;

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal)); // Also notify that LineTotal changed
                }
            }
        }
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string? OriginalInvoiceId { get; set; }
        public InvoiceWriteDto? Invoice { get; set; }

        public int ProductId { get; set; }
        public ProductWriteDto? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnitWriteDto? ProductUnit { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; } = 1m;
        public decimal BaseQuantity { get; set; }
        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }
        private decimal _lineDiscountAmount;
        public decimal LineDiscountAmount
        {
            get => _lineDiscountAmount;
            set
            {
                if (_lineDiscountAmount != value)
                {
                    _lineDiscountAmount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }
        private decimal _freeQuantity;
        public decimal FreeQuantity
        {
            get => _freeQuantity;
            set
            {
                if (_freeQuantity != value)
                {
                    _freeQuantity = value;
                    OnPropertyChanged();
                }
            }
        }
        public string? ProductName { get; set; }
        private string? _unitName;
        public string? UnitName
        {
            get => _unitName;
            set
            {
                if (_unitName != value)
                {
                    _unitName = value;
                    OnPropertyChanged();
                }
            }
        }
        public decimal LineTotal => Math.Max(0m, Quantity * UnitPrice - LineDiscountAmount);
        public void RefreshCalculatedProperties()
        {
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(CostTotal));
        }
        public DateTime ExpiryDate { get; set; }   // ✅ تمت إضافته

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public ProductReadDto? SelectedProduct { get; set; }
        public decimal UnitCost { get; set; }          // PurchasePrice at time of invoice
        public decimal CostTotal => Quantity * UnitCost; // optional computed

        public bool TaxExempt { get; set; }
        public decimal TaxRate { get; set; }           // snapshot from product وقت البيع
        private decimal _taxAmount;
        public decimal TaxAmount
        {
            get => _taxAmount;
            set
            {
                if (_taxAmount != value)
                {
                    _taxAmount = value;
                    OnPropertyChanged();
                }
            }
        }         // Tax for this line
        public decimal LineSubTotal { get; set; }      // Quantity * UnitPrice قبل الضريبة
        public decimal Profit { get; set; }        // (LineSubTotal - Tax? عادة لا) - CostTotal
        public decimal ProfitBeforeTax { get; set; } // (LineSubTotal) - CostTotal
        private decimal _availableQuantitySnapshot;
        public decimal AvailableQuantitySnapshot
        {
            get => _availableQuantitySnapshot;
            set
            {
                if (_availableQuantitySnapshot != value)
                {
                    _availableQuantitySnapshot = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _unitNameSnapshot;
        public string? UnitNameSnapshot
        {
            get => _unitNameSnapshot;
            set
            {
                if (_unitNameSnapshot != value)
                {
                    _unitNameSnapshot = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
