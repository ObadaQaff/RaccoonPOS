using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using System.Text.Json;
using System.Globalization;
using System.Text;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class ChatAssistantKnowledgeService : IChatAssistantKnowledgeService
{
    private readonly Lazy<Task<IReadOnlyList<ChatAssistantHelpTopicDto>>> _topics = new(LoadTopicsAsync);

    public async Task<ChatAssistantHelpTopicDto?> FindTopicAsync(string question, CancellationToken cancellationToken = default)
    {
        var topics = await _topics.Value.WaitAsync(cancellationToken);
        var normalizedQuestion = Normalize(question);
        if (string.IsNullOrWhiteSpace(normalizedQuestion)) return null;

        var ranked = topics.Select(topic => new
            {
                Topic = topic,
                Score = ScoreTopic(normalizedQuestion, topic)
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Topic.Keywords.Max(keyword => keyword.Length))
            .ToList();

        if (ranked.Count == 0 || ranked[0].Score < 2.5) return null;

        var best = ranked[0];
        best.Topic.MatchScore = best.Score;
        best.Topic.IsAmbiguous = ranked.Count > 1 && ranked[1].Score >= best.Score - 1.25;
        return best.Topic;
    }

    private static double ScoreTopic(string question, ChatAssistantHelpTopicDto topic)
    {
        var best = 0d;
        foreach (var keyword in topic.Keywords)
        {
            var normalizedKeyword = Normalize(keyword);
            if (normalizedKeyword.Length == 0) continue;

            if (question.Contains(normalizedKeyword, StringComparison.Ordinal))
            {
                best = Math.Max(best, normalizedKeyword.Contains(' ') ? 8d : 3d);
                continue;
            }

            var keywordTokens = normalizedKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var questionTokens = question.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchedTokens = keywordTokens.Count(keywordToken => questionTokens.Any(questionToken =>
                TokensHaveSameMeaning(keywordToken, questionToken) ||
                (keywordToken.Length >= 4 && questionToken.Length >= 4 && EditDistanceWithinLimit(keywordToken, questionToken))));

            if (matchedTokens > 0)
            {
                var ratio = (double)matchedTokens / keywordTokens.Length;
                best = Math.Max(best, (keywordTokens.Length > 1 ? 5d : 2.6d) * ratio);
            }
        }

        if (HasCreateIntent(question) && topic.Keywords.Any(keyword =>
                Normalize(keyword).Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(IsCreateVerb)))
        {
            best += 2d;
        }

        return best;
    }

    private static bool TokensHaveSameMeaning(string left, string right)
    {
        if (left == right) return true;
        if (left.Length > 3 && left.TrimEnd('s') == right.TrimEnd('s')) return true;

        var groups = new[]
        {
            new[] { "create", "add", "new", "make", "register", "record", "enter", "انشاء", "إنشاء", "اضافة", "إضافة", "سجل", "تسجيل", "اعمل", "سوي" },
            new[] { "supplier", "vendor", "مورد" },
            new[] { "customer", "client", "زبون", "عميل" },
            new[] { "product", "item", "goods", "صنف", "منتج", "بضاعة" },
            new[] { "receive", "collect", "incoming", "received", "استلام", "قبض", "وارد" },
            new[] { "pay", "payment", "paid", "outgoing", "صرف", "دفع" },
            new[] { "stock", "inventory", "مخزون" },
            new[] { "voucher", "slip", "document", "سند" }
        };

        return groups.Any(group => group.Contains(left, StringComparer.OrdinalIgnoreCase) && group.Contains(right, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasCreateIntent(string question) => question.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(IsCreateVerb);

    private static bool IsCreateVerb(string token) => token is "create" or "add" or "new" or "make" or "register" or "record" or "enter" or "انشاء" or "إنشاء" or "اضافة" or "إضافة" or "سجل" or "تسجيل" or "اعمل" or "سوي";

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            var normalized = character switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                _ => char.ToLowerInvariant(character)
            };

            if (char.IsLetterOrDigit(normalized) || normalized == ' ')
                builder.Append(normalized);
            else
                builder.Append(' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Replace("ادخال", "اضافه", StringComparison.Ordinal)
            .Replace("انشاء", "انشاء", StringComparison.Ordinal)
            .Replace("صنف", "منتج", StringComparison.Ordinal);
    }

    private static bool EditDistanceWithinLimit(string left, string right)
    {
        var limit = Math.Max(left.Length, right.Length) >= 7 ? 2 : 1;
        if (Math.Abs(left.Length - right.Length) > limit) return false;
        if (left.Length == right.Length && limit >= 1)
        {
            var mismatches = Enumerable.Range(0, left.Length).Where(index => left[index] != right[index]).ToArray();
            if (mismatches.Length == 2 && mismatches[1] == mismatches[0] + 1 &&
                left[mismatches[0]] == right[mismatches[1]] && left[mismatches[1]] == right[mismatches[0]]) return true;
        }

        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var current = new int[right.Length + 1];
            current[0] = i;
            var rowMinimum = current[0];
            for (var j = 1; j <= right.Length; j++)
            {
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
                rowMinimum = Math.Min(rowMinimum, current[j]);
            }
            if (rowMinimum > limit) return false;
            previous = current;
        }
        return previous[right.Length] <= limit;
    }

    private static async Task<IReadOnlyList<ChatAssistantHelpTopicDto>> LoadTopicsAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ChatAssistant", "Knowledge", "ROCCOPOS_HELP.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ChatAssistantHelpTopicDto>>(stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new List<ChatAssistantHelpTopicDto>();
    }
}
