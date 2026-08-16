using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class AddAccountDialog : Window
    {
        public AddAccountDialog(AddAccountViewModel viewModel)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = viewModel;
        }
    }
}
