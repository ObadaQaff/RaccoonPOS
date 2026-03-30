using RaccoonWarehouse.Application.Service.Settings;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Settings
{
    public partial class LanguageSettingsWindow : Window
    {
        private readonly ILanguageSettingsService _languageSettingsService;
        private readonly App _app;
        private AppLanguage _currentLanguage;

        public LanguageSettingsWindow(ILanguageSettingsService languageSettingsService)
        {
            InitializeComponent();
            _languageSettingsService = languageSettingsService;
            _app = (App)System.Windows.Application.Current;
            Loaded += LanguageSettingsWindow_Loaded;
        }

        private async void LanguageSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyWindowTexts();

            _currentLanguage = await _languageSettingsService.GetCurrentLanguageAsync();

            LanguageComboBox.ItemsSource = new[]
            {
                new LanguageOption(AppLanguage.Arabic, "العربية"),
                new LanguageOption(AppLanguage.English, "English")
            };

            LanguageComboBox.DisplayMemberPath = nameof(LanguageOption.Title);
            LanguageComboBox.SelectedValuePath = nameof(LanguageOption.Value);
            LanguageComboBox.SelectedValue = _currentLanguage;
            SaveButton.IsEnabled = false;
        }

        private void ApplyWindowTexts()
        {
            if (_app.IsEnglish)
            {
                Title = "Language Settings";
                TitleTextBlock.Text = "Language Settings";
                DescriptionTextBlock.Text = "Choose the application language. The app will restart after you save the change.";
                LanguageLabelTextBlock.Text = "Application language";
                SaveButton.Content = "Save and Restart";
                CancelButton.Content = "Cancel";
                return;
            }

            Title = "إعدادات اللغة";
            TitleTextBlock.Text = "إعدادات اللغة";
            DescriptionTextBlock.Text = "اختر لغة التطبيق. سيتم إعادة تشغيل البرنامج بعد حفظ التغيير.";
            LanguageLabelTextBlock.Text = "لغة التطبيق";
            SaveButton.Content = "حفظ وإعادة التشغيل";
            CancelButton.Content = "إلغاء";
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            SaveButton.IsEnabled = LanguageComboBox.SelectedValue is AppLanguage selectedLanguage
                && selectedLanguage != _currentLanguage;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageComboBox.SelectedValue is not AppLanguage selectedLanguage)
                return;

            var result = await _languageSettingsService.SetCurrentLanguageAsync(selectedLanguage);
            MessageBox.Show(
                _app.IsEnglish ? "The application will restart to apply the new language." : "سيتم إعادة تشغيل التطبيق لتطبيق اللغة الجديدة.",
                _app.IsEnglish ? "Restart Required" : "تأكيد",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _app.ApplyLanguage(selectedLanguage);
            Close();
            _app.Restart();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed record LanguageOption(AppLanguage Value, string Title);
    }
}
