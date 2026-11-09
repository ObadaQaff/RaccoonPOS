using System.Windows;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Settings;

public partial class JordanEInvoiceSettingsWindow : Window
{
    private readonly IJordanEInvoiceSettingsService _service;
    private readonly App _app;

    public JordanEInvoiceSettingsWindow(IJordanEInvoiceSettingsService service)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent();
        _service = service;
        _app = (App)System.Windows.Application.Current;
        Loaded += JordanEInvoiceSettingsWindow_Loaded;
    }

    private async void JordanEInvoiceSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTexts();
        var settings = await _service.GetAsync();
        EnabledCheckBox.IsChecked = settings.Enabled;
        ClientIdTextBox.Text = settings.ClientId;
        SecretKeyBox.Password = settings.SecretKey;
        IncomeSourceSequenceTextBox.Text = settings.IncomeSourceSequence;
        TaxRegistrationTypeTextBox.Text = settings.TaxRegistrationType;
        StatusTextBlock.Text = settings.Enabled
            ? UiText.T("تم تفعيل الإرسال لفواتير البيع فقط.", "JoFotara submission is enabled for POS sales only.")
            : UiText.T("جوافوترة غير مفعلة حالياً.", "JoFotara is currently disabled.");
    }

    private void ApplyTexts()
    {
        TaxRegistrationLabelTextBlock.Text = _app.IsEnglish
            ? "Tax Registration Type (1 Income, 2 General Sales Tax, 3 Special Sales Tax)"
            : "نوع التسجيل الضريبي (1 دخل، 2 ضريبة مبيعات عامة، 3 ضريبة خاصة)";

        if (_app.IsEnglish)
        {
            Title = "JoFotara Settings";
            TitleTextBlock.Text = "JoFotara Settings";
            DescriptionTextBlock.Text = "Configure the Jordan national e-invoicing connection. It is used for POS sales only; purchase invoices are not submitted.";
            EnabledCheckBox.Content = "Enable JoFotara for POS sales";
            ClientIdLabelTextBlock.Text = "Client ID / User Number";
            SecretKeyLabelTextBlock.Text = "Secret Key";
            IncomeSourceLabelTextBlock.Text = "Income Source Sequence";
            SaveButton.Content = "Save";
            CancelButton.Content = "Cancel";
            return;
        }

        Title = "إعدادات جوافوترة";
        TitleTextBlock.Text = "إعدادات جوافوترة";
        DescriptionTextBlock.Text = "إعداد اتصال نظام الفوترة الوطني الأردني. يستخدم لفواتير البيع في نقطة البيع فقط، ولا يتم إرسال فواتير الشراء.";
        EnabledCheckBox.Content = "تفعيل جوافوترة لفواتير البيع";
        ClientIdLabelTextBlock.Text = "رقم المستخدم / Client ID";
        SecretKeyLabelTextBlock.Text = "المفتاح السري";
        IncomeSourceLabelTextBlock.Text = "تسلسل مصدر الدخل";
        SaveButton.Content = "حفظ";
        CancelButton.Content = "إلغاء";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        var result = await _service.SaveAsync(new JordanEInvoiceSettingsDto(
            EnabledCheckBox.IsChecked == true,
            ClientIdTextBox.Text,
            SecretKeyBox.Password,
            IncomeSourceSequenceTextBox.Text,
            TaxRegistrationTypeTextBox.Text));

        if (!result.Success)
        {
            MessageBox.Show(result.Message, UiText.T("تعذر الحفظ", "Unable to save"), MessageBoxButton.OK, MessageBoxImage.Warning);
            SaveButton.IsEnabled = true;
            return;
        }

        MessageBox.Show(UiText.T("تم حفظ إعدادات جوافوترة.", "JoFotara settings saved."), UiText.T("تم الحفظ", "Saved"), MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
