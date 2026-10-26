using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class AddAccountDialog : Window
    {
        public AddAccountDialog(AddAccountViewModel viewModel)
        {
            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = viewModel;
        }
    }
}
