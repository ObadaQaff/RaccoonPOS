using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Insurance;

public partial class CreateInsuranceCompany : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly IAccountingService _accounting;
    private readonly ILoadingService _loading;
    private bool _busy;
    private int _companyId;
    private InsuranceCompanyWriteDto? _original;
    public bool Saved { get; private set; }

    public CreateInsuranceCompany(IInsuranceManagementService service, IAccountingService accounting, ILoadingService loading)
    {
        _service = service;
        _accounting = accounting;
        _loading = loading;

        InitializeComponent();
        AddOption(OrganizationTypeComboBox, InsuranceOrganizationType.Insurer, "شركة تأمين", "Insurer");
        AddOption(OrganizationTypeComboBox, InsuranceOrganizationType.Administrator, "مدير تأمين", "Administrator");
        AddOption(OrganizationTypeComboBox, InsuranceOrganizationType.ContractualPayer, "جهة دفع متعاقدة", "Contractual payer");
        AddOption(OrganizationTypeComboBox, InsuranceOrganizationType.InsurerAndPayer, "شركة تأمين وجهة دفع", "Insurer and payer");
        AddOption(OrganizationTypeComboBox, InsuranceOrganizationType.Other, "أخرى", "Other");
        OrganizationTypeComboBox.SelectedIndex = 0;
        AddOption(StatusComboBox, InsuranceRecordStatus.Active, "نشط", "Active");
        AddOption(StatusComboBox, InsuranceRecordStatus.Draft, "مسودة", "Draft");
        AddOption(StatusComboBox, InsuranceRecordStatus.Inactive, "غير نشط", "Inactive");
        AddOption(StatusComboBox, InsuranceRecordStatus.Expired, "منتهي", "Expired");
        StatusComboBox.SelectedIndex = 0;
        UiText.ApplyWindow(this);
        Loaded += async (_, _) => await LoadEditorAsync();
        Closing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    public void InitializeForEdit(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        _companyId = id;
        Title = FormTitleText.Text = UiText.T("تعديل شركة تأمين", "Edit Insurance Company");
        FormSubtitleText.Text = UiText.T("راجع بيانات جهة التأمين والخصم التعاقدي ثم احفظ التعديلات.", "Review the insurance organization details and contractual discount, then save your changes.");
        ActionHelpText.Text = UiText.T("احفظ التعديلات أو عد إلى الجدول.", "Save your changes or return to the table.");
        SaveButton.Content = UiText.T("حفظ التعديلات", "Save Changes");
    }

    private async Task LoadEditorAsync()
    {
        _busy = true;
        FormGrid.IsEnabled = SaveButton.IsEnabled = BackButton.IsEnabled = false;
        string? error = null;
        try
        {
            _loading.Show();
            await LoadReceivableAccountsAsync();
            if (_companyId == 0)
            {
                CodeTextBox.Focus();
                return;
            }
            var result = await _service.GetCompanyForEditAsync(_companyId);
            if (!result.Success || result.Data == null) error = result.Message;
            else
            {
                _original = result.Data;
                CodeTextBox.Text = _original.Code;
                NameTextBox.Text = _original.Name;
                NameEnglishTextBox.Text = _original.NameEnglish ?? string.Empty;
                TaxNumberTextBox.Text = _original.TaxNumber ?? string.Empty;
                PhoneTextBox.Text = _original.Phone ?? string.Empty;
                EmailTextBox.Text = _original.Email ?? string.Empty;
                AddressTextBox.Text = _original.Address ?? string.Empty;
                NotesTextBox.Text = _original.Notes ?? string.Empty;
                ContractualDiscountTextBox.Text = _original.ContractualDiscountPercent.ToString("0.######", CultureInfo.CurrentCulture);
                OrganizationTypeComboBox.SelectedItem = OrganizationTypeComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => Equals(item.Tag, _original.OrganizationType));
                StatusComboBox.SelectedItem = StatusComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => Equals(item.Tag, _original.Status));
                ReceivableAccountComboBox.SelectedItem = ReceivableAccountComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag is AccountReadDto account && account.Id == _original.ReceivableAccountId);
            }
        }
        catch (Exception ex) { error = ex.Message; }
        finally
        {
            _loading.Hide();
            _busy = false;
            FormGrid.IsEnabled = SaveButton.IsEnabled = BackButton.IsEnabled = true;
        }
        if (_original == null)
        {
            MessageBox.Show(this, CompanyError(error), UiText.T("خطأ", "Error"));
            Close();
        }
        else CodeTextBox.Focus();
    }

    private async Task LoadReceivableAccountsAsync()
    {
        var result = await _accounting.GetAccountsAsync(activeOnly: true);
        if (!result.Success || result.Data == null)
            throw new InvalidOperationException(result.Message ?? "Could not load accounting accounts.");

        ReceivableAccountComboBox.Items.Clear();
        ReceivableAccountComboBox.Items.Add(new ComboBoxItem
        {
            Tag = null,
            Content = UiText.T("تلقائي: إنشاء حساب مخصص", "Automatic: create a dedicated account")
        });
        foreach (var account in result.Data.Where(x => x.IsActive && x.IsPosting && x.AccountType == AccountType.Asset))
        {
            ReceivableAccountComboBox.Items.Add(new ComboBoxItem
            {
                Tag = account,
                Content = $"{account.Code} - {account.Name}"
            });
        }
        ReceivableAccountComboBox.SelectedIndex = 0;
    }

    private static string CompanyError(string? error) => error switch
    {
        "Insurance company was not found." => UiText.T("لم يتم العثور على شركة التأمين.", "Insurance company was not found."),
        _ => error ?? UiText.T("تعذر تحميل شركات التأمين.", "Could not load insurance companies.")
    };

    private static void AddOption<T>(ComboBox combo, T value, string arabic, string english) =>
        combo.Items.Add(new ComboBoxItem { Tag = value, Content = UiText.T(arabic, english) });

    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || (_companyId > 0 && _original == null)) return;
        if (string.IsNullOrWhiteSpace(CodeTextBox.Text) || string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show(this, UiText.T("أدخل رمز شركة التأمين واسمها.", "Enter the insurance company code and name."), UiText.T("خطأ", "Error"));
            return;
        }
        const NumberStyles numberStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign
            | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        if ((!decimal.TryParse(ContractualDiscountTextBox.Text, numberStyle, CultureInfo.CurrentCulture, out var discount)
             && !decimal.TryParse(ContractualDiscountTextBox.Text, numberStyle, CultureInfo.InvariantCulture, out discount))
            || discount is < 0m or > 100m)
        {
            MessageBox.Show(this, UiText.T("أدخل نسبة خصم تعاقدي صحيحة من 0 إلى 100.", "Enter a valid contractual discount percentage from 0 to 100."), UiText.T("خطأ", "Error"));
            ContractualDiscountTextBox.Focus();
            ContractualDiscountTextBox.SelectAll();
            return;
        }
        _busy = true;
        FormGrid.IsEnabled = SaveButton.IsEnabled = BackButton.IsEnabled = false;
        var saved = false;
        string? error = null;
        try
        {
            _loading.Show();
            var selectedAccount = (ReceivableAccountComboBox.SelectedItem as ComboBoxItem)?.Tag as AccountReadDto;
            var result = await _service.SaveCompanyAsync(new InsuranceCompanyWriteDto
            {
                Id = _companyId,
                ReceivableAccountId = selectedAccount?.Id,
                DeductionAccountId = _original?.DeductionAccountId,
                PaymentAccountId = _original?.PaymentAccountId,
                Code = CodeTextBox.Text.Trim(), Name = NameTextBox.Text.Trim(),
                NameEnglish = Optional(NameEnglishTextBox.Text), TaxNumber = Optional(TaxNumberTextBox.Text),
                Phone = Optional(PhoneTextBox.Text), Email = Optional(EmailTextBox.Text),
                Address = Optional(AddressTextBox.Text), Notes = Optional(NotesTextBox.Text),
                ContractualDiscountPercent = discount,
                OrganizationType = (OrganizationTypeComboBox.SelectedItem as ComboBoxItem)?.Tag is InsuranceOrganizationType role
                    ? role : InsuranceOrganizationType.Insurer,
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag is InsuranceRecordStatus status
                    ? status : InsuranceRecordStatus.Active
            });
            saved = result.Success;
            if (!saved)
                error = result.Message == "Insurance company code already exists."
                    ? UiText.T("رمز شركة التأمين مستخدم بالفعل.", "The insurance company code already exists.")
                    : CompanyError(result.Message);
        }
        catch (Exception ex) { error = ex.Message; }
        finally
        {
            _loading.Hide();
            _busy = false;
            FormGrid.IsEnabled = SaveButton.IsEnabled = BackButton.IsEnabled = true;
        }
        if (!saved)
        {
            MessageBox.Show(this, UiText.T("تعذر حفظ شركة التأمين.", "Could not save the insurance company.")
                + Environment.NewLine + error, UiText.T("خطأ", "Error"));
            return;
        }
        Saved = true;
        MessageBox.Show(this, _companyId > 0
            ? UiText.T("تم تحديث شركة التأمين بنجاح.", "Insurance company updated successfully.")
            : UiText.T("تم إنشاء شركة التأمين بنجاح.", "Insurance company created successfully."), UiText.T("تم", "Success"));
        DialogResult = true;
    }

    private void Back_Click(object sender, RoutedEventArgs e) => Close();
}
