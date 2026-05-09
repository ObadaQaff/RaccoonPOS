using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.SubCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.Categories
{
    /// <summary>
    /// Interaction logic for CategoriesTable.xaml
    /// </summary>
    public partial class CategoriesTable : Window
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;
        private string _currentNameSearch = "";
        private CancellationTokenSource _searchCts;

        public CategoriesTable(ICategoryService categoryService, IMapper mapper, ILoadingService loadingService)
        {
            _categoryService = categoryService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            CategoriesTable1.AutoGeneratingColumn += CategoriesTable1_AutoGeneratingColumn;
            _ = Load_CategoriesAsync();
        }

        private async Task Load_CategoriesAsync()
        {
            try
            {
                _loadingService.Show();
                var result = await _categoryService.GetAllAsync();
                if (result.Success)
                {
                    CategoriesTable1.ItemsSource = result.Data;
                }
                else
                {
                    _loadingService.Hide();
                    _loadingService.Hide();
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الفئات.", "Failed to load categories."));
                }
            }
            catch (Exception ex)
            {
                _loadingService.Hide();
                _loadingService.Hide();
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء تحميل الفئات", "Unexpected error while loading categories")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private CategoryReadDto? GetSelectedCategory()
        {
            if (CategoriesTable1.SelectedItem is CategoryReadDto selectedCategory)
                return selectedCategory;

            MessageBox.Show(UiText.T("لم يتم اختيار فئة.", "No category selected."));
            return null;
        }

        private void Update_Category(object sender, RoutedEventArgs e)
        {
            if (GetSelectedCategory() is not CategoryReadDto selectedCategory)
            {
                return;
            }

            WindowManager.ShowDialog<UpdateCategory>(WindowSizeType.MediumRectangle, w =>
            {
                w.Initialize(selectedCategory.Id);
            });

        }

        private async void Delete_Category(object sender, RoutedEventArgs e)
        {
            if (GetSelectedCategory() is not CategoryReadDto selectedCategory)
            {
                return;
            }

            var messageResult = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Are you sure you want to delete the category '{selectedCategory.Name}'?"
                    : $"هل أنت متأكد من أنك تريد حذف الفئة '{selectedCategory.Name}'؟",
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
                var result = await _categoryService.DeleteAsync(selectedCategory.Id);
                if (result.Success)
                {
                    _loadingService.Hide();
                    MessageBox.Show(UiText.T("تم الحذف بنجاح !!", "Delete was successful."));
                    await Load_CategoriesAsync();
                }
                else
                {
                    _loadingService.Hide();
                    MessageBox.Show(result.Message ?? UiText.T("فشل الحذف.", "Delete failed."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء حذف الفئة", "Unexpected error while deleting category")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }


        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateCategory>();
            createCategory.Show();
            this.Hide();

        }

        private void CreateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedCategory() is not CategoryReadDto selectedCategory)
                return;

            WindowManager.ShowDialog<CreateSubCategory>(WindowSizeType.MediumRectangle, window =>
            {
                window.InitializeForParentCategory(selectedCategory.Id, selectedCategory.Name);
            });
        }

        private void OpenSubCategoriesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedCategory() is not CategoryReadDto selectedCategory)
                return;

            var subCategoryTable = ((App)System.Windows.Application.Current)
                .ServiceProvider.GetRequiredService<SubCategoryTable>();
            subCategoryTable.Owner = this;
            subCategoryTable.ApplyParentCategoryFilter(selectedCategory.Id, selectedCategory.Name);
            subCategoryTable.Show();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void CategoriesTable1_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = e.PropertyName switch
            {
                "Id" => UiText.T("الرقم التعريفي", "ID"),
                "Name" => UiText.T("اسم الفئة", "Category Name"),
                "CreatedDate" => UiText.T("تاريخ الإنشاء", "Created Date"),
                "UpdatedDate" => UiText.T("آخر تحديث", "Last Updated"),
                _ => UiText.Translate(e.Column.Header?.ToString() ?? e.PropertyName)
            };
        }
    }
}
