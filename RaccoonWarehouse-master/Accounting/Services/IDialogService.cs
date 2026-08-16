using RaccoonWarehouse.Accounting.ViewModels;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Accounting.Services
{
    public interface IDialogService
    {
        Task<Account?> ShowAddAccountDialogAsync(AddAccountDialogRequest request);
    }
}
