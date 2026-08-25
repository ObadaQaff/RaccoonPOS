using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Common.Loading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RaccoonWarehouse.Accounting
{
    public partial class AccountsTable : Window
    {
        private readonly AccountTreeViewModel _viewModel;
        private readonly ILoadingService _loadingService;

        public AccountsTable(AccountTreeViewModel viewModel, ILoadingService loadingService)
        {
            _viewModel = viewModel;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = _viewModel;
            Loaded += AccountsTable_Loaded;
        }

        private async void AccountsTable_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loadingService.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await _viewModel.RefreshAsync();
                UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AccountsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _viewModel.SelectedAccount = e.NewValue as AccountTreeNode;
        }
    }
}
