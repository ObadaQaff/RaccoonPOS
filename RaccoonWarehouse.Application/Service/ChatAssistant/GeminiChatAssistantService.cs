using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using System.Text;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class GeminiChatAssistantService : IChatAssistantService
{
    private readonly IChatAssistantSettingsService _settings;
    private readonly IChatAssistantKnowledgeService _knowledge;
    private readonly IChatAssistantDataService _data;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public GeminiChatAssistantService(
        IChatAssistantSettingsService settings,
        IChatAssistantKnowledgeService knowledge,
        IChatAssistantDataService data)
    {
        _settings = settings;
        _knowledge = knowledge;
        _data = data;
    }

    public async Task<ChatMessageDto> GetResponseAsync(string message, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetSettingsAsync(cancellationToken);
        var apiKey = await _settings.GetApiKeyAsync(cancellationToken);
        var topic = await _knowledge.FindTopicAsync(message, cancellationToken);
        var liveData = await _data.FindDataAsync(message, cancellationToken);
        var isArabic = message.Any(character => character is >= '\u0600' and <= '\u06ff');
        if (IsGreeting(message)) return BuildGreetingResponse(isArabic);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (liveData != null) return BuildOfflineDataResponse(liveData, isArabic);
            if (topic != null) return BuildOfflineResponse(topic, isArabic);
            return BuildUnknownResponse(isArabic);
        }

        var prompt = BuildPrompt(message, topic, liveData, isArabic);
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
            ActionKey = liveData?.ActionKey ?? (topic is { IsAmbiguous: false } ? topic.ActionKey : null),
            ActionLabel = liveData != null
                ? (isArabic ? liveData.ActionLabelAr : liveData.ActionLabelEn)
                : topic is { IsAmbiguous: false }
                    ? (isArabic ? topic.ActionLabelAr : topic.ActionLabelEn)
                    : null
        };
    }

    private static ChatMessageDto BuildOfflineResponse(ChatAssistantHelpTopicDto topic, bool isArabic)
    {
        var title = isArabic ? topic.TitleAr : topic.TitleEn;
        var steps = isArabic ? topic.StepsAr : topic.StepsEn;
        var text = new StringBuilder()
            .AppendLine(title)
            .AppendLine()
            .Append(string.Join(Environment.NewLine, steps.Select((step, index) => $"{index + 1}. {step}")))
            .ToString();

        if (topic.IsAmbiguous)
        {
            text = isArabic
                ? "هل تقصد هذا الإجراء؟\n\n" + text
                : "Did you mean this workflow?\n\n" + text;
        }

        return new ChatMessageDto
        {
            Text = text,
            ActionKey = topic.IsAmbiguous ? null : topic.ActionKey,
            ActionLabel = topic.IsAmbiguous ? null : isArabic ? topic.ActionLabelAr : topic.ActionLabelEn
        };
    }

    private static ChatMessageDto BuildOfflineDataResponse(ChatAssistantDataResultDto data, bool isArabic)
    {
        string formatted;
        try
        {
            using var document = JsonDocument.Parse(data.DataJson);
            formatted = FormatOfflineData(document.RootElement);
        }
        catch (JsonException)
        {
            formatted = data.DataJson;
        }

        return new ChatMessageDto
        {
            Text = isArabic
                ? $"تم العثور على بيانات حية من النظام ({data.Type}). لاستخدام شرح نصي، أضف مفتاح Gemini.\n\n{formatted}"
                : $"Live ERP data found ({data.Type}). Add a Gemini key for a natural-language explanation.\n\n{formatted}",
            ActionKey = data.ActionKey,
            ActionLabel = isArabic ? data.ActionLabelAr : data.ActionLabelEn
        };
    }

    private static ChatMessageDto BuildGreetingResponse(bool isArabic) => new()
    {
        Text = isArabic
            ? "أهلاً بك! يمكنني مساعدتك في استخدام ROCCOPOS، والبحث في بيانات النظام عند توفر الصلاحية."
            : "Hello! I can help you use ROCCOPOS and search system data when you have permission."
    };

    private static ChatMessageDto BuildUnknownResponse(bool isArabic) => new()
    {
        Text = isArabic
            ? "لم أفهم السؤال بعد. جرّب السؤال عن إضافة منتج، المخزون، فاتورة، رصيد عميل، أو تقرير مبيعات."
            : "I’m not sure what you mean yet. Try asking about adding a product, stock, an invoice, a customer balance, or a sales report."
    };

    private static bool IsGreeting(string message) => ContainsAny(message.Trim().ToLowerInvariant(),
        "hello", "hi", "hey", "good morning", "good evening", "مرحبا", "مرحباً", "اهلا", "أهلا", "السلام عليكم");

    private static bool ContainsAny(string text, params string[] values) => values.Any(text.Contains);

    private static string FormatOfflineData(JsonElement element)
    {
        var lines = new List<string>();
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                lines.Add(item.ValueKind == JsonValueKind.Object
                    ? $"- {FormatObjectInline(item)}"
                    : $"- {FormatJsonValue(item)}");
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                lines.Add($"{Humanize(property.Name)}: {FormatJsonValue(property.Value)}");
            }
        }
        else
        {
            lines.Add(FormatJsonValue(element));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatObjectInline(JsonElement element)
    {
        return string.Join(" | ", element.EnumerateObject().Select(property =>
            $"{Humanize(property.Name)}: {FormatJsonValue(property.Value)}"));
    }

    private static string FormatJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Null => "-",
        JsonValueKind.Array or JsonValueKind.Object => FormatOfflineData(element),
        _ => element.GetRawText()
    };

    private static string Humanize(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (var character in value)
        {
            if (char.IsUpper(character) && builder.Length > 0) builder.Append(' ');
            builder.Append(character);
        }
        return char.ToUpperInvariant(builder[0]) + builder.ToString(1, builder.Length - 1);
    }

    private static string BuildPrompt(string message, ChatAssistantHelpTopicDto? topic, ChatAssistantDataResultDto? liveData, bool isArabic)
    {
        var language = isArabic ? "Arabic" : "English";
        if (liveData != null)
        {
            return $"""
                You are the ROCCOPOS data assistant. Answer in {language}.
                Answer only from the live ERP data below. Do not invent products, prices, quantities, or capabilities.
                If the data array is empty, clearly say that no matching records were found.
                Mention that live data is limited to the returned records when the result is truncated.
                Give a concise answer and use a small list or table when useful.

                Data type: {liveData.Type}
                Search term: {liveData.Query}
                Live ERP data (JSON): {liveData.DataJson}

                User question: {message.Trim()}
                """;
        }
        if (topic == null)
        {
            return $"""
                You are the ROCCOPOS usage assistant. Answer in {language}.
                No matching workflow exists in the supplied product documentation.
                Politely say you do not recognize the request yet, suggest a documented ROCCOPOS area, and ask the user to rephrase.
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
