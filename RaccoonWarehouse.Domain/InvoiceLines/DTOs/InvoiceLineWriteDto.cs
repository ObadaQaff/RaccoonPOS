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
                    if (PurchaseBonusRatio.TryCalculate(BonusRatio, value, out var bonus))
                        FreeQuantity = bonus;
                    OnPropertyChanged(nameof(TotalReceivedQuantity));
                    OnPropertyChanged(nameof(EffectiveLotUnitCost));
                    OnPropertyChanged(nameof(BonusNeedsApply));
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal)); // Also notify that LineTotal changed
                    OnPropertyChanged(nameof(PurchaseLineTotal));
                    OnPropertyChanged(nameof(LineDiscountPercent));
                    OnPropertyChanged(nameof(LineDiscountDisplay));
                    OnPropertyChanged(nameof(LineDiscountInput));
                }
            }
        }
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string? OriginalInvoiceId { get; set; }
        public InvoiceWriteDto? Invoice { get; set; }

        public int ProductId { get; set; }
        // POS-only lot preference; stock allocation still validates availability at checkout.
        public int? PreferredStockLotId { get; set; }
        public ProductWriteDto? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnitWriteDto? ProductUnit { get; set; }
        public string BarcodeDisplay => !string.IsNullOrWhiteSpace(ProductUnit?.AlternateBarcode)
            ? ProductUnit.AlternateBarcode!
            : SelectedProduct?.ITEMCODE?.ToString() ?? "---";
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
                    OnPropertyChanged(nameof(PurchaseLineTotal));
                    OnPropertyChanged(nameof(LineDiscountPercent));
                    OnPropertyChanged(nameof(LineDiscountDisplay));
                    OnPropertyChanged(nameof(LineDiscountInput));
                }
            }
        }
        private decimal _lineDiscountAmount;
        private bool _lineDiscountIsPercentage;
        private decimal _lineDiscountPercent;
        private string? _lineDiscountInput;
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
                    OnPropertyChanged(nameof(PurchaseLineTotal));
                    OnPropertyChanged(nameof(LineDiscountPercent));
                    OnPropertyChanged(nameof(LineDiscountDisplay));
                    OnPropertyChanged(nameof(LineDiscountInput));
                }
            }
        }

        // UI-only mode; the persisted source of truth remains LineDiscountAmount.
        public bool LineDiscountIsPercentage
        {
            get => _lineDiscountIsPercentage;
            set
            {
                if (_lineDiscountIsPercentage != value)
                {
                    _lineDiscountIsPercentage = value;
                    if (!value)
                        _lineDiscountPercent = 0m;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineDiscountDisplay));
                    OnPropertyChanged(nameof(LineDiscountInput));
                }
            }
        }

        // UI-only input buffer. POS interprets a trailing '%' as percentage mode.
        public string LineDiscountInput
        {
            get => _lineDiscountInput ?? LineDiscountDisplay;
            set
            {
                if (_lineDiscountInput != value)
                {
                    _lineDiscountInput = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LineDiscountDisplay => _lineDiscountInput ?? (LineDiscountIsPercentage
            ? $"{LineDiscountPercent:0.000}%"
            : $"{LineDiscountAmount:0.000}");

        // UI-only marker. The POS sets this after the decimal shortcut has
        // passed partial-sale validation and conversion.
        public bool PartialSaleResolved { get; set; }

        // The stored value remains LineDiscountAmount. This calculated property
        // provides percentage editing in POS without changing the database.
        public decimal LineDiscountPercent
        {
            get
            {
                if (LineDiscountIsPercentage)
                    return _lineDiscountPercent;

                var grossLineTotal = Math.Abs(Quantity * UnitPrice);
                return grossLineTotal <= 0m
                    ? 0m
                    : Math.Round(Math.Abs(LineDiscountAmount) / grossLineTotal * 100m, 3, MidpointRounding.AwayFromZero);
            }
            set
            {
                var percentage = Math.Clamp(value, 0m, 100m);
                _lineDiscountPercent = percentage;
                var grossLineTotal = Math.Abs(Quantity * UnitPrice);
                LineDiscountAmount = grossLineTotal <= 0m
                    ? 0m
                    : Math.Round(grossLineTotal * percentage / 100m, 3, MidpointRounding.AwayFromZero);
                OnPropertyChanged();
                OnPropertyChanged(nameof(LineDiscountDisplay));
                OnPropertyChanged(nameof(LineDiscountInput));
            }
        }
        private string? _bonusRatio;
        public string? BonusRatio
        {
            get => _bonusRatio;
            set { _bonusRatio = value; OnPropertyChanged(); OnPropertyChanged(nameof(BonusNeedsApply)); }
        }
        private decimal? _bonusPercentage;
        public decimal? BonusPercentage
        {
            get => _bonusPercentage;
            set
            {
                if (_bonusPercentage != value)
                {
                    _bonusPercentage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BonusPercentageInput));
                    OnPropertyChanged(nameof(BonusNeedsApply));
                    OnPropertyChanged(nameof(PurchaseLineTotal));
                }
            }
        }
        private string? _bonusPercentageInput;
        public string BonusPercentageInput
        {
            get => _bonusPercentageInput ?? (BonusPercentage.HasValue ? $"{BonusPercentage.Value:0.###}%" : string.Empty);
            set
            {
                if (_bonusPercentageInput != value)
                {
                    _bonusPercentageInput = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BonusNeedsApply));
                }
            }
        }
        private decimal? _purchaseAcquisitionCost;
        public decimal? PurchaseAcquisitionCost
        {
            get => _purchaseAcquisitionCost;
            set { _purchaseAcquisitionCost = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveLotUnitCost)); }
        }
        public int? SourcePurchaseLineId { get; set; }
        public decimal TotalReceivedQuantity => Quantity + FreeQuantity;
        public decimal? EffectiveLotUnitCost => TotalReceivedQuantity > 0m && PurchaseAcquisitionCost.HasValue
            ? Math.Round(PurchaseAcquisitionCost.Value / TotalReceivedQuantity, 6) : null;
        public bool BonusNeedsApply
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(BonusRatio))
                    return !PurchaseBonusRatio.TryCalculate(BonusRatio, Quantity, out var expectedRatio)
                        || expectedRatio != FreeQuantity;

                if (BonusPercentage.HasValue)
                {
                    return PurchaseBonusPercentage.TryNormalize(BonusPercentageInput, out var normalized)
                        && normalized != BonusPercentage.Value;
                }

                return false;
            }
        }
        public bool ApplyBonus()
        {
            if (!PurchaseBonusRatio.TryCalculate(BonusRatio, Quantity, out var free)) return false;
            FreeQuantity = free;
            return true;
        }
        public bool ApplyBonusPercentage()
        {
            if (!PurchaseBonusPercentage.TryNormalize(BonusPercentageInput, out var normalized)) return false;
            BonusPercentage = normalized;
            BonusRatio = null;
            // Percentage bonus affects cost only; it must not create free stock quantity.
            FreeQuantity = 0m;
            return true;
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
                    OnPropertyChanged(nameof(TotalReceivedQuantity));
                    OnPropertyChanged(nameof(EffectiveLotUnitCost));
                    OnPropertyChanged(nameof(BonusNeedsApply));
                    OnPropertyChanged();
                }
            }
        }
        private string? _productName;
        public string? ProductName
        {
            get => _productName;
            set
            {
                if (_productName != value)
                {
                    _productName = value;
                    OnPropertyChanged();
                }
            }
        }
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
        public decimal PurchaseLineTotal
        {
            get
            {
                var lineTotal = LineTotal;
                if (!BonusPercentage.HasValue || Quantity <= 0m)
                    return lineTotal;

                var costQuantity = PurchaseBonusPercentage.CalculateCostQuantity(Quantity, BonusPercentage.Value);
                return costQuantity > 0m
                    ? Math.Round(lineTotal * Quantity / costQuantity, 3, MidpointRounding.AwayFromZero)
                    : lineTotal;
            }
        }
        // UI-only POS row presentation. It is never persisted with the invoice.
        public string RowBackground { get; set; } = "#FFFFFF";
        public string RowForeground { get; set; } = "#263238";
        public string RowAccent { get; set; } = "#90A4AE";
        public void RefreshCalculatedProperties()
        {
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(PurchaseLineTotal));
            OnPropertyChanged(nameof(CostTotal));
            OnPropertyChanged(nameof(RowBackground));
            OnPropertyChanged(nameof(RowForeground));
            OnPropertyChanged(nameof(RowAccent));
        }
        private DateTime _expiryDate;
        public DateTime ExpiryDate
        {
            get => _expiryDate;
            set
            {
                if (_expiryDate != value)
                {
                    _expiryDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpiryDateDisplay));
                }
            }
        }

        public string ExpiryDateDisplay => ExpiryDate == default
            ? "---"
            : ExpiryDate.ToString("dd-MM-yyyy");

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        private ProductReadDto? _selectedProduct;
        public ProductReadDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (!ReferenceEquals(_selectedProduct, value))
                {
                    _selectedProduct = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BarcodeDisplay));
                }
            }
        }
        public decimal UnitCost { get; set; }          // PurchasePrice at time of invoice
        private decimal _salePrice;
        public decimal SalePrice
        {
            get => _salePrice;
            set
            {
                if (_salePrice != value)
                {
                    _salePrice = value;
                    OnPropertyChanged();
                }
            }
        }
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
