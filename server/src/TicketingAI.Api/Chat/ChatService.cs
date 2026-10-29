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

    private const string NotFoundReply =
        "I couldn't find anything about that in the company documents. Would you like me to raise a support ticket so the right team can help?";

    private readonly ChatOptions _options = options.Value;

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var message = request.Message.Trim();
        var history = (request.History ?? [])
            .Where(m => m.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(_options.HistoryMessages)
            .ToList();

        var sections = (await knowledge.SearchAsync(BuildSearchQuery(message, history), _options.TopK, cancellationToken))
            .Where(s => s.Score >= _options.MinScore)
            .ToList();

        // Nothing related at all: skip the (slow) model call.
        if (sections.Count == 0)
            return new ChatResponse(NotFoundReply, []);

        var answer = await model.ChatAsync(ChatPromptBuilder.Build(history, sections, message), cancellationToken);

        return new ChatResponse(
            answer.Trim(),
            sections.Select(s => new ChatSource(s.DocumentTitle, s.Heading, s.Category, Math.Round(s.Score, 3))).ToList());
    }

    private static string BuildSearchQuery(string message, IReadOnlyList<ChatMessage> history)
    {
        var isFollowUp = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= FollowUpMaxWords;
        var previousQuestion = history.LastOrDefault(m => m.Role == "user")?.Content;

        return isFollowUp && previousQuestion is not null ? $"{previousQuestion} {message}" : message;
    }
}
