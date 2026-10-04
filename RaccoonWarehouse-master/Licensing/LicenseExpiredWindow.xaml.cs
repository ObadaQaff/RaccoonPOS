using System.Diagnostics;
using System.Windows;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Licensing;

public partial class LicenseExpiredWindow : Window
{
    private const string PhoneNumber = "0782495807";
    private const string WhatsAppUrl = "https://wa.me/962782495807";

    public LicenseExpiredWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FlowDirection = UiText.CurrentFlowDirection;
        Title = UiText.T("انتهت الفترة التجريبية", "Trial period expired");
        TitleText.Text = UiText.T("انتهت الفترة التجريبية", "Trial period expired");
        MessageText.Text = UiText.T(
            "انتهت الفترة التجريبية المجانية. يرجى التواصل معنا لتفعيل البرنامج.",
            "The free trial has expired. Please contact us to activate the application.");
        ContactLabelText.Text = UiText.T(
            "للتفعيل تواصل معنا عبر الهاتف أو واتساب",
            "To activate, contact us by phone or WhatsApp");
        WhatsAppButton.Content = UiText.T("واتساب", "WhatsApp");
        PhoneButton.Content = UiText.T("اتصال", "Call");
        ExitButton.Content = UiText.T("إغلاق البرنامج", "Close application");
    }

    private void WhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalUrl(WhatsAppUrl);
    }

    private void PhoneButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalUrl($"tel:{PhoneNumber}");
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private static void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            Clipboard.SetText(PhoneNumber);
        }
    }
}
