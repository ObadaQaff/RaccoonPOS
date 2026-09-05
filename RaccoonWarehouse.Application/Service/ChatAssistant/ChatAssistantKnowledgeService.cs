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
                questionToken == keywordToken ||
                (keywordToken.Length >= 4 && questionToken.Length >= 4 && EditDistanceAtMostOne(keywordToken, questionToken))));

            if (matchedTokens > 0)
            {
                var ratio = (double)matchedTokens / keywordTokens.Length;
                best = Math.Max(best, (keywordTokens.Length > 1 ? 5d : 1.5d) * ratio);
            }
        }

        return best;
    }

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

    private static bool EditDistanceAtMostOne(string left, string right)
    {
        if (Math.Abs(left.Length - right.Length) > 1) return false;
        var differences = 0;
        var leftIndex = 0;
        var rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            if (left[leftIndex] == right[rightIndex])
            {
                leftIndex++;
                rightIndex++;
                continue;
            }

            if (++differences > 1) return false;
            if (left.Length > right.Length) leftIndex++;
            else if (right.Length > left.Length) rightIndex++;
            else { leftIndex++; rightIndex++; }
        }

        return differences + Math.Max(left.Length - leftIndex, right.Length - rightIndex) <= 1;
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
