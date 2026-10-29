using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

public class ChatService(
    IKnowledgeService knowledge,
    IChatModelClient model,
    IOptions<ChatOptions> options) : IChatService
{
    // Questions this short are usually follow-ups ("and for sick leave?") that only make sense with the previous question.
    private const int FollowUpMaxWords = 5;

    // The model is asked to start every reply with "Source: <document title>" or "Source: none". Small models sometimes put the line at the end instead, so both ends are accepted.
    private static readonly Regex LeadingSourceLine = new(@"^\s*Source:[ \t]*(?<title>[^\r\n]*)\r?\n?", RegexOptions.IgnoreCase);
    private static readonly Regex TrailingSourceLine = new(@"\s*Source:[ \t]*(?<title>[^\r\n]*)\s*$", RegexOptions.IgnoreCase);

    private readonly ChatOptions _options = options.Value;

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var message = request.Message.Trim();
        var history = (request.History ?? [])
            .Where(m => m.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(_options.HistoryMessages)
            .ToList();

        var sections = await FindSectionsAsync(message, history, cancellationToken);

        // The model always answers. The prompt tells it what to do when the sections answer the question, when they do not, and for casual chat.
        var answer = await model.ChatAsync(ChatPromptBuilder.Build(history, sections, message), cancellationToken);

        return BuildResponse(answer.Trim(), sections);
    }

    /// <summary>
    /// Takes the "Source:" line off the reply. Sources are shown only when it names a document the model was given,
    /// so replies that used no document (chat, "contact HR") show none.
    /// </summary>
    private static ChatResponse BuildResponse(string reply, IReadOnlyList<KnowledgeSearchResult> sections)
    {
        var (answer, title) = SplitSourceLine(reply);
        if (answer.Length == 0)
            return new ChatResponse(reply, []);

        var cited = sections.Where(s => title.StartsWith(s.DocumentTitle, StringComparison.OrdinalIgnoreCase)).ToList();

        return new ChatResponse(
            answer,
            cited.Select(s => new ChatSource(s.DocumentTitle, s.Heading, s.Category, Math.Round(s.Score, 3))).ToList());
    }

    private static (string Answer, string Title) SplitSourceLine(string reply)
    {
        var leading = LeadingSourceLine.Match(reply);
        if (leading.Success)
            return (reply[leading.Length..].Trim(), leading.Groups["title"].Value.Trim());

        var trailing = TrailingSourceLine.Match(reply);
        return trailing.Success
            ? (reply[..trailing.Index].Trim(), trailing.Groups["title"].Value.Trim())
            : (reply, "");
    }

    private async Task<List<KnowledgeSearchResult>> FindSectionsAsync(
        string message, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken) =>
        (await knowledge.SearchAsync(BuildSearchQuery(message, history), _options.TopK, cancellationToken))
            .Where(s => s.Score >= _options.MinScore)
            .ToList();

    private static string BuildSearchQuery(string message, IReadOnlyList<ChatMessage> history)
    {
        var isFollowUp = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= FollowUpMaxWords;
        var previousQuestion = history.LastOrDefault(m => m.Role == "user")?.Content;

        return isFollowUp && previousQuestion is not null ? $"{previousQuestion} {message}" : message;
    }
}
