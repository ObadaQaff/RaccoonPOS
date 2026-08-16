using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Accounting.ViewModels
{
    public partial class AddAccountViewModel : ObservableObject
    {
        private readonly AccountService _accountService;
        private Action<bool?>? _closeAction;
        private HashSet<string> _existingCodes = new(StringComparer.OrdinalIgnoreCase);
        private int _parentId;
        private int _level;
        private string _parentCode = string.Empty;

        public Account? CreatedAccount { get; private set; }

        public IReadOnlyList<string> NatureOptions { get; } = new[] { "Debit", "Credit" };
        public IReadOnlyList<string> CategoryOptions { get; } = new[] { "Asset", "Liability", "Equity", "Revenue", "Expense" };
        public IReadOnlyList<string> TypeOptions { get; } = new[] { "BS", "PL" };

        [ObservableProperty] private string accountName = string.Empty;
        [ObservableProperty] private string accountCode = string.Empty;
        [ObservableProperty] private string parentAccountName = string.Empty;
        [ObservableProperty] private int accountLevel;
        [ObservableProperty] private string? accountNature;
        [ObservableProperty] private string? accountCategory;
        [ObservableProperty] private string? accountType;
        [ObservableProperty] private bool isPosting;
        [ObservableProperty] private bool isPostingEnabled;
        [ObservableProperty] private string? description;
        [ObservableProperty] private bool isSaving;

        [ObservableProperty] private string? nameError;
        [ObservableProperty] private string? codeError;
        [ObservableProperty] private string? natureError;
        [ObservableProperty] private string? categoryError;

        public bool HasNameError => !string.IsNullOrWhiteSpace(NameError);
        public bool HasCodeError => !string.IsNullOrWhiteSpace(CodeError);
        public bool HasNatureError => !string.IsNullOrWhiteSpace(NatureError);
        public bool HasCategoryError => !string.IsNullOrWhiteSpace(CategoryError);

        public AddAccountViewModel(AccountService accountService)
        {
            _accountService = accountService;
        }

        public void Initialize(AddAccountDialogRequest request, Action<bool?> closeAction)
        {
            _closeAction = closeAction;
            _parentId = request.ParentId;
            _level = request.ParentLevel + 1;
            _parentCode = request.ParentCode;
            _existingCodes = new HashSet<string>(
                request.ExistingCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);

            ParentAccountName = request.ParentName;
            AccountLevel = _level;
            AccountName = string.Empty;
            AccountNature = request.DefaultNature;
            AccountCategory = request.DefaultCategory;
            AccountType = ResolveTypeFromCategory(AccountCategory);
            IsPosting = _level >= 5;
            IsPostingEnabled = _level >= 5;
            Description = null;
            CreatedAccount = null;

            var siblingCount = request.ParentChildCount + 1;
            var fakeParent = new Account { AccountCode = _parentCode, Code = _parentCode, AccountLevel = request.ParentLevel };
            AccountCode = AccountCodeHelper.GenerateCode(fakeParent, siblingCount);

            ValidateAll();
        }

        partial void OnAccountCategoryChanged(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                AccountType = ResolveTypeFromCategory(value);
            ValidateCategory();
        }

        partial void OnAccountNameChanged(string value) => ValidateName();
        partial void OnAccountCodeChanged(string value) => ValidateCode();
        partial void OnAccountNatureChanged(string? value) => ValidateNature();

        [RelayCommand]
        private void Cancel() => _closeAction?.Invoke(false);

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsSaving)
                return;

            ValidateAll();
            if (HasNameError || HasCodeError || HasNatureError || HasCategoryError)
                return;

            try
            {
                IsSaving = true;
                var account = await _accountService.CreateChildAsync(
                    _parentId,
                    AccountName.Trim(),
                    AccountNature!,
                    AccountCategory!,
                    AccountCode.Trim(),
                    AccountType,
                    IsPosting,
                    Description);

                CreatedAccount = account;
                _closeAction?.Invoke(true);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ValidateAll()
        {
            ValidateName();
            ValidateCode();
            ValidateNature();
            ValidateCategory();
        }

        private void ValidateName()
        {
            NameError = null;
            if (string.IsNullOrWhiteSpace(AccountName))
                NameError = "Account name is required.";
            else if (AccountName.Trim().Length > 100)
                NameError = "Account name cannot exceed 100 characters.";
            OnPropertyChanged(nameof(HasNameError));
        }

        private void ValidateCode()
        {
            CodeError = null;
            if (string.IsNullOrWhiteSpace(AccountCode))
            {
                CodeError = "Account code is required.";
                OnPropertyChanged(nameof(HasCodeError));
                return;
            }

            var code = AccountCode.Trim();
            if (_existingCodes.Contains(code))
                CodeError = "Account code must be unique.";
            OnPropertyChanged(nameof(HasCodeError));
        }

        private void ValidateNature()
        {
            NatureError = null;
            if (string.IsNullOrWhiteSpace(AccountNature))
                NatureError = "Account nature is required.";
            OnPropertyChanged(nameof(HasNatureError));
        }

        private void ValidateCategory()
        {
            CategoryError = null;
            if (string.IsNullOrWhiteSpace(AccountCategory))
                CategoryError = "Account category is required.";
            OnPropertyChanged(nameof(HasCategoryError));
        }

        private static string ResolveTypeFromCategory(string? category)
        {
            if (string.Equals(category, "Revenue", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
            {
                return "PL";
            }

            return "BS";
        }
    }

    public sealed class AddAccountDialogRequest
    {
        public int ParentId { get; init; }
        public string ParentName { get; init; } = string.Empty;
        public string ParentCode { get; init; } = string.Empty;
        public int ParentLevel { get; init; }
        public int ParentChildCount { get; init; }
        public string DefaultNature { get; init; } = "Debit";
        public string DefaultCategory { get; init; } = "Expense";
        public IReadOnlyCollection<string> ExistingCodes { get; init; } = Array.Empty<string>();
    }
}
