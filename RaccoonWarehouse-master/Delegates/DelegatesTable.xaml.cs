using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Delegates.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace RaccoonWarehouse.Delegates
{
    public partial class DelegatesTable : Window
    {
        private readonly IDelegateService _delegateService;
        private readonly IDelegateFeatureService _featureService;
        private readonly List<DelegateReadDto> _items = new();
        private ICollectionView? _view;

        public DelegatesTable(IDelegateService delegateService, IDelegateFeatureService featureService)
        {
            _delegateService = delegateService;
            _featureService = featureService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += DelegatesTable_Loaded;
        }

        private async void DelegatesTable_Loaded(object sender, RoutedEventArgs e)
        {
            StatusFilter.ItemsSource = new object[] { UiText.T("الكل", "All") }.Concat(Enum.GetValues(typeof(DelegateStatus)).Cast<object>());
            StatusFilter.SelectedIndex = 0;
            TypeFilter.ItemsSource = new object[] { UiText.T("الكل", "All") }.Concat(Enum.GetValues(typeof(DelegateType)).Cast<object>());
            TypeFilter.SelectedIndex = 0;
            await LoadDelegatesAsync();
        }

        private async Task LoadDelegatesAsync()
        {
            var enabled = await _featureService.IsEnabledAsync();
            if (!enabled)
            {
                MessageBox.Show(UiText.T("نظام المندوبين غير مفعل حالياً.", "The delegates system is currently disabled."));
                Close();
                return;
            }

            FeatureStateText.Text = UiText.T("النظام مفعل حالياً ويمكن ربط المندوبين بالفواتير.", "The system is currently enabled and delegates can be linked to invoices.");
            HintText.Text = UiText.T("يمكن البحث بالكود أو الاسم أو الهاتف.", "You can search by code, name, or phone.");
            CreateDelegateBtn.IsEnabled = enabled;

            var result = await _delegateService.GetListAsync();
            _items.Clear();
            if (result.Data != null)
            {
                _items.AddRange(result.Data);
            }

            TotalDelegatesText.Text = _items.Count.ToString();
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = ApplyFilters;
            DelegatesGrid.ItemsSource = _view;
        }

        private bool ApplyFilters(object item)
        {
            if (item is not DelegateReadDto dto)
            {
                return false;
            }

            var search = SearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var matched = dto.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || dto.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (dto.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);

                if (!matched)
                {
                    return false;
                }
            }

            if (StatusFilter.SelectedItem is DelegateStatus status && dto.Status != status)
            {
                return false;
            }

            if (TypeFilter.SelectedItem is DelegateType type && dto.DelegateType != type)
            {
                return false;
            }

            return true;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            _view?.Refresh();
        }

        private async void CreateDelegateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await _featureService.IsEnabledAsync())
            {
                MessageBox.Show(UiText.T("نظام المندوبين غير مفعل.", "The delegates system is disabled."));
                return;
            }

            WindowManager.ShowDialog<CreateDelegate>(WindowSizeType.MediumRectangle);
            await LoadDelegatesAsync();
        }

        private DelegateReadDto? GetSelectedDelegate()
        {
            if (DelegatesGrid.SelectedItem is DelegateReadDto dto)
            {
                return dto;
            }

            MessageBox.Show(UiText.T("يرجى اختيار مندوب أولاً.", "Please select a delegate first."));
            return null;
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedDelegate();
            if (selected == null)
            {
                return;
            }

            WindowManager.ShowDialog<UpdateDelegate>(WindowSizeType.MediumRectangle, window => window.Initialize(selected.Id));
            await LoadDelegatesAsync();
        }

        private async void Details_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedDelegate();
            if (selected == null)
            {
                return;
            }

            WindowManager.ShowDialog<DelegateDetails>(WindowSizeType.MediumRectangle, window => window.Initialize(selected.Id));
            await LoadDelegatesAsync();
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedDelegate();
            if (selected == null)
            {
                return;
            }

            await _delegateService.SetStatusAsync(selected.Id, DelegateStatus.Active);
            await LoadDelegatesAsync();
        }

        private async void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedDelegate();
            if (selected == null)
            {
                return;
            }

            await _delegateService.SetStatusAsync(selected.Id, DelegateStatus.Inactive);
            await LoadDelegatesAsync();
        }

        private async void FeatureSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowDialog<DelegateFeatureSettingsWindow>(WindowSizeType.SmallSquare);
            await LoadDelegatesAsync();
        }

        private void DelegatesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Details_Click(sender, e);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
