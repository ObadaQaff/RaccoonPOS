using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.AuthService;
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace RaccoonWarehouse.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IUserSession _userSession;
        private readonly IAuthService _authService;

        public LoginWindow(
            IServiceProvider serviceProvider,
            ICashierSessionService cashierSessionService,
            IUserSession userSession,
            IAuthService authService,
            IFinancialTransactionService financialTransactionService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _cashierSessionService = cashierSessionService;
            _userSession = userSession;
            _authService = authService;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Focus();

            var storyboard = new Storyboard();

            var opacityAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(opacityAnim, Card);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(Border.OpacityProperty));
            storyboard.Children.Add(opacityAnim);

            var scaleXAnim = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(scaleXAnim, Card);
            Storyboard.SetTargetProperty(
                scaleXAnim,
                new PropertyPath("RenderTransform.(ScaleTransform.ScaleX)")
            );
            storyboard.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(scaleYAnim, Card);
            Storyboard.SetTargetProperty(
                scaleYAnim,
                new PropertyPath("RenderTransform.(ScaleTransform.ScaleY)")
            );
            storyboard.Children.Add(scaleYAnim);

            storyboard.Begin();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Visibility == Visibility.Visible
                ? PasswordBox.Password
                : PasswordTextBox.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password");
                return;
            }

            SetLoading(true);

            try
            {
                var auth = await _authService.AuthenticateAsync(username, password);
                if (!auth.Success)
                {
                    ShowError(auth.Message);
                    return;
                }

                var user = auth.User;
                if (user == null)
                {
                    ShowError("Authenticated user was not returned.");
                    return;
                }

                _userSession.StartUserSession(user);

                var openSession = await _cashierSessionService.GetOpenSessionByCashierAsync(user.Id);
                CashierSessionReadDto session;

                if (openSession == null)
                {
                    var startWin = _serviceProvider.GetRequiredService<StartCashierSessionWindow>();
                    var ok = startWin.ShowDialog();

                    if (ok != true)
                    {
                        ShowError("Cashier session start was canceled.");
                        return;
                    }

                    session = _userSession.CurrentCashierSession!;
                }
                else
                {
                    session = openSession;
                    session.CashierName = user.Name;
                    _userSession.AttachCashierSession(session);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred.\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoginProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LoginText.Text = isLoading ? "Signing in..." : "LOGIN";
            LoginButton.IsEnabled = !isLoading;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void EnterToLogin(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Login_Click(null!, null!);
        }

        private void TogglePassword(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
            }
        }
    }
}
