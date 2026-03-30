using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace RaccoonWarehouse.Settings
{
    public partial class ReportPermissionsManager : Window
    {
        private readonly IPermissionService _permissionService;
        private readonly IUserSession _userSession;
        private readonly ObservableCollection<PermissionMatrixRowViewModel> _rows = new();
        private ICollectionView? _rowsView;
        private List<string> _actions = new();
        private bool _isInitializing;
        private bool _isLoading;

        public ReportPermissionsManager(IPermissionService permissionService, IUserSession userSession)
        {
            InitializeComponent();
            _permissionService = permissionService;
            _userSession = userSession;
            Loaded += ReportPermissionsManager_Loaded;
        }

        private async void ReportPermissionsManager_Loaded(object sender, RoutedEventArgs e)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin)
            {
                MessageBox.Show("فقط المدير يمكنه إدارة صلاحيات النظام.");
                Close();
                return;
            }

            _isInitializing = true;
            RoleComboBox.ItemsSource = Enum.GetValues(typeof(UserRole));
            RoleComboBox.SelectedItem = UserRole.Casher;

            var modules = await _permissionService.GetModuleNamesAsync();
            ModuleComboBox.ItemsSource = new List<string> { "الكل" }.Concat(modules);
            ModuleComboBox.SelectedIndex = 0;

            _actions = await _permissionService.GetActionNamesAsync();
            BuildColumns();
            _isInitializing = false;
            await LoadRowsAsync();
        }

        private void BuildColumns()
        {
            PermissionsGrid.Columns.Clear();

            PermissionsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "الوحدة",
                Binding = new Binding(nameof(PermissionMatrixRowViewModel.Module)),
                Width = new DataGridLength(1.2, DataGridLengthUnitType.Star),
                IsReadOnly = true
            });

            PermissionsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "العنصر",
                Binding = new Binding(nameof(PermissionMatrixRowViewModel.DisplayName)),
                Width = new DataGridLength(1.8, DataGridLengthUnitType.Star),
                IsReadOnly = true
            });

            var rowButtonFactory = new FrameworkElementFactory(typeof(Button));
            rowButtonFactory.SetValue(ContentProperty, "الكل");
            rowButtonFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(SelectAllRow_Click));
            PermissionsGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "الصف",
                Width = 90,
                CellTemplate = new DataTemplate { VisualTree = rowButtonFactory }
            });

            foreach (var action in _actions)
            {
                var factory = new FrameworkElementFactory(typeof(CheckBox));
                factory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                factory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                factory.SetBinding(ToggleButton.IsCheckedProperty, new Binding($"Actions[{action}]")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

                PermissionsGrid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = action,
                    CellTemplate = new DataTemplate { VisualTree = factory },
                    Width = 95
                });
            }
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
            var selectedModule = string.Equals(module, "الكل", StringComparison.Ordinal) ? null : module;

            var matrix = await _permissionService.GetPermissionMatrixAsync(role, search, selectedModule);

            _rows.Clear();
            foreach (var row in matrix)
            {
                var vm = new PermissionMatrixRowViewModel
                {
                    Module = row.Module,
                    Resource = row.Resource,
                    DisplayName = row.DisplayName
                };

                foreach (var action in _actions)
                    vm.Actions[action] = row.Actions.TryGetValue(action, out var allowed) && allowed;

                _rows.Add(vm);
            }

            _rowsView = CollectionViewSource.GetDefaultView(_rows);
            PermissionsGrid.ItemsSource = _rowsView;

            SummaryTextBlock.Text = $"العناصر الظاهرة: {_rows.Count} | الدور الحالي: {role}";
            UpdateSelectedCount();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateSelectedCount()
        {
            var count = _rows.Sum(x => x.Actions.Values.Count(v => v));
            SelectedCountText.Text = count.ToString();
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

            foreach (var action in _actions)
                row.Actions[action] = true;

            PermissionsGrid.Items.Refresh();
            UpdateSelectedCount();
        }

        private void SelectAllModuleBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
                foreach (var action in _actions)
                    row.Actions[action] = true;

            PermissionsGrid.Items.Refresh();
            UpdateSelectedCount();
        }

        private void ClearModuleBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
                foreach (var action in _actions)
                    row.Actions[action] = false;

            PermissionsGrid.Items.Refresh();
            UpdateSelectedCount();
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RoleComboBox.SelectedItem is not UserRole role)
                return;

            var payload = _rows
                .SelectMany(row => _actions.Select(action =>
                {
                    var catalog = PermissionCatalog.FindByKey($"{row.Resource}.{action}");
                    return catalog == null
                        ? null
                        : new RolePermissionWriteDto
                        {
                            Role = role,
                            PermissionKey = catalog.Key,
                            IsAllowed = row.Actions.TryGetValue(action, out var allowed) && allowed
                        };
                }))
                .Where(x => x != null)!
                .ToList();

            var result = await _permissionService.SavePermissionsAsync(role, payload!);
            MessageBox.Show(result.Message, result.Success ? "نجاح" : "خطأ");
            UpdateSelectedCount();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class PermissionMatrixRowViewModel : INotifyPropertyChanged
    {
        public string Module { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Dictionary<string, bool> Actions { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
