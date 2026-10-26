using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Common.Loading
{
    using System.Windows;

    public class LoadingService : ILoadingService
    {
        private LoadingWindow_v2? _window;

        public void Show()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_window == null)
                {
                    _window = new LoadingWindow_v2();
                    _window.Show();
                }
            });
        }

        public void Hide()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _window?.Close();
                _window = null;
            });
        }

        public void UpdateProgress(int progress, string? message = null, string? hint = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_window == null)
                {
                    return;
                }

                var value = Math.Clamp(progress, 0, 100);
                _window.LoadingProgressBar.Value = value;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    _window.MessageText.Text = message;
                }

                if (!string.IsNullOrWhiteSpace(hint))
                {
                    _window.HintText.Text = hint;
                    _window.HintText.Visibility = Visibility.Visible;
                }
            });
        }
    }


    public interface ILoadingService
    {
        void Show();
        void Hide();
        void UpdateProgress(int progress, string? message = null, string? hint = null);
    }



}
