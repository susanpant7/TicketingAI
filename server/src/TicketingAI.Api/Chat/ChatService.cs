using System.Diagnostics;
using System.Text.RegularExpressions;
using TicketingAI.Api.Departments;
using TicketingAI.Api.Knowledge;
using TicketingAI.Api.Settings;
using TicketingAI.Api.Tickets;

namespace TicketingAI.Api.Chat;

public class ChatService(
    IKnowledgeService knowledge,
    IChatModelClient model,
    ISettingsService settingsService,
    IDepartmentRepository departmentRepository,
    ITicketService ticketService,
    ILogger<ChatService> logger) : IChatService
{
    // The model is asked to start every reply with "Department: <a department name or none>". Small models sometimes put the line at the end instead, so both ends are accepted.
    private static readonly Regex LeadingDepartmentLine = new(@"^\s*Department:[ \t]*(?<name>[^\r\n]*)\r?\n?", RegexOptions.IgnoreCase);
    private static readonly Regex TrailingDepartmentLine = new(@"\s*Department:[ \t]*(?<name>[^\r\n]*)\s*$", RegexOptions.IgnoreCase);

    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    private static readonly Regex TicketLine = new(@"^\s*(?<key>Action|Title|Description|Problem):[ \t]*(?<value>[^\r\n]*)\r?\n?", RegexOptions.IgnoreCase);

    public async Task<ChatResponse> AskAsync(
        ChatRequest request, int? employeeId, CancellationToken cancellationToken, Func<string, Task>? onDraft = null)
    {
        // Read the settings first, so a change in the Settings table applies to this message.
        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);

        // The departments are read from the Departments table, so the prompt and the check of the reply follow any change made there.
        var departments = await departmentRepository.GetAllAsync();

        var message = request.Message.Trim();
        var conversation = (request.History ?? [])
            .Where(m => m.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Content))
            .ToList();

        // The model only gets the last few messages (a small prompt is a fast reply), but a short follow-up is still searched together
        // with the previous question from the whole conversation the page sent.
        var history = conversation.TakeLast(settings.HistoryMessages).ToList();

        var clock = Stopwatch.StartNew();
        var sections = await FindSectionsAsync(message, conversation, settings, cancellationToken);
        var searchMs = clock.ElapsedMilliseconds;

        // The model always answers. The prompt tells it what to do when the sections answer the question, when they do not, and for casual chat.
        var prompt = ChatPromptBuilder.Build(
            history, sections, message,
            FillDepartments(settings.SystemPrompt, departments), FillDepartments(settings.FormatReminder, departments));
        var chatOptions = new ChatModelOptions(
            settings.ChatModel, settings.Temperature, settings.TopP, settings.NumCtx, settings.MaxTokens, settings.KeepAlive,
            settings.ChatProvider, settings.ChatBaseUrl, settings.ChatReasoningEffort);

        // While the reply is written, the employee sees it without the Department, Problem and ticket lines.
        var shown = "";
        long? firstTextMs = null;
        clock.Restart();
        var reply = await model.ChatAsync(prompt, chatOptions, cancellationToken, async soFar =>
        {
            firstTextMs ??= clock.ElapsedMilliseconds;
            if (onDraft is null) return;

            var draft = VisibleDraft(soFar);
            if (draft.Length > 0 && draft != shown)
            {
                shown = draft;
                await onDraft(draft);
            }
        });

        // Where the time goes: the search (embedding the question and the vector query), the wait for the first words of the
        // model's reply (it reads the prompt first, and some models think first), and the time it takes to write the whole reply.
        logger.LogInformation(
            "Chat timing: search {SearchMs} ms ({Sections} sections), first text after {FirstMs} ms, reply complete after {TotalMs} ms; " +
            "{Provider} {Model}, prompt {PromptChars} characters, reply {ReplyChars} characters",
            searchMs, sections.Count, firstTextMs, clock.ElapsedMilliseconds,
            chatOptions.Provider, chatOptions.Model, prompt.Sum(m => m.Content.Length), reply.Length);

        var response = BuildResponse(reply.Trim(), departments);
        return await CreateTicketIfAgreedAsync(response, employeeId, departments);
    }

    /// <summary>
    /// When the employee agreed to a ticket, the reply has "Action: create_ticket", "Title:" and "Description:" lines. They are taken
    /// off the reply, and the server (not the model) creates the ticket and tells the employee who got it.
    /// </summary>
    private async Task<ChatResponse> CreateTicketIfAgreedAsync(
        ChatResponse response, int? employeeId, IReadOnlyList<DepartmentDto> departments)
    {
        var (answer, draft, isProblem) = SplitTicketLines(response.Answer);
        if (draft is null)
        {
            // The model only labels the message as a problem report. The server adds the ticket offer, so the wording is fixed
            // and the model cannot add it by habit to an ordinary question.
            var offerTo = departments.FirstOrDefault(d => d.Name == response.Department)?.Name;
            return isProblem && employeeId is not null && offerTo is not null
                ? response with { Answer = $"{answer}\n\nIf this does not solve the issue, I can create a ticket for the {offerTo} department. Let me know." }
                : response with { Answer = answer };
        }

        if (employeeId is null)
            return response with { Answer = "Tickets can only be created for employees. Please log in with an employee account." };

        var department = departments.FirstOrDefault(d => d.Name == response.Department);
        if (department is null)
            return response with { Answer = "I could not tell which department the ticket is for. Please tell me which one it should go to." };

        var ticket = await ticketService.CreateAsync(employeeId.Value, department.Id, draft.Title, draft.Description);
        if (ticket is null)
            return response with { Answer = $"I could not create the ticket, because the {department.Name} department has no members yet." };

        var created = $"Ticket #{ticket.Id} was created and assigned to {ticket.AssignedTo} ({ticket.Department}).";
        return response with { Answer = answer.Length == 0 ? created : $"{answer}\n\n{created}" };
    }

    private static readonly string[] HeaderKeys = ["Department:", "Problem:", "Action:", "Title:", "Description:"];

    /// <summary>
    /// The part of an unfinished reply the employee may see: the reply without its leading header lines. Until a line is known
    /// not to be a header (it is complete, or no longer starts like one), nothing more is shown.
    /// </summary>
    private static string VisibleDraft(string soFar)
    {
        var rest = soFar.TrimStart();
        while (true)
        {
            var lineEnd = rest.IndexOf('\n');
            var line = lineEnd < 0 ? rest : rest[..lineEnd];
            var mightBeHeader = HeaderKeys.Any(key =>
                line.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(line, StringComparison.OrdinalIgnoreCase));
            if (!mightBeHeader) return rest.Trim();
            if (lineEnd < 0) return "";
            rest = rest[(lineEnd + 1)..].TrimStart();
        }
    }

    private record TicketDraft(string Title, string Description);

    private static (string Answer, TicketDraft? Draft, bool IsProblem) SplitTicketLines(string answer)
    {
        string? action = null, title = null, description = null, problem = null;
        var rest = answer;

        for (var match = TicketLine.Match(rest); match.Success; match = TicketLine.Match(rest))
        {
            var value = match.Groups["value"].Value.Trim();
            switch (match.Groups["key"].Value.ToLowerInvariant())
            {
                case "action": action = value; break;
                case "title": title = value; break;
                case "problem": problem = value; break;
                default: description = value; break;
            }
            rest = rest[match.Length..];
        }

        if (action is null && title is null && description is null && problem is null)
            return (answer, null, false);

        var draft = action?.Equals("create_ticket", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrWhiteSpace(title)
            ? new TicketDraft(Clip(title, MaxTitleLength), Clip(string.IsNullOrWhiteSpace(description) ? title : description, MaxDescriptionLength))
            : null;
        return (rest.Trim(), draft, problem?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..max];

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
        KeepClosest(
            (await knowledge.SearchAsync(BuildSearchQuery(message, history, settings.FollowUpMaxWords), settings.TopK, cancellationToken))
                .Where(s => s.Score >= settings.MinScore)
                .ToList(),
            settings.MaxScoreGap);

    // Drops the chunks that score well below the best one: fewer chunks make a shorter prompt, and a shorter prompt is a faster reply.
    private static List<KnowledgeSearchResult> KeepClosest(List<KnowledgeSearchResult> sections, double maxGap)
    {
        if (maxGap <= 0 || sections.Count == 0)
            return sections;

        var best = sections.Max(s => s.Score);
        return sections.Where(s => best - s.Score <= maxGap).ToList();
    }

    // Messages this short are usually follow-ups ("and for sick leave?") that only make sense with the previous question.
    private static string BuildSearchQuery(string message, IReadOnlyList<ChatMessage> history, int followUpMaxWords)
    {
        var isFollowUp = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= followUpMaxWords;
        var previousQuestion = history.LastOrDefault(m => m.Role == "user")?.Content;

        return isFollowUp && previousQuestion is not null ? $"{previousQuestion} {message}" : message;
    }
}
