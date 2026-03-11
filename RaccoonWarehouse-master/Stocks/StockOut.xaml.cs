using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Helpers.pdf;
using RaccoonWarehouse.Helpers.Pdf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace RaccoonWarehouse.Stocks
{
    public partial class StockOut : Window
    {
        #region Difinition
        private Dictionary<StockItemWriteDto, int> _itemUnits = new();
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockDocumentService _stockDocumentService;
        private readonly IUserService _userService;
        private bool _isLoadingUnits = false;
        public ObservableCollection<ProductUnitWriteDto> GetUnitsForProduct(int productId)
        {
            if (_productUnitsMap.ContainsKey(productId))
                return _productUnitsMap[productId];
            return new ObservableCollection<ProductUnitWriteDto>();
        }
        private Dictionary<int, ObservableCollection<ProductUnitWriteDto>> _productUnitsMap = new();

        public ObservableCollection<StockItemWriteDto> Items { get; set; } = new();
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        public ObservableCollection<ProductUnitWriteDto> Units { get; set; } = new();
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        private int? _currentDocumentId = null;
        private List<StockItemReadDto> _originalItems = new(); // Used for stock adjustment
        #endregion

        #region Constructor
        public StockOut(
            IUserService userService,
            IProductService productService,
            IProductUnitService productUnitService,
            IStockDocumentService stockDocumentService,
            IStockService stockService,
            IStockTransactionService stockTransactionService)
        {
            _userService = userService;
            _stockService = stockService;
            _stockTransactionService = stockTransactionService;
            _productService = productService;
            _productUnitService = productUnitService;
            _stockDocumentService = stockDocumentService;

            InitializeComponent();
            DataContext = this;
            ProductsGrid.ItemsSource = Items;

            this.Loaded += StockOut_Loaded;
        }
        #endregion
        #region Page Load 
        private async void StockOut_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }
        private async Task LoadDataAsync()
        {
            try
            {
                VoucherNumberTxt.Text = GenerateDocumentNumber();
                DatePickerInvoice.SelectedDate = DateTime.Now;

                var stockedProducts = await _stockService.GetAllWithFilteringAndIncludeAsync(
                            s => s.Quantity > 0,
                            new Expression<Func<Stock, object>>[]
                            {
                                s => s.Product,
                                s => s.Product.SubCategory,
                                s => s.Product.Brand,
                                s => s.Product.ProductUnits
                            });


                Products.Clear();

                var distinctProducts = stockedProducts.Data
                    .Where(s => s.Product != null)
                    .GroupBy(s => s.ProductId)
                    .Select(g => g.First().Product!)
                    .OrderBy(p => p.Name)
                    .ToList();

                foreach (var product in distinctProducts)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ");
            }
        }
        #endregion
        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            Items.Add(new StockItemWriteDto
            {

                Quantity = 0,
                PurchasePrice = 0,
                SalePrice = 0,
                ExpiryDate = DateTime.Now.AddMonths(6),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
        }
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockItemWriteDto item)
            {

                Items.Remove(item);
                if (_itemUnits.ContainsKey(item))
                {
                    _itemUnits.Remove(item); // Automatically removes the mapping
                }
            }

        }
        private async void SaveStockOutBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Items.Count == 0)
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate Units
                foreach (var item in Items)
                {
                    if (!_itemUnits.TryGetValue(item, out var unitId) || unitId <= 0)
                    {
                        MessageBox.Show($"الوحدة غير صحيحة للمنتج {item.ProductName ?? "غير معروف"}.");
                        return;
                    }

                    item.ProductUnitId = unitId;
                    await NormalizeStockItemAsync(item);
                }

                bool isUpdate = _currentDocumentId != null;

                // ============= CREATE DTO =============
                var documentDto = new StockDocumentWriteDto
                {
                    Id = _currentDocumentId ?? 0,
                    DocumentNumber = VoucherNumberTxt.Text,
                    Type = StockVoucherType.Out,
                    SupplierId = 1,
                    Notes = NotesTxt.Text,
                    Items = Items.ToList(),
                    CreatedDate = isUpdate ? _originalItems.First().CreatedDate : DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                if (!isUpdate)
                {
                    // ============= CREATE =============
                    var result = await _stockDocumentService.CreateAsync(documentDto);
                    if (result.Success)
                    {
                        var movementResult = await _stockService.PostMovementsAsync(
                            BuildStockMovements(Items, TransactionType.Adjustment, $"Stock out document #{documentDto.DocumentNumber}"));
                        if (!movementResult.Success)
                        {
                            MessageBox.Show(movementResult.Message ?? "فشل تحديث المخزون.", "خطأ");
                            return;
                        }
                        _currentDocumentId = result.Data?.Id;
                    }


                    MessageBox.Show("تم إنشاء السند بنجاح.", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ============= UPDATE =============
                    var result = await _stockDocumentService.UpdateAsync(documentDto);
                    if (result.Success)
                    {
                        // 1️⃣ Return original quantities to stock (reverse)
                        var reverseResult = await _stockService.PostMovementsAsync(
                            BuildStockMovements(_originalItems, TransactionType.Adjustment, $"Reverse stock out document #{documentDto.DocumentNumber}", multiplier: 1m));
                        if (!reverseResult.Success)
                        {
                            MessageBox.Show(reverseResult.Message ?? "فشل عكس حركة المخزون.", "خطأ");
                            return;
                        }
               
                        // 2️⃣ Add new quantities
                        var applyResult = await _stockService.PostMovementsAsync(
                            BuildStockMovements(Items, TransactionType.Adjustment, $"Update stock out document #{documentDto.DocumentNumber}"));
                        if (!applyResult.Success)
                        {
                            MessageBox.Show(applyResult.Message ?? "فشل تحديث حركة المخزون.", "خطأ");
                            return;
                        }
                    }

                    MessageBox.Show("تم تحديث السند بنجاح.", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewStockOutBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearForm()
        {
            VoucherNumberTxt.Text = GenerateDocumentNumber();
            DatePickerInvoice.SelectedDate = DateTime.Now;

            NotesTxt.Text = "";
            Items.Clear();
            _itemUnits.Clear();

            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
            PurchaseBox.Text = "";
            SaleBox.Text = "";
            ExpiryBox.SelectedDate = null;

            ProductsGrid.Items.Refresh();
        }
        private string GenerateDocumentNumber()
        {
            // Example: prefix + current timestamp or sequential number
            string prefix = "DOC";
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{datePart}";
        }
        #region Qty Handle
        private async Task NormalizeStockItemAsync(StockItemWriteDto item)
        {
            if (item.ProductUnitId <= 0)
                return;

            var unitResult = await _productUnitService.GetWriteDtoByIdAsync(item.ProductUnitId);
            var quantityPerUnit = unitResult.Data?.QuantityPerUnit > 0
                ? unitResult.Data.QuantityPerUnit
                : 1m;

            item.QuantityPerUnitSnapshot = quantityPerUnit;
            item.BaseQuantity = item.Quantity * quantityPerUnit;
        }

        private IEnumerable<StockMovementPostDto> BuildStockMovements(
            IEnumerable<StockItemWriteDto> items,
            TransactionType transactionType,
            string notes,
            decimal multiplier = -1m)
        {
            foreach (var item in items)
            {
                yield return new StockMovementPostDto
                {
                    ProductId = item.ProductId,
                    ProductUnitId = item.ProductUnitId,
                    Quantity = item.Quantity * multiplier,
                    QuantityPerUnitSnapshot = item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m,
                    BaseQuantity = item.BaseQuantity * multiplier,
                    UnitPrice = item.SalePrice,
                    TransactionType = transactionType,
                    TransactionDate = DateTime.Now,
                    Notes = notes
                };
            }
        }

        private IEnumerable<StockMovementPostDto> BuildStockMovements(
            IEnumerable<StockItemReadDto> items,
            TransactionType transactionType,
            string notes,
            decimal multiplier = -1m)
        {
            foreach (var item in items)
            {
                var quantityPerUnit = item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m;
                var baseQuantity = item.BaseQuantity != 0 ? item.BaseQuantity : item.Quantity * quantityPerUnit;

                yield return new StockMovementPostDto
                {
                    ProductId = item.ProductId,
                    ProductUnitId = item.ProductUnitId,
                    Quantity = item.Quantity * multiplier,
                    QuantityPerUnitSnapshot = quantityPerUnit,
                    BaseQuantity = baseQuantity * multiplier,
                    UnitPrice = item.SalePrice,
                    TransactionType = transactionType,
                    TransactionDate = DateTime.Now,
                    Notes = notes
                };
            }
        }
        #endregion
        private async void Product_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedValue is int selectedProductId && selectedProductId > 0)
                {
                    // Get the bound row item (StockItemWriteDto)
                    if (cb.DataContext is not StockItemWriteDto item)
                        return;

                    // Cancel any previous unit load operation (prevent thread overlap)
                    // Optional: use a CancellationTokenSource if you already have one

                    item.ProductId = selectedProductId;
                    item.ProductUnitId = 0; // Reset the unit selection

                    // 🌀 Load product units filtered by product ID
                    var unitsResult = await _productUnitService
                        .GetAllWriteDtoWithFilteringAndIncludeAsync(
                            pu => pu.ProductId == selectedProductId,
                            pu => pu.Unit);

                    var availableStocks = await _stockService.GetAllWithFilteringAndIncludeAsync(
                        s => s.ProductId == selectedProductId && s.Quantity > 0);

                    var availableUnitIds = availableStocks.Data
                        .Select(s => s.ProductUnitId)
                        .Distinct()
                        .ToHashSet();

                    var availableUnits = (unitsResult?.Data ?? new List<ProductUnitWriteDto>())
                        .Where(u => availableUnitIds.Contains(u.Id))
                        .ToList();

                    // Update the item's Units collection
                    item.Units.Clear();
                    if (availableUnits.Count > 0)
                    {
                        foreach (var unit in availableUnits)
                            item.Units.Add(unit);
                    }

                    // ✅ Auto-select the first available unit (if any)
                    var defaultUnit = ProductUnitSelector.GetDefaultPurchaseUnit(item.Units);
                    if (defaultUnit != null)
                    {
                        item.ProductUnitId = defaultUnit.Id;
                        item.PurchasePrice = defaultUnit.PurchasePrice;
                        item.SalePrice = defaultUnit.SalePrice;

                        // Map the selected unit for saving
                        _itemUnits[item] = defaultUnit.Id;
                    }

                    // ✅ Set default quantity = 1 if it's 0 or less
                    if (item.Quantity <= 0)
                        item.Quantity = 1;

                    // ✅ Force UI refresh safely on the main thread
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        cb.Items.Refresh();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث الوحدات: {ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        private void ProductComboBox_Loaded(object sender, RoutedEventArgs e)
        {

            /*  if (sender is ComboBox comboBox)
              {
                  if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                  {
                      // Avoid multiple subscriptions
                      textBox.TextChanged -= ProductCombo_TextChanged;
                      textBox.TextChanged += ProductCombo_TextChanged;
                  }
              }*/
        }

        #region SearchAboutProduct Handle
        private void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            string search = ProductBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                ProductBox.ItemsSource = Products;
                ProductBox.IsDropDownOpen = true;
                return;
            }

            var filtered = Products.Where(p =>
                (!string.IsNullOrEmpty(p.Name) &&
                 p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                ||
                p.ITEMCODE.ToString().Contains(search)
            ).ToList();

            ProductBox.ItemsSource = filtered;
            ProductBox.IsDropDownOpen = true;

            if (filtered.Count == 1)
                ProductBox.SelectedItem = filtered.First();
        }

        #endregion

        private void Unit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is StockItemWriteDto item)
            {
                // Get the selected value directly from ComboBox
                if (cb.SelectedValue is int selectedUnitId && selectedUnitId > 0)
                {
                    // Manually set the ProductUnitId
                    item.ProductUnitId = selectedUnitId;
                    item.ProductUnitId = selectedUnitId;
                    _itemUnits[item] = selectedUnitId;  // Map item to its selected unit


                    var unit = item.Units.FirstOrDefault(pu => pu.Id == selectedUnitId);
                    if (unit != null)
                    {
                        item.PurchasePrice = unit.PurchasePrice;
                        item.SalePrice = unit.SalePrice;
                        item.ProductUnitId = unit.Id;
                        ProductsGrid.Items.Refresh();
                    }
                }
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        #region AddProductHandle 


      
        private async void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isLoadingUnits)
                    return;

                if (ProductBox.SelectedValue is not int productId || productId <= 0)
                    return;

                _isLoadingUnits = true;

                // 🔷 Load units from DB
                var unitsResult = await _productUnitService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        pu => pu.ProductId == productId,
                        pu => pu.Unit);

                var availableStocks = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.ProductId == productId && s.Quantity > 0);

                var availableUnitIds = availableStocks.Data
                    .Select(s => s.ProductUnitId)
                    .Distinct()
                    .ToHashSet();

                var availableUnits = (unitsResult?.Data ?? new List<ProductUnitWriteDto>())
                    .Where(u => availableUnitIds.Contains(u.Id))
                    .ToList();

                UnitBox.ItemsSource = availableUnits;

                // 🔷 Auto-select first unit if exists
                var firstUnit = availableUnits.FirstOrDefault();
                if (firstUnit != null)
                {
                    UnitBox.SelectedValue = firstUnit.Id;
                    PurchaseBox.Text = firstUnit.PurchasePrice.ToString();
                    SaleBox.Text = firstUnit.SalePrice.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل الوحدات: {ex.Message}", "خطأ");
            }
            finally
            {
                _isLoadingUnits = false;
            }
        }
        private void ClearProductBtn_Click(object sender, RoutedEventArgs e)
        {
            ProductBox.Text = "";
            ProductBox.SelectedIndex = -1;
            ProductBox.ItemsSource = Products;
            UnitBox.ItemsSource = null;

            PurchaseBox.Text = "";
            SaleBox.Text = "";
            QtyBox.Text = "";
            ExpiryBox.SelectedDate = null;

            ProductBox.IsDropDownOpen = false;
        }

        private void ClearProductInputs()
        {
            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
            PurchaseBox.Text = "";
            SaleBox.Text = "";
            ExpiryBox.SelectedDate = null;
        }

        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProductBox.SelectedItem is not ProductReadDto product)
                {
                    MessageBox.Show("يرجى اختيار منتج.", "تنبيه");
                    return;
                }

                if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
                {
                    MessageBox.Show("يرجى اختيار وحدة.", "تنبيه");
                    return;
                }

                if (!decimal.TryParse(QtyBox.Text, out decimal qty) || qty <= 0)
                {
                    MessageBox.Show("الكمية غير صحيحة.", "تنبيه");
                    return;
                }

                var stockResult = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.ProductId == product.Id && s.ProductUnitId == unit.Id);

                var availableQty = stockResult.Data.FirstOrDefault()?.Quantity ?? 0m;
                if (availableQty <= 0)
                {
                    MessageBox.Show("هذا المنتج/الوحدة غير متوفر بالمخزون حالياً.", "تنبيه");
                    return;
                }

                if (qty > availableQty)
                {
                    MessageBox.Show($"الكمية المطلوبة أكبر من المتوفر. المتوفر: {availableQty}", "تنبيه");
                    return;
                }

                var item = new StockItemWriteDto
                {
                    ProductId = product.Id,
                    ProductUnitId = unit.Id,
                    Quantity = qty,
                    QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m,
                    BaseQuantity = qty * (unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m),
                    PurchasePrice = decimal.TryParse(PurchaseBox.Text, out var p) ? p : 0,
                    SalePrice = decimal.TryParse(SaleBox.Text, out var s) ? s : 0,
                    ExpiryDate = ExpiryBox.SelectedDate ?? DateTime.Now.AddMonths(6),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,

                    // 🔥 Extra fields for DataGrid display
                    ProductName = product.Name,
                    UnitName = unit.Unit.Name
                };

                Items.Add(item);
                _itemUnits[item] = unit.Id;

                ClearProductInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockItemWriteDto item)
            {
                Items.Remove(item);

                if (_itemUnits.ContainsKey(item))
                    _itemUnits.Remove(item);
            }
        }
        


        #endregion

        #region printing



        private void PrintStockOutA4(StockDocumentReadDto dto)
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity,
                TextAlignment = TextAlignment.Right
            };

            // ============ HEADER ================
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            var title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add("سند إخراج بضاعة");
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph(new Run("________________________________________________________")));

            // ============ INFORMATION TABLE ================
            Table infoTable = new Table();
            infoTable.CellSpacing = 10;
            infoTable.Columns.Add(new TableColumn());
            infoTable.Columns.Add(new TableColumn());

            TableRowGroup infoGroup = new TableRowGroup();
            infoTable.RowGroups.Add(infoGroup);

            void AddInfo(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)))
                {
                    FontWeight = FontWeights.Bold
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                infoGroup.Rows.Add(row);
            }

            AddInfo("رقم السند:", dto.DocumentNumber);
            AddInfo("التاريخ:", dto.CreatedDate.ToString("yyyy/MM/dd"));
            AddInfo("نوع السند:", "إخراج بضاعة");
            AddInfo("المستخدم:", dto.Supplier?.Name ?? "-");
            AddInfo("ملاحظات:", dto.Notes ?? "");

            doc.Blocks.Add(infoTable);
            doc.Blocks.Add(new Paragraph(new Run(" ")));

            // ============ ITEMS TABLE ================
            Paragraph itemsHeader = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold,
                FontSize = 20
            };
            itemsHeader.Inlines.Add("تفاصيل المنتجات");
            doc.Blocks.Add(itemsHeader);

            Table itemsTable = new Table();
            itemsTable.CellSpacing = 0;

            string[] headers = { "المنتج", "الوحدة", "الكمية", "سعر الشراء", "سعر البيع", "تاريخ الانتهاء" };

            foreach (var _ in headers)
                itemsTable.Columns.Add(new TableColumn());

            TableRowGroup itemsGroup = new TableRowGroup();
            itemsTable.RowGroups.Add(itemsGroup);

            // HEADER ROW
            TableRow headerRow = new TableRow();
            foreach (var h in headers)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h)))
                {
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(5),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
            }
            itemsGroup.Rows.Add(headerRow);

            // DATA ROWS
            foreach (var item in dto.Items.OrderBy(i => i.Product.Name))
            {
                var row = new TableRow();

                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Product?.Name ?? ""))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductUnit?.Unit?.Name ?? ""))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString()))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.PurchasePrice.ToString("N2")))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.SalePrice.ToString("N2")))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(
                    new Paragraph(
                        new Run(item.ExpiryDate?.ToString("yyyy/MM/dd") ?? "-")
                    ))
                {
                    Padding = new Thickness(5)
                });

                itemsGroup.Rows.Add(row);
            }

            doc.Blocks.Add(itemsTable);

            // ============ FOOTER ================
            doc.Blocks.Add(new Paragraph(new Run("\n________________________________________________________")));

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 0)
            };
            footer.Inlines.Add("توقيع الموظف: ________________________");

            doc.Blocks.Add(footer);

            // PRINT
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = doc;
                dialog.PrintDocument(dps.DocumentPaginator, "Print Stock Out A4");
            }
        }

        private async void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocumentId == null)
                return;

            var doc = await _stockDocumentService.GetFullDocumentByIdAsync(_currentDocumentId.Value);

            if (doc == null)
            {
                MessageBox.Show("السند غير موجود.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {

                SaveStockOutPdf(doc);
                MessageBox.Show("تم إلغاء العملية.", "إلغاء", MessageBoxButton.OK, MessageBoxImage.Information);
                return;

            }


            
        }
        private void SaveStockOutPdf(StockDocumentReadDto doc)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"StockOut_{doc.DocumentNumber}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                // Full path selected by user
                var path = dlg.FileName;

                // Create the PDF
                PdfGenerator.StockOut(doc, path);

                MessageBox.Show("تم حفظ ملف PDF بنجاح.",
                    "تم الحفظ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Open file after saving
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }

        }




        private void NewStockOutBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            PrintBtn.Visibility = Visibility.Collapsed;  // 🔥 Show Print Button
            NewStockOutBtn.Visibility = Visibility.Collapsed;


        }


        #endregion


        #region Search Daialog about stock  


        private void SearchStockBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchStockInWindow(_stockDocumentService, false)
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() == true)
            {
                LoadSelectedStockIn(searchWindow.Result);
            }
        }


        private void LoadSelectedStockIn(StockDocumentReadDto doc)
        {
            ClearForm();

            _currentDocumentId = doc.Id;                 // <-- critical
            _originalItems = doc.Items.ToList();         // <-- for adjusting stock differences

            VoucherNumberTxt.Text = doc.DocumentNumber;
            DatePickerInvoice.SelectedDate = doc.CreatedDate;
            NotesTxt.Text = doc.Notes;

            Items.Clear();
            _itemUnits.Clear();

            foreach (var item in doc.Items)
            {
                var stockItem = new StockItemWriteDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name,
                    ProductUnitId = item.ProductUnitId,
                    UnitName = item.ProductUnit?.Unit?.Name,
                    Quantity = item.Quantity,
                    PurchasePrice = item.PurchasePrice,
                    SalePrice = item.SalePrice,
                    ExpiryDate = item.ExpiryDate,
                    CreatedDate = item.CreatedDate,
                    UpdatedDate = item.UpdatedDate
                };

                Items.Add(stockItem);
                _itemUnits[stockItem] = item.ProductUnitId;
            }

            ProductsGrid.Items.Refresh();
            PrintBtn.Visibility = Visibility.Visible;
            NewStockOutBtn.Visibility = Visibility.Visible;
        }






        #endregion
    }
}






