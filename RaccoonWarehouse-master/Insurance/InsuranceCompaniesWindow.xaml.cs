using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.Insurance;

public partial class InsuranceCompaniesWindow : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly ILoadingService _loading;
    private readonly ObservableCollection<InsuranceCompanyDto> _companies = new();

    public InsuranceCompaniesWindow(IInsuranceManagementService service, ILoadingService loading)
    {
        _service = service;
        _loading = loading;
        InitializeComponent();
        CompaniesGrid.ItemsSource = _companies;
        OrganizationTypeComboBox.ItemsSource = Enum.GetValues<InsuranceOrganizationType>();
        OrganizationTypeComboBox.SelectedItem = InsuranceOrganizationType.Insurer;
        UiText.ApplyWindow(this);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading.Show();
        try
        {
            var result = await _service.GetCompaniesAsync();
            if (!result.Success)
            {
                MessageBox.Show(result.Message, UiText.T("خطأ", "Error"));
                return;
            }

            _companies.Clear();
            foreach (var company in result.Data ?? new List<InsuranceCompanyDto>())
                _companies.Add(company);
            EmptyStatePanel.Visibility = _companies.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var result = await _service.SaveCompanyAsync(new InsuranceCompanyWriteDto
        {
            Code = CodeTextBox.Text,
            Name = NameTextBox.Text,
            Status = InsuranceRecordStatus.Active,
            OrganizationType = (InsuranceOrganizationType?)OrganizationTypeComboBox.SelectedItem ?? InsuranceOrganizationType.Insurer
        });

        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success)
        {
            CodeTextBox.Clear();
            NameTextBox.Clear();
            await LoadAsync();
        }
    }
}
