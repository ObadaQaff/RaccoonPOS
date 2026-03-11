using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



namespace RaccoonWarehouse.Vouchers
{
    public partial class CreateVoucher : Window
    {
        private List<CheckWriteDto> _checks = new();
        private readonly  IVoucherService _voucherService;
        private readonly IUserService _userService;
        private int? _currentVoucherId = null;
        private List<CheckReadDto> _originalChecks = new();
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;
        private readonly ICashierSessionService _cashierSessionService;
        private readonly ILoadingService _loadingService;


        public CreateVoucher(IVoucherService voucherService, IUserService userService,
                                     IFinancialTransactionService financialService,
                                     IUserSession userSession,
                                     ILoadingService loadingService)
        {
            _voucherService = voucherService;
            _userService = userService;
            _financialService = financialService;
            _userSession = userSession;
            _loadingService = loadingService;
            InitializeComponent();

            Loaded += async (s, e) => await CreateVoucher_Loaded();
            ReceiptNumber.Text = GenerateDocumentNumber();

        }
        private string GenerateDocumentNumber()
        {
            // Example: prefix + current timestamp or sequential number
            string prefix = "DOC";
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{datePart}";
        }

        private async Task CreateVoucher_Loaded()
        {
            try
            {
                _loadingService.Show();
                ReceiptDate.SelectedDate = DateTime.Now;
                var users = await _userService.GetAllAsync();
                AccountComboBox.ItemsSource = users.Data;
                AccountComboBox.DisplayMemberPath = "Name";
                AccountComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }

        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session != null)
                return true;

            MessageBox.Show("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "خطأ");
            return false;
        }

        /*private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (!decimal.TryParse(Amount.Text, out decimal amount))
                {
                    MessageBox.Show("يرجى إدخال مبلغ صالح.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedUser = AccountComboBox.SelectedItem as UserWriteDto;

                // 🔥 VERY IMPORTANT: Push DataGrid edits into the object
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                // 🔥 Now read checks from DataGrid safely
                if (ChecksGrid.Items.Count > 0)
                {
                    _checks = ChecksGrid.Items
                                        .Cast<object>()
                                        .Where(x => x is CheckWriteDto)
                                        .Cast<CheckWriteDto>()
                                        .ToList();
                }
                bool isUpdate = _currentVoucherId != null;
                var dto = new VoucherWriteDto
                {
                    VoucherNumber = ReceiptNumber.Text,
                    VoucherType = VoucherType.Receipt,
                    Amount = amount,
                    CasherId = 0,
                    Notes = ReceiptDescription.Text,
                    CustomerId = AccountComboBox.SelectedValue != null ? (int)AccountComboBox.SelectedValue : null,
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    // PAYMENT TYPE
                    PaymentType = (PaymentType)int.Parse((PaymentTypeCombo.SelectedItem as ComboBoxItem).Tag.ToString())

                };
                if (dto.PaymentType == PaymentType.Check)
                {
                    dto.Checks = _checks.ToList();
                    if (_checks.Count == 0)
                    {
                        MessageBox.Show("يرجى إضافة شيك واحد على الأقل.", "تنبيه");
                        return;
                    }
                }
                else
                {
                    dto.Checks = null;

                }


                if (!isUpdate)
                {
                    var result = await _voucherService.CreateAsync(dto);

                    if (result.Success)
                    {
                        MessageBox.Show("تم حفظ السند بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        PrintBtn.Visibility = Visibility.Visible;  // 🔥 Show Print Button
                        NewVoucherBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show($"فشل في الحفظ: {result.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    dto.Id = _currentVoucherId.Value;
                    var result = await _voucherService.UpdateAsync(dto);
                    if (result.Success)
                    {
                        MessageBox.Show("تم تحديث السند بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        PrintBtn.Visibility = Visibility.Visible;  // 🔥 Show Print Button
                        NewVoucherBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show($"فشل في التحديث: {result.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ السند:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(Amount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صالح.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PaymentTypeCombo.SelectedItem is not ComboBoxItem payItem || payItem.Tag == null)
                {
                    MessageBox.Show("يرجى اختيار طريقة الدفع.", "تنبيه");
                    return;
                }

                var paymentType = (PaymentType)int.Parse(payItem.Tag.ToString()!);

                // Commit edits for checks
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                if (paymentType == PaymentType.Check)
                {
                    _checks = ChecksGrid.Items
                        .OfType<CheckWriteDto>()
                        .ToList();

                    if (_checks.Count == 0)
                    {
                        MessageBox.Show("يرجى إضافة شيك واحد على الأقل.", "تنبيه");
                        return;
                    }
                }
                else
                {
                    _checks.Clear();
                }

                bool isUpdate = _currentVoucherId != null;
                if (!TryGetActiveCashierSession(out var session))
                    return;
                _loadingService.Show();

                var dto = new VoucherWriteDto
                {
                    VoucherNumber = ReceiptNumber.Text,
                    VoucherType = VoucherType.Receipt, // أو حسب شاشتك (Receipt/Payment)
                    Amount = amount,
                    CasherId = session.CashierId,
                    Notes = ReceiptDescription.Text,
                    CustomerId = AccountComboBox.SelectedValue != null ? (int)AccountComboBox.SelectedValue : null,
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    PaymentType = paymentType,
                    Checks = paymentType == PaymentType.Check ? _checks.ToList() : null
                };

                // =========================
                // 1) Save Voucher (Create/Update)
                // =========================
                int savedVoucherId;

                if (!isUpdate)
                {
                    var createResult = await _voucherService.CreateAsync(dto);
                    if (!createResult.Success)
                    {
                        MessageBox.Show($"فشل في الحفظ: {createResult.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    savedVoucherId = createResult.Data.Id; // تأكد CreateAsync بيرجع Id في Data
                    _currentVoucherId = savedVoucherId;
                }
                else
                {
                    dto.Id = _currentVoucherId!.Value;

                    var updateResult = await _voucherService.UpdateAsync(dto);
                    if (!updateResult.Success)
                    {
                        MessageBox.Show($"فشل في التحديث: {updateResult.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    savedVoucherId = dto.Id;
                }
                
                if (paymentType == PaymentType.Check && !ValidatePaymentByCheck(dto.Amount))
                    return;

                // =========================
                // 2) Financial handling
                // =========================
                var sourceType = GetSourceType(dto.VoucherType);
                var direction = GetDirection(dto.VoucherType);
                var method = MapPaymentMethod(dto.PaymentType);

                // Update case: void old financial then post new
                if (isUpdate)
                {
                    var voidRes = await _financialService.VoidBySourceAsync(
                        sourceType,
                        savedVoucherId,
                        reason: $"Voucher updated #{dto.VoucherNumber}"
                    );

                    // حتى لو ما لقى حركات قديمة، ما تعتبرها فشل
                    if (!voidRes.Success)
                    {
                        MessageBox.Show(voidRes.Message ?? "فشل في إلغاء الحركات القديمة.", "خطأ");
                        return;
                    }
                }

                var postDto = new FinancialPostDto
                {
                    Direction = direction,
                    Method = method,
                    Amount = dto.Amount,
                    TransactionDate = dto.CreatedDate,

                    SourceType = sourceType,
                    SourceId = savedVoucherId,

                    CashierSessionId = session.Id,
                    CashierId = session.CashierId,

                    Notes = $"{dto.VoucherType} Voucher #{dto.VoucherNumber}"
                };

                // مهم: إذا الطريقة Cash لازم SessionId مش null (حسب validations عندك)
                var postRes = await _financialService.PostAsync(postDto);
                if (!postRes.Success)
                {
                    MessageBox.Show(postRes.Message ?? "تم حفظ السند لكن فشل تسجيل الحركة المالية", "تحذير");
                    return;
                }

                // =========================
                // 3) UI
                // =========================
                MessageBox.Show(isUpdate ? "تم تحديث السند وتسجيل الحركة المالية ✅" : "تم حفظ السند وتسجيل الحركة المالية ✅", "نجاح");
                PrintBtn.Visibility = Visibility.Visible;
                NewVoucherBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ السند:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void NewVoucherBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();


            // Hide buttons after clearing
            PrintBtn.Visibility = Visibility.Collapsed;
            NewVoucherBtn.Visibility = Visibility.Collapsed;
        }

        private void ClearFields()
        {
            // Reset voucher fields
            ReceiptNumber.Text = GenerateDocumentNumber();
            Amount.Text = string.Empty;
            AccountComboBox.SelectedIndex = -1;
            ReceiptDescription.Text = string.Empty;
            ReceiptDate.SelectedDate = DateTime.Now;

            // Reset payment method
            PaymentTypeCombo.SelectedIndex = -1;

            // Clear check input fields
            CheckNumberBox.Text = string.Empty;
            BankNameBox.Text = string.Empty;
            CheckAmountBox.Text = string.Empty;
            CheckNotesBox.Text = string.Empty;
            CheckDueDatePicker.SelectedDate = null;

            // Clear check list
            _checks.Clear();

            // Clear DataGrid
            ChecksGrid.ItemsSource = null;

            // Hide check UI
            CheckFieldsPanel.Visibility = Visibility.Collapsed;
            ChecksGrid.Visibility = Visibility.Collapsed;
            AddCheckButton.Visibility = Visibility.Collapsed;
        }


        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void PaymentTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaymentTypeCombo.SelectedItem is ComboBoxItem selected)
            {
                if (selected.Tag == null || !int.TryParse(selected.Tag.ToString(), out int paymentType))
                    return;

                // Show check fields only if user selected "Check = 4"
                ChecksGrid.Visibility = (paymentType == (int)PaymentType.Check)
                                              ? Visibility.Visible
                                              : Visibility.Collapsed;
                AddCheckButton.Visibility = (paymentType == (int)PaymentType.Check)
                                              ? Visibility.Visible
                                              : Visibility.Collapsed;
                CheckFieldsPanel.Visibility = (paymentType == (int)PaymentType.Check)
                                              ? Visibility.Visible
                                              : Visibility.Collapsed;

            }
        }

        private async void AccountComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox)
                return;

            var tb = comboBox.Template.FindName("PART_EditableTextBox", AccountComboBox) as TextBox;
            if (tb == null)
                return;

            string searchText = tb.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                AccountComboBox.ItemsSource = (await _userService.GetAllAsync()).Data;
                return;
            }

            var users = (await _userService.GetAllAsync()).Data;
            AccountComboBox.ItemsSource = users.Where(u => u.Name.ToLower().Contains(searchText)).ToList();
            AccountComboBox.IsDropDownOpen = true;  // keep list open
        }
        #region Check Handle 
        private void AddCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CheckNumberBox.Text))
                {
                    MessageBox.Show("يرجى إدخال رقم الشيك.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(CheckAmountBox.Text, out var checkAmount) || checkAmount <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ شيك صالح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var checkNumber = CheckNumberBox.Text.Trim();
                if (_checks.Any(c => string.Equals(c.CheckNumber, checkNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("رقم الشيك مكرر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var check = new CheckWriteDto
                {
                    CheckNumber = checkNumber,
                    BankName = string.IsNullOrWhiteSpace(BankNameBox.Text) ? "-" : BankNameBox.Text.Trim(),
                    DueDate = CheckDueDatePicker.SelectedDate ?? DateTime.Now,
                    Amount = checkAmount,
                    Notes = string.IsNullOrWhiteSpace(CheckNotesBox.Text) ? null : CheckNotesBox.Text.Trim(),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _checks.Add(check);

                ChecksGrid.ItemsSource = null;
                ChecksGrid.ItemsSource = _checks;

                ChecksGrid.Visibility = Visibility.Visible;

                // Clear input fields
                CheckNumberBox.Text = "";
                BankNameBox.Text = "";
                CheckAmountBox.Text = "";
                CheckNotesBox.Text = "";
                CheckDueDatePicker.SelectedDate = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة الشيك:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteCheck_Click(object sender, RoutedEventArgs e)
        {
            var check = (sender as Button).DataContext as CheckWriteDto;
            if (check == null)
                return;

            _checks.Remove(check);

            ChecksGrid.ItemsSource = null;
            ChecksGrid.ItemsSource = _checks;

            if (_checks.Count == 0)
                ChecksGrid.Visibility = Visibility.Collapsed;
        }

        #endregion
        #region Print handle 
        private void PrintVoucher(VoucherWriteDto dto)
        {
            // Create FlowDocument
            var doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                PagePadding = new Thickness(40),
                ColumnWidth = double.PositiveInfinity // single column
            };

            // ==== HEADER ====
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            var subHeader = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            subHeader.Inlines.Add("سند قبض");
            doc.Blocks.Add(subHeader);

            doc.Blocks.Add(new Paragraph(new Run("────────────────────────────────────────")));

            // ==== BASIC INFO TABLE ====
            var infoTable = new Table();
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
            infoTable.Columns.Add(new TableColumn());

            var infoRowGroup = new TableRowGroup();
            infoTable.RowGroups.Add(infoRowGroup);

            void AddInfoRow(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(value ?? ""))));
                infoRowGroup.Rows.Add(row);
            }

            AddInfoRow("رقم السند:", ReceiptNumber.Text);
            AddInfoRow("التاريخ:", (dto.CreatedDate).ToString("yyyy/MM/dd"));
            AddInfoRow("العميل / الجهة:", (AccountComboBox.Text ?? ""));
            AddInfoRow("المبلغ:", dto.Amount.ToString("N2"));
            AddInfoRow("طريقة الدفع:", dto.PaymentType.ToString());

            doc.Blocks.Add(infoTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")));// spacer
            doc.Blocks.Add(new Paragraph(new Run("تفاصيل الشيكات:"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            });

            // ==== CHECKS TABLE (IF ANY) ====
            if (dto.Checks != null && dto.Checks.Count > 0)
            {
                var checkTable = new Table();
                checkTable.CellSpacing = 0;
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(120) }); // check number
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(120) }); // bank
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(80) });  // amount
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // due date
                checkTable.Columns.Add(new TableColumn());                               // notes

                var checkHeaderGroup = new TableRowGroup();
                checkTable.RowGroups.Add(checkHeaderGroup);

                // Header row
                var headerRow = new TableRow();
                string[] headers = { "رقم الشيك", "البنك", "المبلغ", "تاريخ الاستحقاق", "ملاحظات" };
                foreach (var h in headers)
                {
                    var cell = new TableCell(new Paragraph(new Run(h)))
                    {
                        FontWeight = FontWeights.Bold,
                        Padding = new Thickness(3),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    headerRow.Cells.Add(cell);
                }
                checkHeaderGroup.Rows.Add(headerRow);

                // Data rows
                foreach (var c in dto.Checks)
                {
                    var row = new TableRow();

                    TableCell MakeCell(string text)
                    {
                        return new TableCell(new Paragraph(new Run(text ?? "")))
                        {
                            Padding = new Thickness(3),
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(0, 0, 0, 0.5)
                        };
                    }

                    row.Cells.Add(MakeCell(c.CheckNumber));
                    row.Cells.Add(MakeCell(c.BankName));
                    row.Cells.Add(MakeCell(c.Amount.ToString("N2")));
                    row.Cells.Add(MakeCell(c.DueDate.ToString("yyyy/MM/dd")));
                    row.Cells.Add(MakeCell(c.Notes));

                    checkHeaderGroup.Rows.Add(row);
                }

                doc.Blocks.Add(checkTable);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("لا يوجد شيكات.")));
            }

            // ==== NOTES ====
            doc.Blocks.Add(new Paragraph(new Run(" ")));// spacer
            doc.Blocks.Add(new Paragraph(new Run("ملاحظات:"))
            {
                FontWeight = FontWeights.Bold
            });
            doc.Blocks.Add(new Paragraph(new Run(dto.Notes ?? "")));

            doc.Blocks.Add(new Paragraph(new Run("────────────────────────────────────────")));
            doc.Blocks.Add(new Paragraph(new Run("شكراً لتعاملكم"))
            {
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic
            });

            // ==== PRINT DIALOG ====
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = doc;
                printDialog.PrintDocument(dps.DocumentPaginator, "طباعة سند قبض");
            }
        }

        private void PrintVoucherA4(VoucherWriteDto dto)
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity
            };

            // HEADER
            Paragraph header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            Paragraph title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add("سند قبض");
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            // BASIC INFORMATION TABLE
            Table infoTable = new Table();
            infoTable.CellSpacing = 10;
            infoTable.Columns.Add(new TableColumn());
            infoTable.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            infoTable.RowGroups.Add(group);

            void AddInfo(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                group.Rows.Add(row);
            }

            AddInfo("رقم السند:", dto.Id.ToString());
            AddInfo("التاريخ:", dto.CreatedDate.ToString("yyyy/MM/dd"));
            AddInfo("العميل:", AccountComboBox.Text);
            AddInfo("طريقة الدفع:", dto.PaymentType.ToString());
            AddInfo("المبلغ:", dto.Amount.ToString("N2"));

            doc.Blocks.Add(infoTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")));


            // CHECKS SECTION
            Paragraph checkTitle = new Paragraph(new Run("تفاصيل الشيكات"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 20
            };
            doc.Blocks.Add(checkTitle);

            if (dto.Checks != null && dto.Checks.Count > 0)
            {
                Table tbl = new Table();
                tbl.CellSpacing = 0;

                string[] headers = { "رقم الشيك", "البنك", "المبلغ", "تاريخ الاستحقاق", "ملاحظات" };

                foreach (var _ in headers)
                    tbl.Columns.Add(new TableColumn());

                TableRowGroup tgroup = new TableRowGroup();
                tbl.RowGroups.Add(tgroup);

                // Header row
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
                tgroup.Rows.Add(headerRow);

                // Data rows
                foreach (var ch in dto.Checks)
                {
                    TableRow r = new TableRow();
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.CheckNumber))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.BankName))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Amount.ToString("N2")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.DueDate.ToString("yyyy/MM/dd")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Notes ?? ""))) { Padding = new Thickness(5) });

                    tgroup.Rows.Add(r);
                }

                doc.Blocks.Add(tbl);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("لا يوجد شيكات.")));
            }

            doc.Blocks.Add(new Paragraph(new Run(" ")));
            doc.Blocks.Add(new Paragraph(new Run("ملاحظات:")) { FontWeight = FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run(dto.Notes ?? "")));

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            footer.Inlines.Add("توقيع الموظف: ________________________");
            doc.Blocks.Add(footer);

            // PRINT
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = doc;
                dialog.PrintDocument(dps.DocumentPaginator, "Print Voucher A4");
            }
        }



        private FlowDocument BuildVoucherA4Document(VoucherWriteDto dto)
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity
            };

            // ---- HEADER ----
            Paragraph header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            Paragraph title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add("سند قبض");
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            // BASIC INFO
            Table infoTable = new Table();
            infoTable.CellSpacing = 10;
            infoTable.Columns.Add(new TableColumn());
            infoTable.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            infoTable.RowGroups.Add(group);

            void AddInfo(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                group.Rows.Add(row);
            }

            AddInfo("رقم السند:", dto.Id.ToString());
            AddInfo("التاريخ:", dto.CreatedDate.ToString("yyyy/MM/dd"));
            AddInfo("العميل:", AccountComboBox.Text);
            AddInfo("طريقة الدفع:", dto.PaymentType.ToString());
            AddInfo("المبلغ:", dto.Amount.ToString("N2"));

            doc.Blocks.Add(infoTable);
            doc.Blocks.Add(new Paragraph(new Run(" ")));

            // CHECKS TABLE
            Paragraph checkTitle = new Paragraph(new Run("تفاصيل الشيكات"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 20
            };
            doc.Blocks.Add(checkTitle);

            if (dto.Checks != null && dto.Checks.Count > 0)
            {
                Table tbl = new Table();
                tbl.CellSpacing = 0;

                string[] headers = { "رقم الشيك", "البنك", "المبلغ", "تاريخ الاستحقاق", "ملاحظات" };
                foreach (var _ in headers)
                    tbl.Columns.Add(new TableColumn());

                TableRowGroup tgroup = new TableRowGroup();
                tbl.RowGroups.Add(tgroup);

                // Header row
                TableRow hr = new TableRow();
                foreach (var h in headers)
                {
                    hr.Cells.Add(new TableCell(new Paragraph(new Run(h)))
                    {
                        FontWeight = FontWeights.Bold,
                        Padding = new Thickness(5),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, 0, 2)
                    });
                }
                tgroup.Rows.Add(hr);

                // Data rows
                foreach (var ch in dto.Checks)
                {
                    TableRow r = new TableRow();
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.CheckNumber))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.BankName))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Amount.ToString("N2")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.DueDate.ToString("yyyy/MM/dd")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Notes ?? ""))) { Padding = new Thickness(5) });

                    tgroup.Rows.Add(r);
                }

                doc.Blocks.Add(tbl);
            }

            // NOTES
            doc.Blocks.Add(new Paragraph(new Run(" ")));
            doc.Blocks.Add(new Paragraph(new Run("ملاحظات:")) { FontWeight = FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run(dto.Notes ?? "")));

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            footer.Inlines.Add("توقيع الموظف: ________________________");
            doc.Blocks.Add(footer);

            return doc;
        }
        private FixedDocument ConvertFlowDocumentToFixed(FlowDocument flowDoc)
        {
            DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDoc).DocumentPaginator;

            paginator.PageSize = new Size(793, 1122); // A4 size

            FixedDocument fixedDoc = new FixedDocument();

            for (int i = 0; i < paginator.PageCount; i++)
            {
                DocumentPage page = paginator.GetPage(i);

                FixedPage fixedPage = new FixedPage();
                fixedPage.Width = paginator.PageSize.Width;
                fixedPage.Height = paginator.PageSize.Height;

                // WRAP VISUAL INSIDE RECTANGLE → UIElement
                Rectangle rect = new Rectangle
                {
                    Width = paginator.PageSize.Width,
                    Height = paginator.PageSize.Height,
                    Fill = new VisualBrush(page.Visual)
                };

                // add rectangle to page
                fixedPage.Children.Add(rect);

                PageContent pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);

                fixedDoc.Pages.Add(pageContent);
            }

            return fixedDoc;
        }
      
        private void ForceRenderDocument(FlowDocument doc)
        {
            // Create hidden RichTextBox to render the document
            RichTextBox rtb = new RichTextBox();
            rtb.Document = doc;
            rtb.Width = 800;
            rtb.Height = 1122;

            // Force layout pass
            rtb.Measure(new Size(800, 1122));
            rtb.Arrange(new Rect(new Size(800, 1122)));
            rtb.UpdateLayout();
        }


        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Commit DataGrid edits
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                _checks = ChecksGrid.Items
                                    .OfType<CheckWriteDto>()
                                    .ToList();

                if (!decimal.TryParse(Amount.Text, out decimal amount))
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح.", "خطأ");
                    return;
                }

                if (PaymentTypeCombo.SelectedItem is not ComboBoxItem paymentItem || paymentItem.Tag == null)
                {
                    MessageBox.Show("يرجى اختيار طريقة الدفع.", "تنبيه");
                    return;
                }

                var paymentType = (PaymentType)int.Parse(paymentItem.Tag.ToString()!);
                var dto = new VoucherWriteDto
                {
                    Id = _currentVoucherId ?? 0,
                    VoucherNumber = ReceiptNumber.Text,
                    Amount = amount,
                    Notes = string.IsNullOrWhiteSpace(ReceiptDescription.Text) ? null : ReceiptDescription.Text.Trim(),
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    VoucherType = VoucherType.Receipt,
                    PaymentType = paymentType,
                    CustomerId = AccountComboBox.SelectedValue != null ? (int?)AccountComboBox.SelectedValue : null,
                    Checks = paymentType == PaymentType.Check ? _checks : null
                };

                if (paymentType == PaymentType.Check && !ValidatePaymentByCheck(amount))
                    return;

                PrintVoucherPdf(dto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الطباعة:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintVoucherPdf(VoucherWriteDto dto)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"Voucher_{dto.VoucherNumber}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                PdfGenerator.GenerateVoucherPdf(dto, dlg.FileName);

                MessageBox.Show("تم حفظ ملف PDF بنجاح.", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dlg.FileName,
                    UseShellExecute = true
                });
            }
        }

        #endregion
        #region search voucher 
        private async void SearchVoucherBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var search = new SearchVoucherWindow(_voucherService, true);
                if (search.ShowDialog() == true && search.Result != null)
                {
                    LoadVoucher(search.Result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء البحث عن السند:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVoucher(VoucherReadDto dto)
        {
            if (dto == null)
                return;

            _currentVoucherId = dto.Id;
            _originalChecks = dto.Checks?.ToList() ?? new();

            ReceiptNumber.Text = dto.VoucherNumber;
            ReceiptDate.SelectedDate = dto.CreatedDate;
            Amount.Text = dto.Amount.ToString();
            ReceiptDescription.Text = dto.Notes;

            AccountComboBox.SelectedValue = dto.CustomerId;

            PaymentTypeCombo.SelectedIndex = (int)dto.PaymentType - 1;

            _checks = dto.Checks?.Select(c => new CheckWriteDto
            {
                Id = c.Id,
                BankName = c.BankName,
                CheckNumber = c.CheckNumber,
                Amount = c.Amount,
                Notes = c.Notes,
                DueDate = c.DueDate
            }).ToList() ?? new();

            ChecksGrid.ItemsSource = _checks;
            ChecksGrid.Visibility = dto.PaymentType == PaymentType.Check ? Visibility.Visible : Visibility.Collapsed;
            AddCheckButton.Visibility = dto.PaymentType == PaymentType.Check ? Visibility.Visible : Visibility.Collapsed;

            PrintBtn.Visibility = Visibility.Visible;
            NewVoucherBtn.Visibility = Visibility.Visible;
        }
        #endregion
        #region payment method handle 
        private PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Visa => PaymentMethod.Visa,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }

        private FinancialSourceType GetSourceType(VoucherType voucherType)
        {
            return voucherType switch
            {
                VoucherType.Receipt => FinancialSourceType.ReceiptVoucher,
                VoucherType.Payment => FinancialSourceType.PaymentVoucher,
                _ => FinancialSourceType.Manual
            };
        }

        private TransactionDirection GetDirection(VoucherType voucherType)
        {
            // سند قبض = In ، سند صرف = Out
            return voucherType switch
            {
                VoucherType.Receipt => TransactionDirection.In,
                VoucherType.Payment => TransactionDirection.Out,
                _ => TransactionDirection.In
            };
        }

        private bool ValidatePaymentByCheck(decimal voucherAmount)
        {
            if (_checks.Count == 0)
            {
                MessageBox.Show("يرجى إضافة شيك واحد على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_checks.Any(c => string.IsNullOrWhiteSpace(c.CheckNumber) || c.Amount <= 0))
            {
                MessageBox.Show("بيانات الشيك غير مكتملة أو تحتوي مبالغ غير صالحة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var duplicateCheck = _checks
                .Where(c => !string.IsNullOrWhiteSpace(c.CheckNumber))
                .GroupBy(c => c.CheckNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (duplicateCheck)
            {
                MessageBox.Show("لا يمكن تكرار رقم الشيك داخل نفس السند.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var totalChecks = _checks.Sum(c => c.Amount);
            if (totalChecks != voucherAmount)
            {
                MessageBox.Show("مجموع مبالغ الشيكات يجب أن يساوي مبلغ السند.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        #endregion


    }
}
