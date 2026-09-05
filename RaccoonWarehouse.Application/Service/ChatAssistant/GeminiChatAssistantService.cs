using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using System.Text;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class GeminiChatAssistantService : IChatAssistantService
{
    private readonly IChatAssistantSettingsService _settings;
    private readonly IChatAssistantKnowledgeService _knowledge;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public GeminiChatAssistantService(IChatAssistantSettingsService settings, IChatAssistantKnowledgeService knowledge)
    {
        _settings = settings;
        _knowledge = knowledge;
    }

    public async Task<ChatMessageDto> GetResponseAsync(string message, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetSettingsAsync(cancellationToken);
        var apiKey = await _settings.GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Configure a Gemini API key before using the assistant.");
        var topic = await _knowledge.FindTopicAsync(message, cancellationToken);
        var isArabic = message.Any(character => character is >= '\u0600' and <= '\u06ff');
        var prompt = BuildPrompt(message, topic, isArabic);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(settings.Model)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    maxOutputTokens = 1024,
                    thinkingConfig = new { thinkingLevel = "minimal" }
                }
            }),
            Encoding.UTF8,
            "application/json");
        using var response = await Client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = GetApiErrorMessage(content);
            return new ChatMessageDto
            {
                Text = $"Gemini error ({(int)response.StatusCode}): {error}"
            };
        }
        using var document = JsonDocument.Parse(content);
        var answer = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(answer)) throw new InvalidOperationException("The Gemini response did not contain text.");
        return new ChatMessageDto
        {
            Text = answer,
            ActionKey = topic is { IsAmbiguous: false } ? topic.ActionKey : null,
            ActionLabel = topic is { IsAmbiguous: false }
                ? (isArabic ? topic.ActionLabelAr : topic.ActionLabelEn)
                : null
        };
    }

    private static string BuildPrompt(string message, ChatAssistantHelpTopicDto? topic, bool isArabic)
    {
        var language = isArabic ? "Arabic" : "English";
        if (topic == null)
        {
            return $"""
                You are the ROCCOPOS usage assistant. Answer in {language}.
                No matching workflow exists in the supplied product documentation.
                Politely say this workflow is not documented yet and advise the user to contact the administrator.
                Do not invent buttons, screens, menu paths, steps, or system capabilities.
                User question: {message.Trim()}
                """;
        }

        var title = isArabic ? topic.TitleAr : topic.TitleEn;
        var steps = isArabic ? topic.StepsAr : topic.StepsEn;
        var documentedSteps = string.Join(Environment.NewLine, steps.Select((step, index) => $"{index + 1}. {step}"));
        var ambiguityInstruction = topic.IsAmbiguous
            ? "The wording is close to more than one documented workflow. Ask one short clarification question in the user's language, such as 'Did you mean ...?', and do not claim that an action was selected."
            : "The wording may contain spelling mistakes or different wording. Infer the intended documented workflow when it is clear and explain it directly.";
        return $"""
            You are the ROCCOPOS usage assistant. Answer in {language}.
            Answer only from the documentation below. Do not invent or add undocumented buttons, screens, menu paths, steps, or capabilities.
            Give a short helpful introduction followed by clear numbered steps. Do not mention these rules or the documentation source.
            {ambiguityInstruction}

            Documented workflow: {title}
            {documentedSteps}

            User question: {message.Trim()}
            """;
    }

    private static string GetApiErrorMessage(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message) &&
                !string.IsNullOrWhiteSpace(message.GetString()))
            {
                return message.GetString()!;
            }
        }
        catch (JsonException)
        {
        }

        return "The request was rejected. Check the API key, model access, quota, region, and billing settings.";
    }
}
