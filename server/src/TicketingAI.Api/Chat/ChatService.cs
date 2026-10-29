using System.Text.RegularExpressions;
using TicketingAI.Api.Knowledge;
using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Chat;

public class ChatService(
    IKnowledgeService knowledge,
    IChatModelClient model,
    ISettingsService settingsService) : IChatService
{
    // The departments the model may name. They must match the categories of the knowledge documents.
    private static readonly string[] Departments = ["HR", "IT", "Finance"];

    // The model is asked to start every reply with "Department: <HR, IT, Finance or none>". Small models sometimes put the line at the end instead, so both ends are accepted.
    private static readonly Regex LeadingDepartmentLine = new(@"^\s*Department:[ \t]*(?<name>[^\r\n]*)\r?\n?", RegexOptions.IgnoreCase);
    private static readonly Regex TrailingDepartmentLine = new(@"\s*Department:[ \t]*(?<name>[^\r\n]*)\s*$", RegexOptions.IgnoreCase);

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        // Read the settings first, so a change in the Settings table applies to this message.
        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);

        var message = request.Message.Trim();
        var history = (request.History ?? [])
            .Where(m => m.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(settings.HistoryMessages)
            .ToList();

        var sections = await FindSectionsAsync(message, history, settings, cancellationToken);

        // The model always answers. The prompt tells it what to do when the sections answer the question, when they do not, and for casual chat.
        var prompt = ChatPromptBuilder.Build(history, sections, message, settings.SystemPrompt, settings.FormatReminder);
        var chatOptions = new ChatModelOptions(
            settings.ChatModel, settings.Temperature, settings.TopP, settings.NumCtx, settings.MaxTokens);

        var reply = await model.ChatAsync(prompt, chatOptions, cancellationToken);

        return BuildResponse(reply.Trim());
    }

    /// <summary>Takes the "Department:" line off the reply. Anything other than HR, IT or Finance (such as "none") means no department.</summary>
    private static ChatResponse BuildResponse(string reply)
    {
        var (answer, name) = SplitDepartmentLine(reply);
        if (answer.Length == 0)
            return new ChatResponse(reply, null);

        var department = Departments.FirstOrDefault(d => d.Equals(name, StringComparison.OrdinalIgnoreCase));
        return new ChatResponse(answer, department);
    }

    private static (string Answer, string Name) SplitDepartmentLine(string reply)
    {
        var leading = LeadingDepartmentLine.Match(reply);
        if (leading.Success)
            return (reply[leading.Length..].Trim(), leading.Groups["name"].Value.Trim());

        var trailing = TrailingDepartmentLine.Match(reply);
        return trailing.Success
            ? (reply[..trailing.Index].Trim(), trailing.Groups["name"].Value.Trim())
            : (reply, "");
    }

    private async Task<List<KnowledgeSearchResult>> FindSectionsAsync(
        string message, IReadOnlyList<ChatMessage> history, AiSettings settings, CancellationToken cancellationToken) =>
        (await knowledge.SearchAsync(BuildSearchQuery(message, history, settings.FollowUpMaxWords), settings.TopK, cancellationToken))
            .Where(s => s.Score >= settings.MinScore)
            .ToList();

    // Messages this short are usually follow-ups ("and for sick leave?") that only make sense with the previous question.
    private static string BuildSearchQuery(string message, IReadOnlyList<ChatMessage> history, int followUpMaxWords)
    {
        var isFollowUp = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= followUpMaxWords;
        var previousQuestion = history.LastOrDefault(m => m.Role == "user")?.Content;

        return isFollowUp && previousQuestion is not null ? $"{previousQuestion} {message}" : message;
    }
}
