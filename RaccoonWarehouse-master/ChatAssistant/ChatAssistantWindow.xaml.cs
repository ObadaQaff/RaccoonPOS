using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Navigation.Modules;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.ChatAssistant;

public partial class ChatAssistantWindow : Window
{
    private readonly IChatAssistantService _assistant;
    private readonly DashboardActionRegistry _dashboardActions;
    private readonly IWindowNavigationService _windowNavigation;
    public ObservableCollection<ChatMessageDto> Messages { get; } = new();

    public ChatAssistantWindow(IChatAssistantService assistant, DashboardActionRegistry dashboardActions, IWindowNavigationService windowNavigation)
    {
        _assistant = assistant;
        _dashboardActions = dashboardActions;
        _windowNavigation = windowNavigation;
        InitializeComponent();
        DataContext = this;
        Loaded += (_, _) =>
        {
            ApplyTexts();
            if (Messages.Count == 0)
            {
                var english = ((App)System.Windows.Application.Current).IsEnglish;
                Messages.Add(new ChatMessageDto { Text = english
                    ? "Ask how to use ROCCOPOS. Documented help works without an API key."
                    : "اسأل عن طريقة استخدام ROCCOPOS. تعمل المساعدة الموثقة بدون مفتاح API." });
            }
            MessageTextBox.Focus();
        };
    }

    private void ApplyTexts()
    {
        var english = ((App)System.Windows.Application.Current).IsEnglish;
        Title = english ? "ROCCOPOS Assistant" : "مساعد ROCCOPOS";
        AssistantTitleTextBlock.Text = Title;
        AssistantDescriptionTextBlock.Text = english ? "Ask about documented workflows and features." : "اسأل عن الإجراءات والميزات الموثقة.";
        ClearButton.Content = english ? "Clear" : "مسح";
        SettingsButton.Content = english ? "Settings" : "الإعدادات";
        SendButton.Content = english ? "Send" : "إرسال";
        CopyButtonContent = english ? "Copy" : "نسخ";
    }

    public string CopyButtonContent { get; private set; } = "Copy";

    private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendAsync();

    private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; await SendAsync(); }
    }

    private async Task SendAsync()
    {
        var text = MessageTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        MessageTextBox.Clear();
        SendButton.IsEnabled = MessageTextBox.IsEnabled = false;
        Messages.Add(new ChatMessageDto { Text = text, IsFromUser = true });
        var thinkingMessage = new ChatMessageDto { Text = ((App)System.Windows.Application.Current).IsEnglish ? "Thinking…" : "جارٍ التفكير…", IsThinking = true };
        Messages.Add(thinkingMessage);
        Dispatcher.BeginInvoke(MessagesScrollViewer.ScrollToEnd);
        try
        {
            Messages.Add(await _assistant.GetResponseAsync(text));
        }
        catch (Exception ex)
        {
            var english = ((App)System.Windows.Application.Current).IsEnglish;
            var detail = ex is InvalidOperationException ? ex.Message : english ? "Check Settings and your internet connection." : "تحقق من الإعدادات واتصال الإنترنت.";
            Messages.Add(new ChatMessageDto { Text = english ? $"The assistant could not return a response. {detail}" : $"تعذر على المساعد إرجاع رد. {detail}" });
        }
        finally
        {
            Messages.Remove(thinkingMessage);
            SendButton.IsEnabled = MessageTextBox.IsEnabled = true;
            Dispatcher.BeginInvoke(MessagesScrollViewer.ScrollToEnd);
            MessageTextBox.Focus();
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Messages.Clear();
        ApplyTexts();
        MessageTextBox.Focus();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ChatMessageDto message } && !string.IsNullOrWhiteSpace(message.Text)) Clipboard.SetText(message.Text);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => WindowManager.ShowDialog<ChatAssistantSettingsWindow>(WindowSizeType.MediumRectangle);

    private async void OpenActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionKey } || string.IsNullOrWhiteSpace(actionKey)) return;
        try
        {
            if (_dashboardActions.CanHandle(actionKey))
            {
                await _dashboardActions.ExecuteAsync(actionKey, new DashboardActionContext
                {
                    OpenReportWindow = openAction => openAction(),
                    RefreshAccountingNavigationAsync = () => Task.CompletedTask
                });
            }
            else if (_windowNavigation.CanShow(actionKey))
            {
                _windowNavigation.Show(actionKey, WindowSizeType.LargeRectangle);
            }
            else
            {
                throw new InvalidOperationException($"No navigation target is registered for '{actionKey}'.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Helpers.Localization.UiText.T("تعذر فتح النافذة", "Could not open the window")}: {ex.Message}", Helpers.Localization.UiText.T("خطأ", "Error"));
        }
    }
}
