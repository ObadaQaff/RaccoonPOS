using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Settings
{
    public partial class ReportPermissionsManager : Window
    {
        private readonly IPermissionService _permissionService;
        private readonly IUserSession _userSession;
        private readonly ObservableCollection<PermissionMatrixRowViewModel> _rows = new();
        private readonly List<string> _actions = new();
        private bool _isInitializing;
        private bool _isLoading;

        public ReportPermissionsManager(IPermissionService permissionService, IUserSession userSession)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _permissionService = permissionService;
            _userSession = userSession;
            Loaded += ReportPermissionsManager_Loaded;
        }

        private async void ReportPermissionsManager_Loaded(object sender, RoutedEventArgs e)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin)
            {
                MessageBox.Show(
                    UiText.T("فقط المدير يمكنه إدارة صلاحيات النظام.", "Only the administrator can manage system permissions."),
                    UiText.T("تنبيه", "Notice"));
                Close();
                return;
            }

            _isInitializing = true;
            RoleComboBox.ItemsSource = Enum.GetValues(typeof(UserRole));
            RoleComboBox.SelectedItem = UserRole.Casher;

            var modules = await _permissionService.GetModuleNamesAsync();
            ModuleComboBox.ItemsSource = new List<string> { UiText.T("الكل", "All") }.Concat(modules);
            ModuleComboBox.SelectedIndex = 0;

            _actions.Clear();
            _actions.AddRange(await _permissionService.GetActionNamesAsync());

            PermissionsItemsControl.ItemsSource = _rows;

            _isInitializing = false;
            await LoadRowsAsync();
            UiText.ApplyTranslations(this);
        }

        private async Task LoadRowsAsync()
        {
            if (_isInitializing || _isLoading)
                return;

            _isLoading = true;
            try
            {
                if (RoleComboBox.SelectedItem is not UserRole role)
                    return;

                var module = ModuleComboBox.SelectedItem as string;
                var search = SearchTextBox.Text?.Trim();
                var allLabel = UiText.T("الكل", "All");
                var selectedModule = string.Equals(module, allLabel, StringComparison.Ordinal) ? null : module;

                var matrix = await _permissionService.GetPermissionMatrixAsync(role, search, selectedModule);

                UnsubscribeRows();
                _rows.Clear();

                foreach (var row in matrix.OrderBy(x => x.Module).ThenBy(x => x.DisplayName))
                {
                    var vm = new PermissionMatrixRowViewModel
                    {
                        Module = row.Module,
                        Resource = row.Resource,
                        DisplayName = row.DisplayName
                    };

                    foreach (var action in _actions)
                    {
                        var toggle = new PermissionActionToggleViewModel
                        {
                            ActionKey = action,
                            DisplayName = GetActionDisplayName(action),
                            IsAllowed = row.Actions.TryGetValue(action, out var allowed) && allowed
                        };

                        toggle.PropertyChanged += PermissionToggle_PropertyChanged;
                        vm.Actions.Add(toggle);
                    }

                    _rows.Add(vm);
                }

                SummaryTextBlock.Text = string.Format(
                    UiText.T("الدور الحالي: {0}\nالوحدة المحددة: {1}", "Current role: {0}\nSelected module: {1}"),
                    role,
                    selectedModule ?? allLabel);

                UpdateSelectedCount();
                UiText.ApplyTranslations(this);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UnsubscribeRows()
        {
            foreach (var row in _rows)
            {
                foreach (var action in row.Actions)
                    action.PropertyChanged -= PermissionToggle_PropertyChanged;
            }
        }

        private void PermissionToggle_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PermissionActionToggleViewModel.IsAllowed))
                UpdateSelectedCount();
        }

        private static string GetActionDisplayName(string action)
        {
            return action switch
            {
                "View" => UiText.T("عرض", "View"),
                "Create" => UiText.T("إنشاء", "Create"),
                "Edit" => UiText.T("تعديل", "Edit"),
                "Delete" => UiText.T("حذف", "Delete"),
                "Print" => UiText.T("طباعة", "Print"),
                "Cancel" => UiText.T("إلغاء", "Cancel"),
                "Return" => UiText.T("مرتجع", "Return"),
                "Post" => UiText.T("ترحيل", "Post"),
                "Approve" => UiText.T("اعتماد", "Approve"),
                "Change" => UiText.T("تغيير", "Change"),
                "Reopen" => UiText.T("إعادة فتح", "Reopen"),
                "Close" => UiText.T("إغلاق", "Close"),
                "Export" => UiText.T("تصدير", "Export"),
                "Manage" => UiText.T("إدارة", "Manage"),
                _ => action
            };
        }

        private void UpdateSelectedCount()
        {
            var selectedActions = _rows.Sum(x => x.Actions.Count(a => a.IsAllowed));
            SelectedCountText.Text = selectedActions.ToString();
            VisibleRowsText.Text = string.Format(
                UiText.T("عدد الموارد الظاهرة: {0}", "Visible resources: {0}"),
                _rows.Count);
            VisibleActionsText.Text = string.Format(
                UiText.T("إجمالي مفاتيح الإجراءات لكل الموارد: {0}", "Total action slots across visible resources: {0}"),
                _rows.Sum(x => x.Actions.Count));
        }

        private async void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            await LoadRowsAsync();
        }

        private async void ModuleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            await LoadRowsAsync();
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            await LoadRowsAsync();
        }

        private void SelectAllRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PermissionMatrixRowViewModel row)
                return;

            foreach (var action in row.Actions)
                action.IsAllowed = true;
        }

        private void SelectAllModuleBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
            {
                foreach (var action in row.Actions)
                    action.IsAllowed = true;
            }
        }

        private void ClearModuleBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
            {
                foreach (var action in row.Actions)
                    action.IsAllowed = false;
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RoleComboBox.SelectedItem is not UserRole role)
                return;

            var payload = _rows
                .SelectMany(row => row.Actions.Select(action =>
                {
                    var catalog = PermissionCatalog.FindByKey($"{row.Resource}.{action.ActionKey}");
                    return catalog == null
                        ? null
                        : new RolePermissionWriteDto
                        {
                            Role = role,
                            PermissionKey = catalog.Key,
                            IsAllowed = action.IsAllowed
                        };
                }))
                .Where(x => x != null)!
                .ToList();

            var result = await _permissionService.SavePermissionsAsync(role, payload!);
            MessageBox.Show(
                result.Message,
                result.Success ? UiText.T("نجاح", "Success") : UiText.T("خطأ", "Error"));
            UpdateSelectedCount();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class PermissionMatrixRowViewModel
    {
        public string Module { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ObservableCollection<PermissionActionToggleViewModel> Actions { get; } = new();
    }

    public class PermissionActionToggleViewModel : INotifyPropertyChanged
    {
        private bool _isAllowed;

        public string ActionKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public bool IsAllowed
        {
            get => _isAllowed;
            set
            {
                if (_isAllowed == value)
                    return;

                _isAllowed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAllowed)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
