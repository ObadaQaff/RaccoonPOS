using System.Windows;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Settings;

public partial class RefundCodeSettingsWindow : Window
{
    private readonly IRefundAuthorizationCodeService _service;
    private readonly App _app;

    public RefundCodeSettingsWindow(IRefundAuthorizationCodeService service)
    {
        
        InitializeComponent();
        _service = service;
        _app = (App)System.Windows.Application.Current;
        Loaded += RefundCodeSettingsWindow_Loaded;
    }

    private async void RefundCodeSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTexts();
        StatusTextBlock.Text = await _service.IsConfiguredAsync()
            ? UiText.T("يوجد رمز مرتجع محفوظ. يمكنك تغييره من هنا.", "A refund code is configured. You can change it here.")
            : UiText.T("لم يتم إعداد رمز مرتجع بعد.", "No refund code has been configured yet.");
        CodeBox.Focus();
    }

    private void ApplyTexts()
    {
        if (_app.IsEnglish)
        {
            Title = "Refund Code Settings";
            TitleTextBlock.Text = "Refund Code Settings";
            DescriptionTextBlock.Text = "Create a dedicated code for approving sales refunds. This code is independent from user passwords and is stored securely.";
            CodeLabelTextBlock.Text = "New refund code";
            ConfirmLabelTextBlock.Text = "Confirm refund code";
            SaveButton.Content = "Save";
            CancelButton.Content = "Cancel";
            return;
        }

        Title = "إعدادات رمز المرتجع";
        TitleTextBlock.Text = "إعدادات رمز المرتجع";
        DescriptionTextBlock.Text = "أنشئ رمزاً مستقلاً لاعتماد مرتجعات البيع. هذا الرمز منفصل عن كلمات مرور المستخدمين ويتم حفظه بشكل آمن.";
        CodeLabelTextBlock.Text = "رمز المرتجع الجديد";
        ConfirmLabelTextBlock.Text = "تأكيد رمز المرتجع";
        SaveButton.Content = "حفظ";
        CancelButton.Content = "إلغاء";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (CodeBox.Password != ConfirmCodeBox.Password)
        {
            MessageBox.Show(UiText.T("تأكيد رمز المرتجع غير مطابق.", "Refund code confirmation does not match."));
            return;
        }

        SaveButton.IsEnabled = false;
        var result = await _service.SetCodeAsync(CodeBox.Password);
        if (!result.Success)
        {
            MessageBox.Show(result.Message, UiText.T("تعذر الحفظ", "Unable to save"), MessageBoxButton.OK, MessageBoxImage.Warning);
            SaveButton.IsEnabled = true;
            return;
        }

        MessageBox.Show(UiText.T("تم حفظ رمز المرتجع.", "Refund code saved."), UiText.T("تم الحفظ", "Saved"), MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
