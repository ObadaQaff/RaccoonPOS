using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse
{
    public partial class CreateUser : Window
    {
        private readonly IUserService _unitService;

        public CreateUser(IUserService userService)
        {

            _unitService = userService;
            InitializeComponent();


           Role.ItemsSource = Enum.GetValues(typeof(UserRole));
            Role.SelectedIndex = 0;
        }

       
        private async void  Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Basic input validation
                if (string.IsNullOrWhiteSpace(FullName.Text) ||
                    string.IsNullOrWhiteSpace(Password.Text))
                {
                    MessageBox.Show("Please fill in all required fields.");
                    return;
                }

                var user = new UserWriteDto
                {
                    Name = FullName.Text,
                    PhoneNumber = PhoneNumber.Text,
                    Password = Password.Text,
                    Role = (UserRole)Role.SelectedItem
                };

                var result = await _unitService.CreateAsync(user);
                if (result.Success) {

                    MessageBox.Show("User was added successfully!");

                    FullName.Text = "";
                    PhoneNumber.Text = "";
                    Password.Text = "";
                    ConfirmPassword.Text = "";
                    Role.SelectedItem = 0;

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

          
            this.Close();

        }
    }
}
