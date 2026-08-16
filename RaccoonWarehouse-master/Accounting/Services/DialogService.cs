using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Accounting.Services
{
    public class DialogService : IDialogService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DialogService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task<Account?> ShowAddAccountDialogAsync(AddAccountDialogRequest request)
        {
            using var scope = _scopeFactory.CreateScope();
            var window = scope.ServiceProvider.GetRequiredService<AddAccountDialog>();
            var viewModel = (AddAccountViewModel)window.DataContext;

            viewModel.Initialize(request, result => window.DialogResult = result);

            window.Owner = System.Windows.Application.Current?.MainWindow;
            var ok = window.ShowDialog() == true;
            return Task.FromResult(ok ? viewModel.CreatedAccount : null);
        }
    }
}
