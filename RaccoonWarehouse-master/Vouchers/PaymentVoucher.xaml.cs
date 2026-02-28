using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Core.Common;
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
    /// <summary>
    /// Interaction logic for PaymentVoucher.xaml
    /// </summary>
    public partial class PaymentVoucher : Window
    {

        private List<CheckWriteDto> _checks = new();
        private readonly IVoucherService _voucherService;
        private readonly IUserService _userService;
        private int? _currentVoucherId = null;
        private List<CheckReadDto> _originalChecks = new();
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;

        public PaymentVoucher(
            IVoucherService voucherService,
            IUserService userService,
            IFinancialTransactionService financialService,
            IUserSession userSession)
        {
            _voucherService = voucherService;
            _userService = userService;
            _financialService = financialService;
            _userSession = userSession;

            InitializeComponent();
            CreateVoucher_Loaded();
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

            // Default date
            ReceiptDate.SelectedDate = DateTime.Now;
            var users = await _userService.GetAllAsync(); 
            AccountComboBox.ItemsSource = users.Data;
            AccountComboBox.DisplayMemberPath = "Name";
            AccountComboBox.SelectedValuePath = "Id";


        }
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(Amount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صالح.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PaymentTypeCombo.SelectedItem is not ComboBoxItem paymentItem)
                {
                    MessageBox.Show("يرجى اختيار طريقة الدفع.", "تنبيه");
                    return;
                }

                if (_userSession?.CurrentCashierSession == null)
                {
                    MessageBox.Show("لا توجد جلسة كاشير مفتوحة. الرجاء تسجيل الدخول من جديد.", "خطأ");
                    return;
                }

                var paymentType = (PaymentType)int.Parse(paymentItem.Tag.ToString());

                // Commit DataGrid edits
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                _checks = ChecksGrid.Items.OfType<CheckWriteDto>().ToList();

                bool isUpdate = _currentVoucherId != null;

                var dto = new VoucherWriteDto
                {
                    VoucherNumber = ReceiptNumber.Text,
                    VoucherType = VoucherType.Payment,
                    Amount = amount,
                    CasherId = _userSession.CurrentCashierSession.CashierId, // ✅
                    Notes = ReceiptDescription.Text,
                    CustomerId = AccountComboBox.SelectedValue != null ? (int)AccountComboBox.SelectedValue : null,
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    PaymentType = paymentType,
                    Checks = paymentType == PaymentType.Check ? _checks.ToList() : null
                };

                if (paymentType == PaymentType.Check && (dto.Checks == null || dto.Checks.Count == 0))
                {
                    MessageBox.Show("يرجى إضافة شيك واحد على الأقل.", "تنبيه");
                    return;
                }

                // =========================
                // 1) Save Voucher (Create / Update)
                // =========================
                Result<VoucherWriteDto> saveResult;

                if (!isUpdate)
                {
                    saveResult = await _voucherService.CreateAsync(dto);
                }
                else
                {
                    dto.Id = _currentVoucherId!.Value;
                    saveResult = await _voucherService.UpdateAsync(dto);
                }

                if (!saveResult.Success)
                {
                    MessageBox.Show(saveResult.Message ?? "فشل حفظ السند", "خطأ");
                    return;
                }

                var savedVoucherId = saveResult.Data.Id;

                // =========================
                // 2) Financial Handling
                // =========================

                if (isUpdate)
                {
                    // ✅ Void old posted financial transactions for this voucher
                    var voidResult = await _financialService.VoidBySourceAsync(
                        FinancialSourceType.PaymentVoucher,
                        savedVoucherId,
                        "Voucher updated"
                    );

                    if (!voidResult.Success)
                    {
                        MessageBox.Show(voidResult.Message ?? "تم تحديث السند لكن فشل إلغاء الحركة المالية السابقة", "تحذير");
                        return;
                    }
                }

                // ✅ Post new transaction (Payment = OUT)
                var postDto = new FinancialPostDto
                {
                    Direction = TransactionDirection.Out,
                    Method = MapPaymentMethod(paymentType),
                    Amount = dto.Amount,
                    TransactionDate = DateTime.Now,

                    SourceType = FinancialSourceType.PaymentVoucher,
                    SourceId = savedVoucherId,

                    CashierSessionId = _userSession.CurrentCashierSession.Id,
                    CashierId = _userSession.CurrentCashierSession.CashierId,

                    Notes = $"Payment Voucher #{dto.VoucherNumber}"
                };

                var postResult = await _financialService.PostAsync(postDto);

                if (!postResult.Success)
                {
                    MessageBox.Show(postResult.Message ?? "تم حفظ السند لكن فشل تسجيل الحركة المالية", "تحذير");
                    return;
                }

                // =========================
                // 3) UI Success
                // =========================
                MessageBox.Show(isUpdate ? "تم تحديث السند وتسجيل الحركة المالية ✅" : "تم حفظ السند وتسجيل الحركة المالية ✅", "نجاح");

                _currentVoucherId = savedVoucherId;
                PrintBtn.Visibility = Visibility.Visible;
                NewVoucherBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ السند:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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


        private void BackBtn_ClickBackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void PaymentTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaymentTypeCombo.SelectedItem is ComboBoxItem selected)
            {
                int paymentType = int.Parse(selected.Tag.ToString());

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
            var tb = (sender as ComboBox).Template.FindName("PART_EditableTextBox", AccountComboBox) as TextBox;
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
            if (string.IsNullOrWhiteSpace(CheckNumberBox.Text))
            {
                MessageBox.Show("يرجى إدخال رقم الشيك.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var check = new CheckWriteDto
            {
                CheckNumber = CheckNumberBox.Text,
                BankName = BankNameBox.Text,
                DueDate = CheckDueDatePicker.SelectedDate ?? DateTime.Now,
                Amount = decimal.Parse(CheckAmountBox.Text), // or add independent check amount textbox
                Notes = CheckNotesBox.Text,
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
        private void DeleteCheck_Click(object sender, RoutedEventArgs e)
        {
            var check = (sender as Button).DataContext as CheckWriteDto;
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
            subHeader.Inlines.Add("سند دفع");
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
                printDialog.PrintDocument(dps.DocumentPaginator, "طباعة سند دفع");
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
            title.Inlines.Add("سند دفع");
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





        private void SavePaymentVoucherPdf(VoucherWriteDto dto)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"PaymentVoucher_{dto.VoucherNumber}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                PdfGenerator.GeneratePaymentVoucherPdf(dto, dlg.FileName);

                MessageBox.Show("تم حفظ ملف PDF بنجاح.",
                    "تم الحفظ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dlg.FileName,
                    UseShellExecute = true
                });
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
            // تأكيد حفظ بيانات الشيكات من الـ DataGrid
            ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

            _checks = ChecksGrid.Items
                                .OfType<CheckWriteDto>()
                                .ToList();

            if (!decimal.TryParse(Amount.Text, out decimal amount))
            {
                MessageBox.Show("يرجى إدخال مبلغ صالح.", "تنبيه");
                return;
            }

            if (PaymentTypeCombo.SelectedItem is not ComboBoxItem paymentItem)
            {
                MessageBox.Show("يرجى اختيار طريقة الدفع.", "تنبيه");
                return;
            }

            var paymentType = (PaymentType)int.Parse(paymentItem.Tag.ToString());

            var dto = new VoucherWriteDto
            {
                Id = _currentVoucherId ?? 0,
                VoucherNumber = ReceiptNumber.Text,
                VoucherType = VoucherType.Payment,
                Amount = amount,
                CasherId = 0,
                Notes = ReceiptDescription.Text,
                CustomerId = AccountComboBox.SelectedValue != null
                                ? (int?)AccountComboBox.SelectedValue
                                : null,
                CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                UpdatedDate = DateTime.Now,
                PaymentType = paymentType,
                Checks = paymentType == PaymentType.Check ? _checks.ToList() : null
            };

            if (paymentType == PaymentType.Check && (dto.Checks == null || dto.Checks.Count == 0))
            {
                MessageBox.Show("يرجى إضافة شيك واحد على الأقل.", "تنبيه");
                return;
            }

         
            SavePaymentVoucherPdf(dto);
        }

        #endregion
        #region search voucher 
        private async void SearchVoucherBtn_Click(object sender, RoutedEventArgs e)
        {
            var search = new SearchVoucherWindow(_voucherService,false);
            if (search.ShowDialog() == true)
            {
                LoadVoucher(search.Result);
            }
        }

        private void LoadVoucher(VoucherReadDto dto)
        {
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

        #endregion
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
