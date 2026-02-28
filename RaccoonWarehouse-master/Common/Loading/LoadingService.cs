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
    }


    public interface ILoadingService
    {
        void Show();
        void Hide();
    }



}
