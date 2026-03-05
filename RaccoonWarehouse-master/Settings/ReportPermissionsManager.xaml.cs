using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
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
        private readonly IReportPermissionService _reportPermissionService;
        private readonly IUserSession _userSession;
        private readonly ObservableCollection<ReportPermissionRow> _rows = new();
        private ICollectionView? _rowsView;
        private static readonly UserRole[] ManagedRoles = { UserRole.Admin, UserRole.Casher };

        public ReportPermissionsManager(IReportPermissionService reportPermissionService, IUserSession userSession)
        {
            InitializeComponent();
            _reportPermissionService = reportPermissionService;
            _userSession = userSession;
            Loaded += ReportPermissionsManager_Loaded;
        }

        private async void ReportPermissionsManager_Loaded(object sender, RoutedEventArgs e)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin)
            {
                MessageBox.Show("فقط المدير يمكنه إدارة صلاحيات التقارير.");
                Close();
                return;
            }

            BuildColumns();
            await LoadRowsAsync();
        }

        private void BuildColumns()
        {
            PermissionsGrid.Columns.Clear();

            PermissionsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "التقرير",
                Binding = new Binding(nameof(ReportPermissionRow.DisplayName)),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                IsReadOnly = true
            });

            PermissionsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "التصنيف",
                Binding = new Binding(nameof(ReportPermissionRow.Category)),
                Width = new DataGridLength(1.4, DataGridLengthUnitType.Star),
                IsReadOnly = true
            });

            foreach (var role in ManagedRoles)
            {
                var factory = new FrameworkElementFactory(typeof(CheckBox));
                factory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                factory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                factory.SetBinding(ToggleButton.IsCheckedProperty, new Binding($"Permissions[{role}]")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

                PermissionsGrid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = role.ToString(),
                    CellTemplate = new DataTemplate { VisualTree = factory },
                    Width = new DataGridLength(120)
                });
            }
        }

        private async Task LoadRowsAsync()
        {
            var permissionsMap = await _reportPermissionService.GetPermissionsMapAsync();

            _rows.Clear();
            foreach (var report in ReportCatalog.All.OrderBy(x => x.Category).ThenBy(x => x.DisplayName))
            {
                var row = new ReportPermissionRow
                {
                    ReportKey = report.Key,
                    DisplayName = report.DisplayName,
                    Category = report.Category
                };

                foreach (var role in ManagedRoles)
                {
                    var canView = true;
                    if (permissionsMap.TryGetValue(report.Key, out var roleMap) && roleMap.TryGetValue(role, out var savedValue))
                        canView = savedValue;

                    row.Permissions[role.ToString()] = canView;
                }

                _rows.Add(row);
            }

            _rowsView = CollectionViewSource.GetDefaultView(_rows);
            _rowsView.Filter = FilterRows;
            PermissionsGrid.ItemsSource = _rowsView;
        }

        private bool FilterRows(object item)
        {
            if (item is not ReportPermissionRow row)
                return false;

            var search = SearchTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return row.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || row.Category.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _rowsView?.Refresh();
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var payload = _rows
                .SelectMany(row => ManagedRoles.Select(role => new ReportPermissionWriteDto
                {
                    ReportKey = row.ReportKey,
                    Role = role,
                    CanView = row.Permissions.TryGetValue(role.ToString(), out var canView) && canView
                }))
                .ToList();

            var result = await _reportPermissionService.SavePermissionsAsync(payload);
            MessageBox.Show(result.Message, result.Success ? "نجاح" : "خطأ");
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ReportPermissionRow : INotifyPropertyChanged
    {
        public string ReportKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, bool> Permissions { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
