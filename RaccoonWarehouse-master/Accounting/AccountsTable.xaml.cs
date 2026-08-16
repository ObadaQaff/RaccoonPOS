using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Accounting
{
    public partial class AccountsTable : Window
    {
        private readonly AccountTreeViewModel _viewModel;

        public AccountsTable(AccountTreeViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = _viewModel;
            Loaded += async (_, _) => await _viewModel.RefreshAsync();
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
