using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.POS
{
    public partial class ResumeHeldInvoiceWindow : Window
    {
        private static readonly Dictionary<string, string> HeldColors = new()
        {
            ["#D9F2E6"] = "Mint / نعناعي",
            ["#D9EAF7"] = "Blue / أزرق",
            ["#FFF0C2"] = "Yellow / أصفر",
            ["#FADADD"] = "Pink / زهري",
            ["#E6D9F7"] = "Purple / بنفسجي",
            ["#F7DEC6"] = "Orange / برتقالي"
        };

        private readonly IInvoiceService _invoiceService;

        public InvoiceReadDto? SelectedInvoice { get; private set; }

        public static IReadOnlyList<string> AvailableColorPalette => HeldColors.Keys.ToList();

        public static string GetColorName(string color) => HeldColors.TryGetValue(color, out var name) ? name : color;

        public static string GetTextColor(string color)
        {
            try
            {
                var parsed = (Color)ColorConverter.ConvertFromString(color)!;
                var brightness = (parsed.R * 299 + parsed.G * 587 + parsed.B * 114) / 1000;
                return brightness < 150 ? "#FFFFFF" : "#1F2937";
            }
            catch
            {
                return "#1F2937";
            }
        }

        public static string? ChooseAvailableColor(Window owner, IEnumerable<InvoiceReadDto> heldInvoices, int currentInvoiceId = 0)
        {
            var occupied = heldInvoices
                .Where(invoice => invoice.Id != currentInvoiceId && !string.IsNullOrWhiteSpace(invoice.HeldColor))
                .Select(invoice => invoice.HeldColor!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var dialog = new HoldInvoiceColorWindow(AvailableColorPalette, occupied)
            {
                Owner = owner
            };

            return dialog.ShowDialog() == true ? dialog.SelectedColor : null;
        }

        public ResumeHeldInvoiceWindow(IInvoiceService invoiceService)
        {
            InitializeComponent();
            _invoiceService = invoiceService;
            UiText.ApplyWindow(this);
            ResumeHeldInvoiceButton.Content = UiText.T("استئناف", "Resume");
            DeleteHeldInvoiceButton.Content = UiText.T("حذف الفاتورة", "Delete invoice");
            Loaded += ResumeHeldInvoiceWindow_Loaded;
            HeldInvoicesGrid.LoadingRow += HeldInvoicesGrid_LoadingRow;
        }

        private async void ResumeHeldInvoiceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HeldInvoicesGrid.IsEnabled = false;
            DeleteHeldInvoiceButton.IsEnabled = false;
            try
            {
                await LoadHeldInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load held invoices: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                HeldInvoicesGrid.IsEnabled = true;
                ResumeHeldInvoiceButton.IsEnabled = true;
                DeleteHeldInvoiceButton.IsEnabled = true;
            }
        }

        private void ResumeHeldInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            ResumeSelectedInvoice();
        }

        private void HeldInvoicesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            ResumeSelectedInvoice();
        }

        private void ResumeSelectedInvoice()
        {
            SelectedInvoice = HeldInvoicesGrid.SelectedItem as InvoiceReadDto;
            if (SelectedInvoice == null)
            {
                MessageBox.Show(
                    UiText.T("يرجى تحديد فاتورة معلقة أولاً.", "Please select a held invoice first."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private async Task LoadHeldInvoicesAsync()
        {
            var result = await _invoiceService.GetHeldPOSInvoicesAsync();
            if (result.Success)
            {
                HeldInvoicesGrid.ItemsSource = result.Data;
                UiText.ApplyTranslations(this);
            }
            else
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل الفواتير المعلقة.", "Failed to load held invoices."));
            }
        }

        private async void DeleteHeldInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedInvoice = HeldInvoicesGrid.SelectedItem as InvoiceReadDto;
            if (selectedInvoice == null)
            {
                MessageBox.Show(
                    UiText.T("يرجى تحديد فاتورة معلقة أولاً.", "Please select a held invoice first."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var confirmation = MessageBox.Show(
                UiText.T(
                    $"هل تريد حذف الفاتورة المعلقة رقم {selectedInvoice.InvoiceNumber} نهائياً؟",
                    $"Do you permanently delete held invoice {selectedInvoice.InvoiceNumber}?"),
                UiText.T("تأكيد الحذف", "Confirm deletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                DeleteHeldInvoiceButton.IsEnabled = false;
                var result = await _invoiceService.DeleteAsync(selectedInvoice.Id);
                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message ?? UiText.T("تعذر حذف الفاتورة المعلقة.", "Could not delete the held invoice."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                await LoadHeldInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر حذف الفاتورة المعلقة", "Could not delete the held invoice")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                DeleteHeldInvoiceButton.IsEnabled = true;
            }
        }

        private void Grid_PreviewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var clickedRow = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            SelectedInvoice = clickedRow?.Item as InvoiceReadDto;

            if (SelectedInvoice != null)
            {
                e.Handled = true;
                DialogResult = true;
                Close();
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child)
            where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private void HeldInvoicesGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is not InvoiceReadDto invoice || string.IsNullOrWhiteSpace(invoice.HeldColor))
                return;

            e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(invoice.HeldColor)!);
            e.Row.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetTextColor(invoice.HeldColor))!);
        }
    }
}
