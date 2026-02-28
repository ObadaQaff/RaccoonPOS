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
        private static readonly Dictionary<Type, Window> _windows = new();

        public static void Show<T>(
            WindowSizeType size = WindowSizeType.MediumRectangle,
            Action<T>? init = null)
            where T : Window
        {
            // 🔁 If already created
            if (_windows.TryGetValue(typeof(T), out var existing))
            {
                if (!existing.IsVisible)
                    existing.Show();

                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Activate();
                existing.Focus();

                return;
            }

            // 🆕 Create new window
            var window = ((RaccoonWarehouse.App)System.Windows.Application.Current)
                .ServiceProvider.GetRequiredService<T>();

            ApplySize(window, size);
            init?.Invoke(window);

            // ✅ Remove from dictionary when truly closed
            window.Closed += (_, __) =>
            {
                _windows.Remove(typeof(T));
            };

            _windows[typeof(T)] = window;

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
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.SizeToContent = SizeToContent.Manual;

            switch (size)
            {
                case WindowSizeType.SmallSquare:
                    window.Width = 600;
                    window.Height = 600;
                    window.WindowState = WindowState.Normal;
                    break;

                case WindowSizeType.MediumRectangle:
                    window.Width = 1000;
                    window.Height = 650;
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
        }
    }
}
