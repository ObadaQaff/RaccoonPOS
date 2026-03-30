using AutoMapper;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Categories
{
    public partial class CreateCategory : Window
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;

        public CreateCategory(ICategoryService categoryService, IMapper mapper, ILoadingService loadingService)
        {
            _categoryService = categoryService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name.Text))
                {
                    MessageBox.Show(UiText.T("يرجى إدخال اسم فئة صالح.", "Please enter a valid category name."));
                    return;
                }

                _loadingService.Show();

                var categoryWriteDto = new CategoryWriteDto
                {
                    Name = Name.Text.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description.Text) ? null : Description.Text.Trim(),
                };

                var result = await _categoryService.CreateAsync(categoryWriteDto);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تمت إضافة الفئة بنجاح.", "The category was added successfully."));
                    Name.Clear();
                    Description.Clear();
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل إنشاء الفئة.", "Failed to create the category."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء إنشاء الفئة", "An error occurred while creating the category")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
