using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Navigation.Modules;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.ChatAssistant;

public partial class ChatAssistantWindow : Window
{
    private readonly IChatAssistantService _assistant;
    private readonly DashboardActionRegistry _dashboardActions;
    public ObservableCollection<ChatMessageDto> Messages { get; } = new();

    public ChatAssistantWindow(IChatAssistantService assistant, DashboardActionRegistry dashboardActions)
    {
        _assistant = assistant;
        _dashboardActions = dashboardActions;
        InitializeComponent();
        DataContext = this;
        Loaded += (_, _) => { if (Messages.Count == 0) Messages.Add(new ChatMessageDto { Text = "Configure your Gemini API key in Settings, then ask about stock, products, or invoices." }); SettingsButton.Content = ((App)System.Windows.Application.Current).IsEnglish ? "Settings" : "الإعدادات"; MessageTextBox.Focus(); };
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendAsync();
    private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { e.Handled = true; await SendAsync(); } }

    private async Task SendAsync()
    {
        var text = MessageTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        MessageTextBox.Clear();
        SendButton.IsEnabled = MessageTextBox.IsEnabled = false;
        Messages.Add(new ChatMessageDto { Text = text, IsFromUser = true });
        var thinkingMessage = new ChatMessageDto
        {
            Text = ((App)System.Windows.Application.Current).IsEnglish
                ? "Thinking…"
                : "جارٍ التفكير…",
            IsThinking = true
        };
        Messages.Add(thinkingMessage);
        Dispatcher.BeginInvoke(MessagesScrollViewer.ScrollToEnd);

        try
        {
            var response = await _assistant.GetResponseAsync(text);
            Messages.Remove(thinkingMessage);
            Messages.Add(response);
        }
        catch
        {
            Messages.Remove(thinkingMessage);
            Messages.Add(new ChatMessageDto { Text = ((App)System.Windows.Application.Current).IsEnglish ? "The assistant could not return a response. Check Settings and your internet connection." : "تعذر على المساعد إرجاع رد. تحقق من الإعدادات واتصال الإنترنت." });
        }
        finally
        {
            Messages.Remove(thinkingMessage);
            SendButton.IsEnabled = MessageTextBox.IsEnabled = true;
            Dispatcher.BeginInvoke(MessagesScrollViewer.ScrollToEnd);
            MessageTextBox.Focus();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => WindowManager.ShowDialog<ChatAssistantSettingsWindow>(WindowSizeType.MediumRectangle);

    private async void OpenActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionKey } || string.IsNullOrWhiteSpace(actionKey)) return;

        try
        {
            await _dashboardActions.ExecuteAsync(actionKey, new DashboardActionContext
            {
                OpenReportWindow = openAction => openAction(),
                RefreshAccountingNavigationAsync = () => Task.CompletedTask
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{UiText.T("تعذر فتح النافذة", "Could not open the window")}: {ex.Message}",
                UiText.T("خطأ", "Error"));
        }
    }
}
