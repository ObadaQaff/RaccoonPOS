using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Accounting.Services;
using RaccoonWarehouse.Application.Service.Accounting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Accounting.ViewModels
{
    public partial class AccountTreeViewModel : ObservableObject
    {
        private readonly AccountService _accountService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<AccountTreeNode> rootAccounts = new();

        [ObservableProperty]
        private AccountTreeNode? selectedAccount;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? selectedBalanceText;

        public AccountTreeViewModel(AccountService accountService, IDialogService dialogService, IServiceProvider serviceProvider)
        {
            _accountService = accountService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var tree = await _accountService.GetTreeAsync();
                RootAccounts = new ObservableCollection<AccountTreeNode>(tree.Select(MapNode));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to load accounts: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void Rename(AccountTreeNode? node)
        {
            var target = node ?? SelectedAccount;
            if (target == null) return;
            target.EditName = target.Name;
            target.IsEditing = true;
            target.TouchVisuals();
        }

        [RelayCommand]
        public async Task SaveAsync(AccountTreeNode? node)
        {
            var target = node ?? SelectedAccount;
            if (target == null || IsBusy) return;

            if (string.IsNullOrWhiteSpace(target.EditName))
            {
                target.IsEditing = false;
                return;
            }

            IsBusy = true;
            try
            {
                await _accountService.UpdateAsync(target.Id, target.EditName.Trim(), target.IsActive);
                target.Name = target.EditName.Trim();
                target.IsEditing = false;
                target.IsNew = false;
                target.TouchVisuals();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to save account: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task AddChildAsync(AccountTreeNode? node)
        {
            var parent = node ?? SelectedAccount;
            if (parent == null || IsBusy) return;

            try
            {
                var existingCodes = RootAccounts
                    .SelectMany(Flatten)
                    .Select(x => x.DisplayCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                var request = new AddAccountDialogRequest
                {
                    ParentId = parent.Id,
                    ParentName = parent.Name,
                    ParentCode = parent.DisplayCode,
                    ParentLevel = parent.AccountLevel ?? 1,
                    ParentChildCount = parent.Children.Count,
                    DefaultNature = parent.AccountNature ?? "Debit",
                    DefaultCategory = parent.AccountCategory ?? "Expense",
                    ExistingCodes = existingCodes
                };

                var created = await _dialogService.ShowAddAccountDialogAsync(request);
                if (created == null)
                    return;

                IsBusy = true;

                var child = new AccountTreeNode
                {
                    Id = created.Id,
                    Code = created.Code,
                    AccountCode = created.AccountCode,
                    AccountLevel = created.AccountLevel,
                    AccountNature = created.AccountNature,
                    AccountCategory = created.AccountCategory,
                    AccountTypeCode = created.AccountTypeCode,
                    Name = created.Name,
                    Description = created.Description,
                    IsPosting = created.IsPosting,
                    IsActive = created.IsActive,
                    ParentAccountId = created.ParentAccountId,
                    IsExpanded = false,
                    IsEditing = false,
                    IsNew = true,
                    EditName = created.Name
                };
                child.TouchVisuals();

                parent.Children.Add(child);
                parent.IsExpanded = true;
                SelectedAccount = child;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to add child: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static System.Collections.Generic.IEnumerable<AccountTreeNode> Flatten(AccountTreeNode node)
        {
            yield return node;
            foreach (var child in node.Children.SelectMany(Flatten))
                yield return child;
        }

        [RelayCommand]
        public async Task DeactivateAsync(AccountTreeNode? node)
        {
            var target = node ?? SelectedAccount;
            if (target == null || IsBusy) return;

            try
            {
                IsBusy = true;
                await _accountService.DeactivateAsync(target.Id);
                target.IsActive = false;
                target.IsEditing = false;
                target.TouchVisuals();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to deactivate: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewBalanceAsync(AccountTreeNode? node)
        {
            var target = node ?? SelectedAccount;
            if (target == null) return;

            try
            {
                var balance = await _accountService.GetBalanceAsync(target.Id);
                SelectedBalanceText = $"Debit: {balance.DebitBalance:N2} | Credit: {balance.CreditBalance:N2} | Net: {balance.NetBalance:N2}";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to load balance: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        public Task OpenTransactionsAsync(AccountTreeNode? node)
        {
            var target = node ?? SelectedAccount;
            if (target == null)
                return Task.CompletedTask;

            try
            {
                var report = _serviceProvider.GetRequiredService<RaccoonWarehouse.Accounting.GeneralLedgerReport>();
                report.OpenForAccount(target.Id);
                report.Owner = System.Windows.Application.Current.MainWindow;
                report.Show();
                report.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open transactions: {ex.Message}", "Error");
            }

            return Task.CompletedTask;
        }

        private static AccountTreeNode MapNode(RaccoonWarehouse.Domain.Accounting.Accounts.DTOs.AccountTreeNodeDto dto)
        {
            var node = new AccountTreeNode
            {
                Id = dto.Id,
                Code = dto.Code,
                AccountCode = dto.Code,
                AccountLevel = dto.Level,
                AccountNature = dto.Nature,
                AccountCategory = dto.Category,
                Name = dto.Name,
                IsPosting = dto.IsPosting,
                IsActive = dto.IsActive,
                IsExpanded = false,
                IsEditing = false,
                IsNew = false,
                EditName = dto.Name
            };

            foreach (var child in dto.Children)
                node.Children.Add(MapNode(child));

            node.TouchVisuals();
            return node;
        }
    }
}
