using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Common.Loading;
using System.Net.Http;
using System.Windows;

namespace RaccoonWarehouse.ChatAssistant;

public partial class ChatAssistantSettingsWindow : Window
{
    private readonly IChatAssistantSettingsService _settings;
    private readonly ILoadingService _loadingService;
    private readonly App _app;

    public ChatAssistantSettingsWindow(IChatAssistantSettingsService settings, ILoadingService loadingService)
    {
        InitializeComponent();
        _settings = settings;
        _loadingService = loadingService;
        _app = (App)System.Windows.Application.Current;
        Loaded += ChatAssistantSettingsWindow_Loaded;
    }

    private async void ChatAssistantSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTexts();
        var settings = await _settings.GetSettingsAsync();
        ModelTextBox.Text = settings.Model;
        KeyHintTextBlock.Text = _app.IsEnglish
            ? settings.HasApiKey ? "A Gemini API key is already stored securely. Enter a new key only to replace it." : "The Gemini API key is encrypted for this Windows user."
            : settings.HasApiKey ? "يوجد مفتاح Gemini API محفوظ بشكل مشفر. أدخل مفتاحاً جديداً فقط للاستبدال." : "يتم حفظ مفتاح Gemini API مشفراً لهذا المستخدم في ويندوز.";
    }

    private void ApplyTexts()
    {
        var english = _app.IsEnglish;
        Title = english ? "Gemini Chatbot Settings" : "إعدادات مساعد Gemini";
        TitleTextBlock.Text = Title;
        DescriptionTextBlock.Text = english ? "Enter your Gemini API key, choose a model, then save and test the connection." : "أدخل مفتاح Gemini API، واختر النموذج، ثم احفظ واختبر الاتصال.";
        ApiKeyLabelTextBlock.Text = english ? "Gemini API key" : "مفتاح Gemini API";
        ModelLabelTextBlock.Text = english ? "Model" : "النموذج";
        SaveButton.Content = english ? "Save and Test" : "حفظ واختبار";
        CancelButton.Content = english ? "Cancel" : "إلغاء";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var model = ModelTextBox.Text.Trim();
        var apiKey = ApiKeyPasswordBox.Password.Trim();
        if (string.IsNullOrWhiteSpace(model)) { StatusTextBlock.Text = _app.IsEnglish ? "Enter a model name." : "أدخل اسم النموذج."; return; }
        if (!model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase))
        {
            StatusTextBlock.Text = _app.IsEnglish
                ? "Enter a Gemini model name, for example gemini-3.5-flash."
                : "أدخل اسم نموذج Gemini، مثل gemini-3.5-flash.";
            return;
        }
        if (string.IsNullOrWhiteSpace(apiKey)) apiKey = await _settings.GetApiKeyAsync() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey)) { StatusTextBlock.Text = _app.IsEnglish ? "Enter an API key." : "أدخل مفتاح API."; return; }
        SaveButton.IsEnabled = false;
        StatusTextBlock.Text = _app.IsEnglish ? "Testing connection..." : "جار اختبار الاتصال...";
        _loadingService.Show();
        try
        {
            if (!await _settings.TestConnectionAsync(apiKey, model))
            {
                StatusTextBlock.Text = _app.IsEnglish
                    ? "Gemini rejected text generation. Check the API key, model access, quota, region, and billing in Google AI Studio."
                    : "رفض Gemini إنشاء النص. تحقق من مفتاح API وصلاحية النموذج والحصة والمنطقة والفوترة في Google AI Studio.";
                return;
            }
            await _settings.SaveAsync(apiKey, model);
            StatusTextBlock.Text = _app.IsEnglish ? "Connection verified and settings saved." : "تم التحقق من الاتصال وحفظ الإعدادات.";
        }
        catch (HttpRequestException) { StatusTextBlock.Text = _app.IsEnglish ? "Unable to reach Gemini. Check your internet connection." : "تعذر الوصول إلى Gemini. تحقق من اتصال الإنترنت."; }
        catch (TaskCanceledException) { StatusTextBlock.Text = _app.IsEnglish ? "The connection test timed out." : "انتهت مهلة اختبار الاتصال."; }
        finally { _loadingService.Hide(); SaveButton.IsEnabled = true; }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
