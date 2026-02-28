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
      
        public decimal UnitPrice { get; set; }
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
        public DateTime ExpiryDate { get; set; }   // ✅ تمت إضافته

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public ProductReadDto? SelectedProduct { get; set; }
        public decimal UnitCost { get; set; }          // PurchasePrice at time of invoice
        public decimal CostTotal => Quantity * UnitCost; // optional computed

        public bool TaxExempt { get; set; }
        public decimal TaxRate { get; set; }           // snapshot from product وقت البيع
        public decimal TaxAmount { get; set; }         // الضريبة لهذا السطر
        public decimal LineSubTotal { get; set; }      // Quantity * UnitPrice قبل الضريبة
        public decimal Profit { get; set; }        // (LineSubTotal - Tax? عادة لا) - CostTotal
        public decimal ProfitBeforeTax { get; set; } // (LineSubTotal) - CostTotal


    }
}
