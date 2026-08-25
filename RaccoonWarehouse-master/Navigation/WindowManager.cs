using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.SubCategories;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RaccoonWarehouse.Navigation
{
    public static class WindowManager
    {
        // Cache ONLY normal windows
        //private static readonly Dictionary<Type, Window> _windows = new();
        //private static readonly Dictionary<Type, (Window window, IServiceScope scope)> _windows = new();

        // ==========================
        // NORMAL WINDOWS (HIDE / SHOW)
        // ==========================
        private static readonly Dictionary<Type, (Window window, IServiceScope scope)> _windows = new();

        public static void Show<T>(
            WindowSizeType size = WindowSizeType.MediumRectangle,
            Action<T>? init = null)
            where T : Window
        {
            // 🔁 If already created
            if (_windows.TryGetValue(typeof(T), out var entry))
            {
                var existing = entry.window;
                init?.Invoke((T)existing);

                if (!existing.IsVisible)
                    existing.Show();

                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Activate();
                existing.Focus();

                return;
            }

            // 🆕 Create new window
            var app = (RaccoonWarehouse.App)System.Windows.Application.Current;
            var scope = app.ServiceProvider.CreateScope();
            var window = scope.ServiceProvider.GetRequiredService<T>();

            ApplySize(window, size);
            init?.Invoke(window);

            // ✅ Remove from dictionary when truly closed
            window.Closed += (_, __) =>
            {
                _windows.Remove(typeof(T));
                scope.Dispose();
            };

            _windows[typeof(T)] = (window, scope);

            window.Show();
        }
        //public static void Show<T>(
        //WindowSizeType size = WindowSizeType.MediumRectangle,
        //Action<T>? init = null)
        //where T : Window
        //{
        //    if (_windows.TryGetValue(typeof(T), out var cached))
        //    {
        //        ApplySize(cached.window, size);
        //        Restore(cached.window);
        //        init?.Invoke((T)cached.window);
        //        return;
        //    }



        //    var window = ((RaccoonWarehouse.App)System.Windows.Application.Current)
        //                        .ServiceProvider.GetRequiredService<T>();

        //    ApplySize(window, size);
        //    init?.Invoke(window);

        //    // ✅ On real close, dispose the scope & remove cache
        //    window.Closed += (_, __) =>
        //    {
        //        _windows.Remove(typeof(T));
        //    };

        //    // ✅ If you want hide instead of close
        //    window.Closing += (s, e) =>
        //    {
        //        e.Cancel = true;
        //        window.Hide();
        //    };

        //    _windows[typeof(T)] = (window);

        //    window.Show();
        //}



        // ==========================
        // DIALOG WINDOWS (CLOSE)
        // ==========================
        public static void ShowDialog<T>(
           WindowSizeType size = WindowSizeType.MediumRectangle,
           Action<T>? init = null)
           where T : Window
        {
            var app = (App)System.Windows.Application.Current;

            using var scope = app.ServiceProvider.CreateScope();
            var window = scope.ServiceProvider.GetRequiredService<T>();

            ApplySize(window, size);
            init?.Invoke(window);

            window.ShowDialog();
        }

        // ==========================
        // HELPERS
        // ==========================
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
