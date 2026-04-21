using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace RaccoonWarehouse.Navigation
{
    public sealed class WindowNavigationService : IWindowNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IReadOnlyDictionary<string, Type> _windowMap;
        private readonly Dictionary<string, Window> _windows = new(StringComparer.Ordinal);

        public WindowNavigationService(
            IServiceProvider serviceProvider,
            IReadOnlyDictionary<string, Type> windowMap)
        {
            _serviceProvider = serviceProvider;
            _windowMap = windowMap;
        }

        public void Show(string windowKey, WindowSizeType size = WindowSizeType.MediumRectangle)
        {
            if (_windows.TryGetValue(windowKey, out var existing))
            {
                Restore(existing);
                return;
            }

            if (!_windowMap.TryGetValue(windowKey, out var windowType))
            {
                throw new InvalidOperationException($"Window key '{windowKey}' is not registered.");
            }

            var resolved = _serviceProvider.GetRequiredService(windowType);
            if (resolved is not Window window)
            {
                throw new InvalidOperationException($"Registered service '{windowType.FullName}' is not a WPF Window.");
            }

            ApplySize(window, size);
            window.Closed += (_, __) => _windows.Remove(windowKey);
            _windows[windowKey] = window;
            window.Show();
        }

        private static void Restore(Window window)
        {
            if (!window.IsVisible)
                window.Show();

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Activate();
            window.Focus();
        }

        private static void ApplySize(Window window, WindowSizeType size)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.SizeToContent = SizeToContent.Manual;

            switch (size)
            {
                case WindowSizeType.SmallSquare:
                    window.Width = 600;
                    window.Height = 600;
                    window.WindowState = WindowState.Normal;
                    break;
                case WindowSizeType.MediumRectangle:
                    window.Width = 1300;
                    window.Height = 800;
                    window.WindowState = WindowState.Normal;
                    break;
                case WindowSizeType.LargeRectangle:
                    window.Width = 1800;
                    window.Height = 800;
                    window.WindowState = WindowState.Normal;
                    break;
                case WindowSizeType.FullScreen:
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = WindowState.Maximized;
                    break;
                case WindowSizeType.SmallRectangle:
                    window.Width = 300;
                    window.Height = 260;
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = WindowState.Normal;
                    break;
            }

            EnsureWindowStartsVisible(window);
        }

        private static void EnsureWindowStartsVisible(Window window)
        {
            void Reposition()
            {
                if (window.WindowState != WindowState.Normal)
                    return;

                var workArea = SystemParameters.WorkArea;
                var width = window.Width > 0 ? window.Width : window.ActualWidth;
                var height = window.Height > 0 ? window.Height : window.ActualHeight;

                if (width <= 0 || height <= 0)
                    return;

                if (width > workArea.Width)
                    width = workArea.Width;

                if (height > workArea.Height)
                    height = workArea.Height;

                window.Width = width;
                window.Height = height;
                window.Left = workArea.Left + Math.Max(0, (workArea.Width - width) / 2);
                window.Top = workArea.Top + Math.Max(0, (workArea.Height - height) / 2);
            }

            window.SourceInitialized -= Window_SourceInitialized;
            window.SourceInitialized += Window_SourceInitialized;

            void Window_SourceInitialized(object? sender, EventArgs e)
            {
                window.SourceInitialized -= Window_SourceInitialized;
                window.Dispatcher.BeginInvoke(Reposition);
            }
        }
    }
}
