using System.Text.RegularExpressions;
using TicketingAI.Api.Departments;
using TicketingAI.Api.Knowledge;
using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Chat;

public class ChatService(
    IKnowledgeService knowledge,
    IChatModelClient model,
    ISettingsService settingsService,
    IDepartmentRepository departmentRepository) : IChatService
{
    // The model is asked to start every reply with "Department: <a department name or none>". Small models sometimes put the line at the end instead, so both ends are accepted.
    private static readonly Regex LeadingDepartmentLine = new(@"^\s*Department:[ \t]*(?<name>[^\r\n]*)\r?\n?", RegexOptions.IgnoreCase);
    private static readonly Regex TrailingDepartmentLine = new(@"\s*Department:[ \t]*(?<name>[^\r\n]*)\s*$", RegexOptions.IgnoreCase);

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        // Read the settings first, so a change in the Settings table applies to this message.
        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);

        // The departments are read from the Departments table, so the prompt and the check of the reply follow any change made there.
        var departments = await departmentRepository.GetAllAsync();

        var message = request.Message.Trim();
        var history = (request.History ?? [])
            .Where(m => m.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(settings.HistoryMessages)
            .ToList();

        var sections = await FindSectionsAsync(message, history, settings, cancellationToken);

        // The model always answers. The prompt tells it what to do when the sections answer the question, when they do not, and for casual chat.
        var prompt = ChatPromptBuilder.Build(
            history, sections, message,
            FillDepartments(settings.SystemPrompt, departments), FillDepartments(settings.FormatReminder, departments));
        var chatOptions = new ChatModelOptions(
            settings.ChatModel, settings.Temperature, settings.TopP, settings.NumCtx, settings.MaxTokens);

        var reply = await model.ChatAsync(prompt, chatOptions, cancellationToken);

        return BuildResponse(reply.Trim(), departments);
    }

    /// <summary>
    /// Fills the departments into a prompt text. {{departments}} becomes one line per department (name, what it handles, how to
    /// reach it) and {{department_names}} becomes the names. A text without these placeholders is used as it is.
    /// </summary>
    private static string FillDepartments(string text, IReadOnlyList<DepartmentDto> departments)
    {
        const string none = "(no departments are set up)";

        var names = departments.Count == 0 ? none : string.Join(", ", departments.Select(d => d.Name));
        var lines = departments.Count == 0 ? $"- {none}" : string.Join("\n   ", departments.Select(Describe));

        return text.Replace("{{departments}}", lines).Replace("{{department_names}}", names);
    }

    private static string Describe(DepartmentDto d) =>
        $"- {d.Name}{(d.Description.Length > 0 ? $" ({d.Description})" : "")}{(d.Contact.Length > 0 ? $": {d.Contact}" : "")}";

    /// <summary>Takes the "Department:" line off the reply. Anything that is not the name of a department (such as "none") means no department.</summary>
    private static ChatResponse BuildResponse(string reply, IReadOnlyList<DepartmentDto> departments)
    {
        var (answer, name) = SplitDepartmentLine(reply);
        if (answer.Length == 0)
            return new ChatResponse(reply, null);

        var department = departments.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Name;
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
