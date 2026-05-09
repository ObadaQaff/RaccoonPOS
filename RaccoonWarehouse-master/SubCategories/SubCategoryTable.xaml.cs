using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using RaccoonWarehouse.Stocks;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.SubCategories
{
    /// <summary>
    /// Interaction logic for SubCategoryTable.xaml
    /// </summary>
    public partial class SubCategoryTable : Window
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;
        private bool _hasLoaded;
        private int? _parentCategoryFilterId;
        private string? _parentCategoryFilterName;

        public SubCategoryTable(ISubCategoryService subCategoryService, IMapper mapper, ILoadingService loadingService)
        {
            _subCategoryService = subCategoryService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            SubCategoriesTable1.AutoGeneratingColumn += SubCategoriesTable1_AutoGeneratingColumn;
            Loaded += SubCategoryTable_Loaded;
            UpdateFilterInfoText();
        }

        public void ApplyParentCategoryFilter(int parentCategoryId, string? parentCategoryName = null)
        {
            _parentCategoryFilterId = parentCategoryId;
            _parentCategoryFilterName = parentCategoryName;
            UpdateFilterInfoText();

            if (IsLoaded)
                _ = Load_SubCategoriesAsync();
        }

        private async void SubCategoryTable_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hasLoaded)
                return;

            _hasLoaded = true;
            await Load_SubCategoriesAsync();
        }

        private async Task Load_SubCategoriesAsync()
        {
            try
            {
                _loadingService.Show();

                var result = _parentCategoryFilterId.HasValue
                    ? await _subCategoryService.GetAllWithFilteringAndIncludeAsync(
                        subCategory => subCategory.ParentCategoryId == _parentCategoryFilterId.Value,
                        subCategory => subCategory.ParentCategory)
                    : await _subCategoryService.GetAllWithIncludeAsync(subCategory => subCategory.ParentCategory);
                if (result.Success)
                {
                    _loadingService.Hide();
                    SubCategoriesTable1.ItemsSource = result.Data;
                    UpdateFilterInfoText();
                }
                else
                {
                    _loadingService.Hide();
                    _loadingService.Hide();
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الفئات الفرعية.", "Failed to load subcategories."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء تحميل الفئات الفرعية", "Unexpected error while loading subcategories")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private SubCategoryReadDto? GetSelectedSubCategory()
        {
            if (SubCategoriesTable1.SelectedItem is SubCategoryReadDto selectedSubCategory)
                return selectedSubCategory;

            MessageBox.Show(UiText.T("لم يتم اختيار فئة فرعية.", "No subcategory selected."));
            return null;
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((RaccoonWarehouse.App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateSubCategory>();
            createCategory.Show();
            this.Hide();

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private async void CreateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowDialog<CreateSubCategory>(WindowSizeType.MediumRectangle, window =>
            {
                if (_parentCategoryFilterId.HasValue)
                    window.InitializeForParentCategory(_parentCategoryFilterId.Value, _parentCategoryFilterName);
            });

            await Load_SubCategoriesAsync();
        }

        private void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSubCategory() is not SubCategoryReadDto selectedSubCategory)
                return;

            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.MediumRectangle, window =>
            {
                window.InitializeForSubCategory(selectedSubCategory.Id, selectedSubCategory.Name);
            });
        }

        private void OpenProductsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSubCategory() is not SubCategoryReadDto selectedSubCategory)
                return;

            var productsTable = ((App)System.Windows.Application.Current)
                .ServiceProvider.GetRequiredService<ProductsTable>();
            productsTable.Owner = this;
            productsTable.ApplySubCategoryFilter(selectedSubCategory.Id, selectedSubCategory.Name);
            productsTable.Show();
        }

        private async void Delete_SubCategory(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSubCategory() is not SubCategoryReadDto selectedSubCategory)
            {
                return;
            }

            var messageResult = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Are you sure you want to delete the subcategory '{selectedSubCategory.Name}'?"
                    : $"هل أنت متأكد من أنك تريد حذف الفئة الفرعية '{selectedSubCategory.Name}'؟",
                UiText.T("تأكيد الحذف", "Confirm Delete"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _loadingService.Show();
                var result = await _subCategoryService.DeleteAsync(selectedSubCategory.Id);

                if (result.Success)
                {
                    _loadingService.Hide();
                    MessageBox.Show(UiText.T("تم الحذف بنجاح !!", "Delete was successful."));
                    await Load_SubCategoriesAsync();
                }
                else
                {
                    _loadingService.Hide();
                    MessageBox.Show(result.Message ?? UiText.T("فشل الحذف.", "Delete failed."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء حذف الفئة الفرعية", "Unexpected error while deleting subcategory")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void Update_SubCategory(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSubCategory() is not SubCategoryReadDto selectedCategory)
            {
                return;
            }

            WindowManager.ShowDialog<UpdateSubCategory>(WindowSizeType.MediumRectangle, w =>
            {
                w.Load_SubCategory_For_Update(selectedCategory.Id);
            });
        }

        private void SubCategoriesTable1_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = e.PropertyName switch
            {
                "Id" => UiText.T("الرقم التعريفي", "ID"),
                "Name" => UiText.T("اسم الفئة الفرعية", "Subcategory Name"),
                "CreatedDate" => UiText.T("تاريخ الإنشاء", "Created Date"),
                "UpdatedDate" => UiText.T("آخر تحديث", "Last Updated"),
                _ => UiText.Translate(e.Column.Header?.ToString() ?? e.PropertyName)
            };
        }
        private void UpdateFilterInfoText()
        {
            if (FilterInfoTextBlock == null)
                return;

            FilterInfoTextBlock.Text = _parentCategoryFilterId.HasValue && !string.IsNullOrWhiteSpace(_parentCategoryFilterName)
                ? UiText.T(
                    $"عرض الفئات الفرعية التابعة للفئة: {_parentCategoryFilterName}",
                    $"Showing subcategories for category: {_parentCategoryFilterName}")
                : UiText.T("جميع الفئات الفرعية", "Showing all subcategories");
        }
    }
}
