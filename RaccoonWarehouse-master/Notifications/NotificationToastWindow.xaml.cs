using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Domain.Notifications;

namespace RaccoonWarehouse.Notifications
{
    public partial class NotificationToastWindow : Window
    {
        private readonly DispatcherTimer _closeTimer;

        public NotificationToastWindow()
        {
            InitializeComponent();
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _closeTimer.Tick += CloseTimer_Tick;
        }

        public void ShowNotification(AppNotificationDto notification)
        {
            TitleText.Text = string.IsNullOrWhiteSpace(notification.Title)
                ? UiText.T("تنبيه", "Alert")
                : notification.Title;
            MessageText.Text = notification.Message;

            ApplySeverity(notification.Severity);
            PlayNotificationSound(notification.Severity);
            PositionWindow();

            Loaded += NotificationToastWindow_Loaded;
            Closed += NotificationToastWindow_Closed;

            Show();
            Activate();
            _closeTimer.Start();
        }

        private static void PlayNotificationSound(NotificationSeverity severity)
        {
            try
            {
                var sound = severity switch
                {
                    NotificationSeverity.Error => SystemSounds.Hand,
                    NotificationSeverity.Warning => SystemSounds.Exclamation,
                    NotificationSeverity.Success => SystemSounds.Exclamation,
                    _ => SystemSounds.Asterisk
                };

                sound.Play();
            }
            catch
            {
                // Sound must never prevent the notification from being displayed.
            }
        }

        private void ApplySeverity(NotificationSeverity severity)
        {
            var (accent, border) = severity switch
            {
                NotificationSeverity.Success => ("#145A4A", "#B7D8D1"),
                NotificationSeverity.Warning => ("#C18D00", "#EAD7A6"),
                NotificationSeverity.Error => ("#B32424", "#E4B1B1"),
                _ => ("#2F83C5", "#C8D9E8")
            };

            AccentBar.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(accent)!;
            RootBorder.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(border)!;
        }

        private void PositionWindow()
        {
            var workArea = SystemParameters.WorkArea;
            var left = workArea.Right - Width - 24;
            var top = workArea.Bottom - 24 - Height;

            Left = Math.Max(workArea.Left + 12, left);
            Top = Math.Max(workArea.Top + 12, top);
        }

        private void NotificationToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= NotificationToastWindow_Loaded;
            PositionWindow();
        }

        private void NotificationToastWindow_Closed(object? sender, EventArgs e)
        {
            Closed -= NotificationToastWindow_Closed;
            _closeTimer.Stop();
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            _closeTimer.Stop();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RootBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
