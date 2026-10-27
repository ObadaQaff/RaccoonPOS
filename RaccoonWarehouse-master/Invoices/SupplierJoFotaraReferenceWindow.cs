using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices;

public sealed class SupplierJoFotaraReferenceWindow : Window
{
    private readonly TextBox _number = new();
    private readonly TextBox _uuid = new();
    private readonly TextBox _qr = new();

    public string? InvoiceNumber { get; private set; }
    public string? InvoiceUuid { get; private set; }
    public string? QrPayload { get; private set; }

    public SupplierJoFotaraReferenceWindow()
    {
        Title = UiText.T("مرجع فاتورة جوافوترة للمورد", "Supplier JoFotara reference");
        Width = 620;
        Height = 430;
        MinWidth = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = UiText.CurrentFlowDirection;
        if (System.Windows.Application.Current.TryFindResource("AppPageBackgroundBrush") is Brush background)
            Background = background;

        var root = new StackPanel { Margin = new Thickness(24) };
        root.Children.Add(new TextBlock
        {
            Text = UiText.T(
                "أدخل بيانات فاتورة جوافوترة التي قدمها المورد. لن يتم إرسال فاتورة الشراء مرة أخرى.",
                "Enter the JoFotara details from the supplier invoice. The purchase invoice will not be submitted again."),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18),
            FontSize = 15
        });

        AddField(root, UiText.T("رقم فاتورة المورد في جوافوترة", "Supplier JoFotara invoice number"), _number);
        AddField(root, UiText.T("المعرف UUID", "UUID"), _uuid);
        AddField(root, UiText.T("قيمة QR أو النص الرسمي", "Official QR value or payload"), _qr);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 18, 0, 0) };
        var save = new Button { Content = UiText.T("حفظ", "Save"), MinWidth = 120, Margin = new Thickness(4) };
        var cancel = new Button { Content = UiText.T("إلغاء", "Cancel"), MinWidth = 120, Margin = new Thickness(4) };
        save.Click += Save_Click;
        cancel.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);
        Content = root;
    }

    public void Initialize(string? supplierName)
    {
        if (!string.IsNullOrWhiteSpace(supplierName))
            Title = string.Format(UiText.T("مرجع جوافوترة للمورد: {0}", "Supplier JoFotara reference: {0}"), supplierName);
    }

    private static void AddField(Panel panel, string label, TextBox textBox)
    {
        panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) });
        textBox.MinHeight = 34;
        textBox.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(textBox);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_number.Text) &&
            string.IsNullOrWhiteSpace(_uuid.Text) &&
            string.IsNullOrWhiteSpace(_qr.Text))
        {
            MessageBox.Show(
                UiText.T("أدخل رقماً أو UUID أو قيمة QR واحدة على الأقل.", "Enter at least an invoice number, UUID, or QR value."),
                UiText.T("بيانات ناقصة", "Missing data"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        InvoiceNumber = TrimOrNull(_number.Text);
        InvoiceUuid = TrimOrNull(_uuid.Text);
        QrPayload = TrimOrNull(_qr.Text);
        DialogResult = true;
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
