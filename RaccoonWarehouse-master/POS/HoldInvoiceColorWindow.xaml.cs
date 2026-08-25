using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.POS
{
    public partial class HoldInvoiceColorWindow : Window
    {
        public string? SelectedColor { get; private set; }

        public HoldInvoiceColorWindow(IEnumerable<string> colors, ISet<string> occupiedColors)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);

            foreach (var color in colors)
            {
                var isOccupied = occupiedColors.Contains(color);
                var button = new Button
                {
                    Content = ResumeHeldInvoiceWindow.GetColorName(color),
                    Tag = color,
                    Margin = new Thickness(6),
                    IsEnabled = !isOccupied,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ResumeHeldInvoiceWindow.GetTextColor(color))!),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    FontWeight = FontWeights.SemiBold,
                    ToolTip = isOccupied
                        ? UiText.T("هذا اللون مستخدم حالياً", "This color is already in use")
                        : UiText.T("اختيار اللون", "Choose this color")
                };
                button.Click += ColorButton_Click;
                ColorsGrid.Children.Add(button);
            }

            if (ColorsGrid.Children.OfType<Button>().All(button => !button.IsEnabled))
            {
                MessageBox.Show(
                    UiText.T("جميع ألوان الفواتير المعلقة مستخدمة حالياً.", "All held-invoice colors are currently in use."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Loaded += (_, _) => Close();
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = (sender as Button)?.Tag as string;
            DialogResult = SelectedColor != null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
