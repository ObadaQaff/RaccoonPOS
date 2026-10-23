using System.Globalization;
using System.Windows;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices;

public partial class POS
{
    // References identify rows discounted by this field, as opposed to existing
    // manual/default discounts. Persisted discounts continue to live on the rows.
    private readonly Dictionary<InvoiceLineWriteDto, decimal> _invoiceDiscountRows = new();
    private bool _hasDistributedInvoiceDiscount;
    private bool _isApplyingInvoiceDiscount;
    private bool _invoiceDiscountReviewRequired;
    private decimal _invoiceDiscountInputValue;

    private void ResetDistributedInvoiceDiscount()
    {
        _invoiceDiscountRows.Clear();
        _hasDistributedInvoiceDiscount = false;
        _invoiceDiscountReviewRequired = false;
        _invoiceDiscountInputValue = 0m;
        _invoiceDiscountIsPercentage = false;
        _invoiceDiscountPercent = 0m;
    }

    private void DetachDistributedInvoiceDiscount(InvoiceLineWriteDto row)
        => _invoiceDiscountRows.Remove(row);

    private List<InvoiceLineWriteDto> GetEligibleInvoiceDiscountRows()
    {
        foreach (var row in _invoiceDiscountRows.Keys.Where(row => !_invoiceLines.Contains(row)).ToList())
            _invoiceDiscountRows.Remove(row);

        return _invoiceLines.Where(row => row.ProductId > 0 && row.Quantity > 0m && row.UnitPrice > 0m &&
            (row.LineDiscountAmount == 0m || _invoiceDiscountRows.ContainsKey(row))).ToList();
    }

    private decimal GetInvoiceDiscountPolicyMaximum(IReadOnlyList<InvoiceLineWriteDto> rows)
    {
        var maximum = 100m;
        foreach (var row in rows)
        {
            var rowMaximum = 100m;
            // A lower-priority maximum rule may become effective after the first
            // maximum rule no longer matches. Follow that chain before applying.
            while (rowMaximum > 0m)
            {
                var decision = GetLineSaleDecision(row, rowMaximum);
                if (!decision.BlocksDiscount)
                    break;
                var nextMaximum = Math.Clamp(decision.DiscountLimitPercent ?? 0m, 0m, 100m);
                if (nextMaximum >= rowMaximum)
                {
                    rowMaximum = 0m;
                    break;
                }
                rowMaximum = nextMaximum;
            }
            maximum = Math.Min(maximum, rowMaximum);
        }
        return Math.Floor(maximum * 1_000m) / 1_000m;
    }

    private string GetInvoiceDiscountInputText(decimal legacyAmount = 0m)
        => _hasDistributedInvoiceDiscount
            ? _invoiceDiscountIsPercentage
                ? $"{_invoiceDiscountInputValue:0.############################}%"
                : _invoiceDiscountInputValue.ToString("0.000", CultureInfo.CurrentCulture)
            : legacyAmount.ToString("0.000", CultureInfo.CurrentCulture);

    private void RestoreInvoiceDiscountInput()
    {
        _isUpdatingInvoiceDiscountText = true;
        try
        {
            DiscountTextBox.Text = GetInvoiceDiscountInputText(_currentInvoice.DiscountAmount ?? 0m);
        }
        finally
        {
            _isUpdatingInvoiceDiscountText = false;
        }
    }

    private void ShowInvoiceDiscountMaximum(decimal maximum)
    {
        MessageBox.Show(this,
            string.Format(CultureInfo.CurrentCulture,
                UiText.T("تم رفض الخصم. أقصى نسبة مسموحة: {0}%.",
                    "Discount rejected. Maximum allowed percentage: {0}%."),
                maximum.ToString("0.###", CultureInfo.CurrentCulture)),
            UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private bool ApplyInvoiceDiscountInput()
    {
        if (_isApplyingInvoiceDiscount || _isUpdatingInvoiceDiscountText || !IsLoaded || _currentInvoice == null)
            return true;

        _isApplyingInvoiceDiscount = true;
        try
        {
            var raw = DiscountTextBox.Text?.Trim() ?? string.Empty;
            var isPercentage = raw.EndsWith('%');
            var valueText = string.IsNullOrWhiteSpace(raw) ? "0" : isPercentage ? raw[..^1].Trim() : raw;
            if (!TryParseDecimalInput(valueText, out var value) || value < 0m)
            {
                MessageBox.Show(this,
                    UiText.T("أدخل الخصم كقيمة مثل 0.200 أو كنسبة مثل 1%.",
                        "Enter a discount amount such as 0.200 or a percentage such as 1%."),
                    UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                RestoreInvoiceDiscountInput();
                return false;
            }

            if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn)
            {
                RestoreInvoiceDiscountInput();
                return value == 0m;
            }

            var rows = GetEligibleInvoiceDiscountRows();
            if (value > 0m && rows.Count == 0)
            {
                MessageBox.Show(this,
                    UiText.T("لا توجد أصناف مؤهلة للخصم. الأصناف التي عليها خصم مسبقاً مستثناة.",
                        "No items are eligible for this discount. Items already discounted are excluded."),
                    UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                RestoreInvoiceDiscountInput();
                return false;
            }

            var plan = PosInvoiceDiscountCalculator.Calculate(rows, value, isPercentage,
                GetInvoiceDiscountPolicyMaximum(rows));
            if (!plan.Success)
            {
                ShowInvoiceDiscountMaximum(plan.MaximumPercentage);
                RestoreInvoiceDiscountInput();
                return false;
            }

            // Check every policy before changing a row, so rejection is atomic.
            if (value > 0m && rows.Any(row => IsLineDiscountBlocked(row, plan.Percentage)))
            {
                RestoreInvoiceDiscountInput();
                return false;
            }

            WriteDistributedInvoiceDiscount(rows, plan, value, isPercentage);
            _invoiceDiscountReviewRequired = false;
            RestoreInvoiceDiscountInput();
            return true;
        }
        finally
        {
            _isApplyingInvoiceDiscount = false;
        }
    }

    private void WriteDistributedInvoiceDiscount(
        IReadOnlyList<InvoiceLineWriteDto> rows, PosInvoiceDiscountPlan plan, decimal input, bool isPercentage)
    {
        foreach (var oldRow in _invoiceDiscountRows.Keys.Where(row => !rows.Contains(row)).ToList())
        {
            oldRow.LineDiscountAmount = 0m;
            oldRow.LineDiscountInput = "0.000";
            RecalculateLineFromCurrentValues(oldRow);
        }
        _invoiceDiscountRows.Clear();

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            row.LineDiscountIsPercentage = true;
            row.LineDiscountAmount = plan.Amounts[index];
            row.LineDiscountInput = $"{plan.Percentage:0.######}%";
            RecalculateLineFromCurrentValues(row);
            if (input > 0m)
                _invoiceDiscountRows[row] = row.LineDiscountAmount;
        }

        _hasDistributedInvoiceDiscount = input > 0m;
        _invoiceDiscountInputValue = input;
        _invoiceDiscountIsPercentage = isPercentage;
        _invoiceDiscountPercent = plan.Percentage;
        // All new discounts are stored in LineDiscountAmount. A second invoice
        // deduction would subtract the same money again in InvoiceService.
        _currentInvoice.DiscountAmount = 0m;
    }

    private void RefreshDistributedInvoiceDiscount()
    {
        if (!_hasDistributedInvoiceDiscount || _isApplyingInvoiceDiscount ||
            _isLoadingHeldInvoice || _isLoadingReturnInvoice)
            return;

        _isApplyingInvoiceDiscount = true;
        try
        {
            var rows = GetEligibleInvoiceDiscountRows();
            var plan = PosInvoiceDiscountCalculator.Calculate(rows, _invoiceDiscountInputValue,
                _invoiceDiscountIsPercentage, GetInvoiceDiscountPolicyMaximum(rows));
            if (plan.Success)
            {
                WriteDistributedInvoiceDiscount(rows, plan, _invoiceDiscountInputValue, _invoiceDiscountIsPercentage);
                return;
            }

            // A price, quantity, cost or rule changed. Remove this field's own
            // discounts; pre-existing/manual row discounts remain untouched.
            foreach (var row in _invoiceDiscountRows.Keys.ToList())
            {
                row.LineDiscountAmount = 0m;
                row.LineDiscountInput = "0.000";
                RecalculateLineFromCurrentValues(row);
            }
            ResetDistributedInvoiceDiscount();
            _invoiceDiscountReviewRequired = true;
            _currentInvoice.DiscountAmount = 0m;
            RestoreInvoiceDiscountInput();
            ShowInvoiceDiscountMaximum(plan.MaximumPercentage);
        }
        finally
        {
            _isApplyingInvoiceDiscount = false;
        }
    }

    private bool ValidateDistributedInvoiceDiscount()
    {
        if (_invoiceDiscountReviewRequired)
        {
            MessageBox.Show(this,
                UiText.T("تغيرت الأصناف وأُلغي خصم الفاتورة. أدخل خصماً جديداً أو امسح حقل الخصم قبل المتابعة.",
                    "Items changed and the invoice discount was removed. Enter a new discount or clear the discount field before continuing."),
                UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return !DiscountTextBox.IsKeyboardFocusWithin || ApplyInvoiceDiscountInput();
    }

    private bool TryPrepareAllocatedInvoiceDiscounts(
        IReadOnlyList<InvoiceLineWriteDto> sourceRows,
        IReadOnlyDictionary<int, List<StockLotAllocationDto>> allocations,
        out Dictionary<int, decimal[]> discounts)
    {
        discounts = new();
        if (!_hasDistributedInvoiceDiscount)
            return true;

        var allocatedRows = new Dictionary<int, List<InvoiceLineWriteDto>>();
        for (var index = 0; index < sourceRows.Count; index++)
        {
            var source = sourceRows[index];
            if (!_invoiceDiscountRows.ContainsKey(source) || !allocations.TryGetValue(index, out var lots))
                continue;
            allocatedRows[index] = lots.Select(lot => new InvoiceLineWriteDto
            {
                Quantity = lot.Quantity,
                UnitPrice = source.UnitPrice,
                UnitCost = lot.PurchasePrice,
                TaxExempt = source.TaxExempt,
                TaxRate = source.TaxRate
            }).ToList();
        }

        var allRows = allocatedRows.Values.SelectMany(rows => rows).ToList();
        if (allRows.Count == 0)
            return true;

        var maximum = Math.Min(PosInvoiceDiscountCalculator.GetMaximumPercentage(allRows),
            GetInvoiceDiscountPolicyMaximum(GetEligibleInvoiceDiscountRows()));
        if (_invoiceDiscountPercent > maximum)
        {
            ShowInvoiceDiscountMaximum(maximum);
            return false;
        }

        foreach (var (index, rows) in allocatedRows)
        {
            var amounts = PosInvoiceDiscountCalculator.AllocateAmounts(rows,
                _invoiceDiscountPercent, sourceRows[index].LineDiscountAmount);
            if (amounts == null)
            {
                ShowInvoiceDiscountMaximum(maximum);
                return false;
            }
            discounts[index] = amounts;
        }
        return true;
    }
}
