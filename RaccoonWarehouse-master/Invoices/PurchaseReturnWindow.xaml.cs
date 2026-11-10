using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Invoices;

public partial class PurchaseReturnWindow : Window
{
    private readonly IInvoiceService _invoiceService;
    private readonly ISaleCheckoutService _checkoutService;
    private readonly IUserService _userService;
    private readonly IUserSession _userSession;
    private readonly ObservableCollection<PurchaseReturnRow> _invoices = new();
    private readonly ObservableCollection<SupplierFilterOption> _supplierOptions = new();
    private readonly ObservableCollection<PurchaseReturnLineRow> _lines = new();
    private InvoiceWriteDto? _sourceInvoice;
    private bool _busy;

    public PurchaseReturnWindow(
        IInvoiceService invoiceService,
        ISaleCheckoutService checkoutService,
        IUserService userService,
        IUserSession userSession)
    {
        InitializeComponent();
        UiText.ApplyWindow(this);
        _invoiceService = invoiceService;
        _checkoutService = checkoutService;
        _userService = userService;
        _userSession = userSession;
        InvoicesGrid.ItemsSource = _invoices;
        SupplierFilterComboBox.ItemsSource = _supplierOptions;
        LinesGrid.ItemsSource = _lines;
        LinesGrid.IsEnabled = false;
        Loaded += async (_, _) =>
        {
            await LoadSuppliersAsync();
            await LoadInvoicesAsync();
        };
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadInvoicesAsync();

    private async void SupplierFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && !_busy)
            await LoadInvoicesAsync();
    }

    private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await LoadInvoicesAsync();
    }

    private async Task LoadInvoicesAsync()
    {
        if (_busy)
            return;

        try
        {
            _busy = true;
            var number = SearchTextBox.Text?.Trim();
            var productSearch = ProductSearchTextBox.Text?.Trim();
            var supplierId = (SupplierFilterComboBox.SelectedItem as SupplierFilterOption)?.Id;
            var result = await _invoiceService.SearchPurchaseInvoicesForReturnAsync(number, supplierId, productSearch);

            if (!result.Success)
            {
                ShowError(result.Message ?? UiText.T("تعذر تحميل فواتير الشراء.", "Could not load purchase invoices."));
                return;
            }

            _invoices.Clear();
            foreach (var invoice in (result.Data ?? new List<InvoiceWriteDto>()).OrderByDescending(x => x.CreatedDate))
                _invoices.Add(new PurchaseReturnRow(invoice));
        }
        catch (Exception ex)
        {
            ShowError(UiText.T($"تعذر تحميل فواتير الشراء: {ex.Message}", $"Could not load purchase invoices: {ex.Message}"));
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var result = await _userService.GetAllAsync();
            _supplierOptions.Clear();
            _supplierOptions.Add(new SupplierFilterOption(null, UiText.T("ÙƒÙ„ Ø§Ù„Ù…ÙˆØ±Ø¯ÙŠÙ†", "All suppliers")));

            foreach (var supplier in (result.Data ?? new List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto>())
                .Where(user => user.Role == UserRole.Supplier)
                .OrderBy(user => user.Name))
            {
                _supplierOptions.Add(new SupplierFilterOption(supplier.Id, supplier.Name));
            }

            SupplierFilterComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ShowError(UiText.T($"ØªØ¹Ø°Ø± ØªØ­Ù…ÙŠÙ„ Ø§Ù„Ù…ÙˆØ±Ø¯ÙŠÙ†: {ex.Message}", $"Could not load suppliers: {ex.Message}"));
        }
    }

    private async void SelectInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || InvoicesGrid.SelectedItem is not PurchaseReturnRow selected)
            return;

        await LoadSelectedInvoiceAsync(selected.Invoice);
    }

    private async Task LoadSelectedInvoiceAsync(InvoiceWriteDto source)
    {
        try
        {
            _busy = true;
            var previousResult = await _invoiceService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                invoice => invoice.InvoiceType == InvoiceType.PurchaseReturn
                    && invoice.OriginalInvoiceId == source.InvoiceNumber,
                invoice => invoice.InvoiceLines);

            var priorLines = previousResult.Data?.SelectMany(invoice => invoice.InvoiceLines ?? Array.Empty<InvoiceLineWriteDto>())
                .Where(line => invoiceIsActiveReturn(line, previousResult.Data))
                .ToList() ?? new List<InvoiceLineWriteDto>();

            _sourceInvoice = source;
            _lines.Clear();
            foreach (var sourceLine in source.InvoiceLines ?? Array.Empty<InvoiceLineWriteDto>())
            {
                if (sourceLine.ProductId <= 0 || sourceLine.ProductUnitId <= 0 || sourceLine.Quantity <= 0)
                    continue;

                var prior = priorLines.Where(line =>
                        sourceLine.Id > 0 && line.SourcePurchaseLineId == sourceLine.Id
                        || sourceLine.Id <= 0 && line.ProductId == sourceLine.ProductId && line.ProductUnitId == sourceLine.ProductUnitId)
                    .ToList();

                var paidReturned = prior.Sum(line => Math.Abs(line.Quantity));
                var freeReturned = prior.Sum(line => line.FreeQuantity);
                _lines.Add(new PurchaseReturnLineRow(sourceLine, paidReturned, freeReturned));
            }

            SelectedInvoiceText.Text = UiText.T($"الفاتورة: {source.InvoiceNumber}", $"Invoice: {source.InvoiceNumber}");
            PaymentMethodText.Text = UiText.T($"الدفع: {GetPaymentName(source.PaymentType)}", $"Payment: {GetPaymentName(source.PaymentType)}");
            SupplierText.Text = UiText.T($"المورد رقم: {source.SupplierId?.ToString() ?? "غير محدد"}", $"Supplier ID: {source.SupplierId?.ToString() ?? "Not specified"}");
            LinesGrid.IsEnabled = _lines.Count > 0;
        }
        catch (Exception ex)
        {
            ShowError(UiText.T($"تعذر تحميل أصناف الفاتورة: {ex.Message}", $"Could not load invoice products: {ex.Message}"));
        }
        finally
        {
            _busy = false;
        }
    }

    private static bool invoiceIsActiveReturn(InvoiceLineWriteDto line, List<InvoiceWriteDto>? invoices)
    {
        var invoice = invoices?.FirstOrDefault(item => item.InvoiceLines?.Contains(line) == true);
        return invoice?.Status is null or InvoiceStatus.Completed or InvoiceStatus.Posted;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _sourceInvoice == null)
            return;

        LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);

        var session = _userSession.CurrentCashierSession;
        if (session == null)
        {
            ShowError(UiText.T("يجب فتح جلسة كاشير قبل حفظ مرتجع المشتريات.", "An open cashier session is required before saving the purchase return."));
            return;
        }

        var selectedLines = _lines.Where(line => line.ReturnedPaid > 0m || line.ReturnedFree > 0m).ToList();
        if (selectedLines.Count == 0)
        {
            ShowError(UiText.T("أدخل كمية مرتجعة واحدة على الأقل.", "Enter at least one returned quantity."));
            return;
        }

        var invalid = selectedLines.FirstOrDefault(line =>
            line.ReturnedPaid > line.RemainingPaid || line.ReturnedFree > line.RemainingFree
            || line.ReturnedPaid < 0m || line.ReturnedFree < 0m);
        if (invalid != null)
        {
            ShowError(UiText.T($"كمية الإرجاع للصنف {invalid.ProductName} تتجاوز الكمية المتبقية.", $"The returned quantity for {invalid.ProductName} exceeds the remaining quantity."));
            return;
        }

        var paymentType = _sourceInvoice.PaymentType ?? PaymentType.Cash;
        var estimatedTotal = EstimateReturnTotal(selectedLines);
        if (paymentType == PaymentType.Check && (_sourceInvoice.Checks == null || _sourceInvoice.Checks.Count == 0))
        {
            ShowError(UiText.T("لا توجد بيانات شيك في الفاتورة الأصلية.", "The original invoice has no check details."));
            return;
        }
        var invoice = new InvoiceWriteDto
        {
            InvoiceNumber = GenerateInvoiceNumber(),
            OriginalInvoiceId = _sourceInvoice.InvoiceNumber,
            InvoiceType = InvoiceType.PurchaseReturn,
            Status = InvoiceStatus.Completed,
            SupplierId = _sourceInvoice.SupplierId,
            PaymentType = paymentType,
            CostPriceIncludesTax = _sourceInvoice.CostPriceIncludesTax,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            CasherId = session.CashierId,
            CashierSessionId = session.Id,
            IsPOS = false,
            InvoiceLines = selectedLines.Select(BuildReturnLine).ToList(),
            Payments = new List<InvoicePaymentWriteDto>(),
            Checks = paymentType == PaymentType.Check ? BuildReturnChecks(estimatedTotal) : new List<CheckWriteDto>()
        };

        try
        {
            _busy = true;
            var result = await _checkoutService.CompleteAsync(new SaleCheckoutRequest
            {
                Invoice = invoice,
                Session = session,
                StockMovementsFactory = savedInvoiceId => BuildStockMovements(invoice, savedInvoiceId, session)
            });

            if (!result.Success)
            {
                ShowError(result.Message ?? UiText.T("فشل حفظ مرتجع المشتريات.", "Could not save the purchase return."));
                return;
            }

            MessageBox.Show(this,
                UiText.T("تم حفظ مرتجع المشتريات وتحديث المخزون والحسابات.", "Purchase return saved; stock and accounting were updated."),
                UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
            _sourceInvoice = null;
            _lines.Clear();
            SelectedInvoiceText.Text = string.Empty;
            PaymentMethodText.Text = string.Empty;
            SupplierText.Text = string.Empty;
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            ShowError(UiText.T($"تعذر حفظ مرتجع المشتريات: {ex.Message}", $"Could not save the purchase return: {ex.Message}"));
        }
        finally
        {
            _busy = false;
        }
    }

    private InvoiceLineWriteDto BuildReturnLine(PurchaseReturnLineRow row)
    {
        var source = row.Source;
        var paidRatio = source.Quantity > 0m ? row.ReturnedPaid / source.Quantity : 0m;
        return new InvoiceLineWriteDto
        {
            SourcePurchaseLineId = source.PurchaseAcquisitionCost.HasValue && source.Id > 0 ? source.Id : null,
            ProductId = source.ProductId,
            ProductName = source.ProductName,
            ProductUnitId = source.ProductUnitId,
            Quantity = -row.ReturnedPaid,
            FreeQuantity = row.ReturnedFree,
            QuantityPerUnitSnapshot = source.QuantityPerUnitSnapshot,
            BaseQuantity = -row.ReturnedPaid * (source.QuantityPerUnitSnapshot > 0m ? source.QuantityPerUnitSnapshot : 1m),
            UnitPrice = source.UnitPrice,
            UnitCost = source.UnitCost,
            PurchaseAcquisitionCost = source.PurchaseAcquisitionCost.HasValue
                ? source.PurchaseAcquisitionCost.Value * paidRatio
                : null,
            TaxExempt = source.TaxExempt,
            TaxRate = source.TaxRate,
            ExpiryDate = source.ExpiryDate,
            LineDiscountAmount = source.LineDiscountAmount * paidRatio,
            LineSubTotal = source.LineSubTotal * paidRatio,
            TaxAmount = source.TaxAmount * paidRatio,
            ProfitBeforeTax = source.ProfitBeforeTax * paidRatio,
            Profit = source.Profit * paidRatio,
            BonusRatio = source.BonusRatio,
            BonusPercentage = source.BonusPercentage,
            OriginalInvoiceId = _sourceInvoice?.InvoiceNumber
        };
    }

    private decimal EstimateReturnTotal(IEnumerable<PurchaseReturnLineRow> lines)
    {
        return Math.Round(lines.Sum(row =>
        {
            var source = row.Source;
            var gross = row.ReturnedPaid * source.UnitPrice;
            var ratio = source.Quantity > 0m ? row.ReturnedPaid / source.Quantity : 0m;
            var discount = source.LineDiscountAmount * ratio;
            var lineTotal = gross - discount;
            var divisor = _sourceInvoice?.CostPriceIncludesTax == true && !source.TaxExempt
                ? 1m + source.TaxRate / 100m
                : 1m;
            var subtotal = source.TaxExempt || divisor <= 0m ? lineTotal : Math.Round(lineTotal / divisor, 3);
            var tax = _sourceInvoice?.CostPriceIncludesTax == true ? Math.Round(lineTotal - subtotal, 3) : 0m;
            return subtotal + tax;
        }), 3);
    }

    private List<CheckWriteDto> BuildReturnChecks(decimal returnTotal)
    {
        var sourceChecks = _sourceInvoice?.Checks?.Where(check => check.Amount > 0m).ToList() ?? new List<CheckWriteDto>();
        var sourceTotal = sourceChecks.Sum(check => check.Amount);
        var result = sourceChecks.Select(check => new CheckWriteDto
        {
            CheckNumber = check.CheckNumber,
            BankName = check.BankName,
            DueDate = check.DueDate,
            Status = CheckStatus.Pending,
            Notes = check.Notes,
            Amount = sourceTotal > 0m ? Math.Round(returnTotal * check.Amount / sourceTotal, 3) : 0m,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        }).ToList();
        if (result.Count > 0)
            result[^1].Amount += returnTotal - result.Sum(check => check.Amount);
        return result;
    }

    private IEnumerable<StockMovementPostDto> BuildStockMovements(InvoiceWriteDto invoice, int invoiceId, RaccoonWarehouse.Domain.Cashiers.DTOs.CashierSessionReadDto session)
    {
        foreach (var line in invoice.InvoiceLines ?? Array.Empty<InvoiceLineWriteDto>())
        {
            var factor = line.QuantityPerUnitSnapshot > 0m ? line.QuantityPerUnitSnapshot : 1m;
            var paid = Math.Abs(line.Quantity);
            var sourceLine = line.SourcePurchaseLineId.HasValue;
            var total = paid + line.FreeQuantity;

            if (total <= 0m)
                continue;

            if (sourceLine)
            {
                yield return BuildMovement(line, invoiceId, session, -total, -total * factor, true);
                continue;
            }

            if (paid > 0m)
                yield return BuildMovement(line, invoiceId, session, -paid, -paid * factor, false);
            if (line.FreeQuantity > 0m)
                yield return BuildMovement(line, invoiceId, session, -line.FreeQuantity, -line.FreeQuantity * factor, false, free: true);
        }
    }

    private static StockMovementPostDto BuildMovement(InvoiceLineWriteDto line, int invoiceId, RaccoonWarehouse.Domain.Cashiers.DTOs.CashierSessionReadDto session, decimal quantity, decimal baseQuantity, bool sourceLine, bool free = false)
        => new()
        {
            ProductId = line.ProductId,
            ProductUnitId = line.ProductUnitId,
            SourcePurchaseLineId = sourceLine ? line.SourcePurchaseLineId : null,
            Quantity = quantity,
            QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot > 0m ? line.QuantityPerUnitSnapshot : 1m,
            BaseQuantity = baseQuantity,
            UnitPrice = free ? 0m : line.UnitPrice,
            PurchasePrice = free ? 0m : line.UnitCost,
            PurchaseAcquisitionCost = sourceLine ? line.PurchaseAcquisitionCost : null,
            ExpiryDate = line.ExpiryDate,
            TransactionType = TransactionType.Return,
            InvoiceId = invoiceId,
            CasherId = session.CashierId,
            CashierSessionId = session.Id,
            TransactionDate = DateTime.Now,
            Notes = $"Purchase return #{invoiceId}"
        };

    private static string GenerateInvoiceNumber() => (DateTime.Now.Ticks % 90000 + 10000).ToString();

    private static string GetPaymentName(PaymentType? paymentType) => paymentType switch
    {
        PaymentType.Cash => UiText.T("نقدي", "Cash"),
        PaymentType.Visa => "Visa",
        PaymentType.Master => "MasterCard",
        PaymentType.Debit => UiText.T("تحويل بنكي", "Bank transfer"),
        PaymentType.Check => UiText.T("شيك", "Check"),
        PaymentType.MobilePayment => UiText.T("دفع إلكتروني", "Mobile payment"),
        PaymentType.Credit => UiText.T("رصيد المورد", "Supplier credit"),
        _ => UiText.T("غير محدد", "Not specified")
    };

    private void ShowError(string message) => MessageBox.Show(this, message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
    private void Close_Click(object sender, RoutedEventArgs e) { if (!_busy) Close(); }

    public sealed class PurchaseReturnRow
    {
        public InvoiceWriteDto Invoice { get; }
        public string InvoiceNumber => Invoice.InvoiceNumber;
        public DateTime CreatedDate => Invoice.CreatedDate;
        public string SupplierName => Invoice.SupplierName ?? "—";
        public string PaymentType => GetPaymentName(Invoice.PaymentType);
        public decimal TotalAmount => Invoice.TotalAmount;
        public string Status => Invoice.Status?.ToString() ?? "Finalized";
        public PurchaseReturnRow(InvoiceWriteDto invoice) => Invoice = invoice;
    }

    public sealed record SupplierFilterOption(int? Id, string Name);

    public sealed class PurchaseReturnLineRow : INotifyPropertyChanged
    {
        private decimal _returnedPaid;
        private decimal _returnedFree;
        public InvoiceLineWriteDto Source { get; }
        public string ProductName => Source.ProductName ?? $"#{Source.ProductId}";
        public decimal OriginalPaid => Source.Quantity;
        public decimal OriginalFree => Source.FreeQuantity;
        public decimal RemainingPaid { get; }
        public decimal RemainingFree { get; }
        public decimal ReturnedPaid { get => _returnedPaid; set { _returnedPaid = value; OnPropertyChanged(); } }
        public decimal ReturnedFree { get => _returnedFree; set { _returnedFree = value; OnPropertyChanged(); } }
        public DateTime? ExpiryDate => Source.ExpiryDate == default ? null : Source.ExpiryDate;

        public PurchaseReturnLineRow(InvoiceLineWriteDto source, decimal paidReturned, decimal freeReturned)
        {
            Source = source;
            RemainingPaid = Math.Max(0m, source.Quantity - paidReturned);
            RemainingFree = Math.Max(0m, source.FreeQuantity - freeReturned);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
