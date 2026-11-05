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
    IChatHistoryRepository chatHistory,
    ILogger<ChatService> logger) : IChatService
{
    // The model is asked to start every reply with "Department: <a department name or none>". Small models sometimes put the line at the end instead, so both ends are accepted.
    private static readonly Regex LeadingDepartmentLine = new(@"^\s*Department:[ \t]*(?<name>[^\r\n]*)\r?\n?", RegexOptions.IgnoreCase);
    private static readonly Regex TrailingDepartmentLine = new(@"\s*Department:[ \t]*(?<name>[^\r\n]*)\s*$", RegexOptions.IgnoreCase);

    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    // The same lines when the model wrote them at the end of the reply, after the answer, instead of before it.
    private static readonly Regex TrailingTicketLine = new(@"(?:\r?\n|^)[ \t]*(?<key>Action|Title|Description|Problem):[ \t]*(?<value>[^\r\n]*)\s*$", RegexOptions.IgnoreCase);
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
            settings.ChatBaseUrl, settings.ChatApiKeyName, settings.ChatReasoningEffort);

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
            "{BaseUrl} {Model}, prompt {PromptChars} characters, reply {ReplyChars} characters",
            searchMs, sections.Count, firstTextMs, clock.ElapsedMilliseconds,
            chatOptions.BaseUrl, chatOptions.Model, prompt.Sum(m => m.Content.Length), reply.Length);

        var response = BuildResponse(reply.Trim(), departments);
        var lastReply = conversation.LastOrDefault(m => m.Role == "assistant")?.Content;
        var final = await CreateTicketIfAgreedAsync(response, employeeId, departments, offeredJustBefore: OfferEnd.IsMatch(lastReply ?? ""));

        // The chat is saved only when it was answered, so a failed message leaves nothing half-saved. The reply is what the
        // employee read (with the ticket sentence, without the header lines).
        await chatHistory.AddAsync(employeeId, message, final);
        return final;
    }

    private const int HistoryCount = 100;

    public Task<List<ChatHistoryItem>> GetHistoryAsync(int? employeeId) => chatHistory.GetRecentAsync(employeeId, HistoryCount);

    public async Task ClearHistoryAsync(int? employeeId) => await chatHistory.DeleteForUserAsync(employeeId);

    public Task<int> ClearAllHistoryAsync() => chatHistory.DeleteAllAsync();

    /// <summary>
    /// The reply can carry an "Action:" line: offer_ticket (the model says a ticket would help), confirm_ticket (the employee agreed to
    /// the offer in the last reply) or create_ticket (the employee asked for a ticket in their own words), with "Title:" and
    /// "Description:" lines for the last two. The lines are taken off the reply. The server writes every ticket offer itself, as the same
    /// last sentence (<see cref="OfferSentence"/>), so it can tell later whether the last reply was an offer. The server (not the model)
    /// creates the ticket and tells the employee who got it. A confirmation counts only when the last reply was such an offer, so a
    /// "yes" or "ok" after an unrelated exchange never creates a ticket.
    /// </summary>
    private async Task<ChatResponse> CreateTicketIfAgreedAsync(
        ChatResponse response, int? employeeId, IReadOnlyList<DepartmentDto> departments, bool offeredJustBefore)
    {
        var lines = SplitTicketLines(response.Answer);
        var department = departments.FirstOrDefault(d => d.Name == response.Department);
        if (lines.Draft is null)
        {
            // The model only labels the message: Problem: yes (an answer to a problem report) or Action: offer_ticket (the problem is
            // not solved). The server adds the offer, so the wording is fixed and the model cannot add it by habit to an ordinary question.
            if (employeeId is not null && department is not null)
            {
                if (lines.IsProblem)
                    return response with { Answer = $"{lines.Answer}\n\nIf this does not solve the issue, {OfferSentence(department.Name)}" };
                if (lines.Action == OfferTicket)
                    return response with { Answer = lines.Answer.Length == 0 ? OfferSentence(department.Name) : $"{lines.Answer}\n\n{OfferSentence(department.Name)}" };
            }
            return response with { Answer = lines.Answer };
        }

        if (employeeId is null)
            return response with { Answer = "Tickets can only be created for employees. Please log in with an employee account." };

        if (department is null)
            return response with { Answer = "I could not tell which department the ticket is for. Please tell me which one it should go to." };

        if (lines.Action == ConfirmTicket && !offeredJustBefore)
        {
            // A yes or ok, but the last reply was not an offer: do not create a ticket by mistake. Offer it, so the next yes counts.
            return response with { Answer = $"I did not offer a ticket just now. {OfferSentence(department.Name)}" };
        }

        var ticket = await ticketService.CreateAsync(employeeId.Value, department.Id, lines.Draft.Title, lines.Draft.Description);
        if (ticket is null)
            return response with { Answer = $"I could not create the ticket, because the {department.Name} department has no members yet." };

        var created = $"Ticket #{ticket.Id} was created and assigned to {ticket.AssignedTo} ({ticket.Department}).";
        return response with { Answer = lines.Answer.Length == 0 ? created : $"{lines.Answer}\n\n{created}" };
    }

    private const string OfferTicket = "offer_ticket";
    private const string ConfirmTicket = "confirm_ticket";
    private const string CreateTicket = "create_ticket";

    /// <summary>The last sentence of every reply that offers a ticket.</summary>
    private static string OfferSentence(string departmentName) => $"I can create a ticket for the {departmentName} department. Let me know.";

    // A reply is an offer when it ends with the sentence above.
    private static readonly Regex OfferEnd = new(@"I can create a ticket for the .+ department\. Let me know\.\s*$", RegexOptions.Singleline);

    private static readonly string[] HeaderKeys = ["Department:", "Problem:", "Action:", "Title:", "Description:"];

    /// <summary>
    /// Takes off the header lines a model wrote after the answer (such as "Problem: no"), also while the last line is still being
    /// written, so the employee never sees them, not even for a moment.
    /// </summary>
    private static string WithoutTrailingHeaders(string text)
    {
        var lines = text.TrimEnd().Split('\n').ToList();
        while (lines.Count > 0)
        {
            var last = lines[^1].Trim();
            var mightBeHeader = last.Length == 0 || HeaderKeys.Any(key =>
                last.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(last, StringComparison.OrdinalIgnoreCase));
            if (!mightBeHeader) break;
            lines.RemoveAt(lines.Count - 1);
        }
        return string.Join('\n', lines).Trim();
    }


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
            if (!mightBeHeader) return WithoutTrailingHeaders(rest);
            if (lineEnd < 0) return "";
            rest = rest[(lineEnd + 1)..].TrimStart();
        }
    }

    private record TicketDraft(string Title, string Description);

    private record TicketLines(string Answer, string? Action, TicketDraft? Draft, bool IsProblem);

    private static TicketLines SplitTicketLines(string answer)
    {
        string? action = null, title = null, description = null, problem = null;
        var rest = answer;

        void Take(Match match)
        {
            var value = match.Groups["value"].Value.Trim();
            switch (match.Groups["key"].Value.ToLowerInvariant())
            {
                case "action": action ??= value.ToLowerInvariant(); break;
                case "title": title ??= value; break;
                case "problem": problem ??= value; break;
                default: description ??= value; break;
            }
        }

        for (var match = TicketLine.Match(rest); match.Success; match = TicketLine.Match(rest))
        {
            Take(match);
            rest = rest[match.Length..];
        }

        // A model sometimes writes these lines after the answer. They are protocol lines, never part of the answer, so they come off too.
        for (var match = TrailingTicketLine.Match(rest); match.Success; match = TrailingTicketLine.Match(rest))
        {
            Take(match);
            rest = rest[..match.Index];
        }

        if (action is null && title is null && description is null && problem is null)
            return new TicketLines(answer, null, null, false);

        var draft = action is CreateTicket or ConfirmTicket && !string.IsNullOrWhiteSpace(title)
            ? new TicketDraft(Clip(title, MaxTitleLength), Clip(string.IsNullOrWhiteSpace(description) ? title : description, MaxDescriptionLength))
            : null;
        return new TicketLines(rest.Trim(), action, draft, problem?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true);
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
        string message, IReadOnlyList<ChatMessage> history, AiSettings settings, CancellationToken cancellationToken)
    {
        // A section found by more than one query counts with its best score.
        var best = new Dictionary<int, KnowledgeSearchResult>();
        foreach (var query in await BuildSearchQueriesAsync(message, history, settings, cancellationToken))
        {
            foreach (var section in await knowledge.SearchAsync(query, settings.TopK, cancellationToken))
            {
                if (!best.TryGetValue(section.Id, out var seen) || section.Score > seen.Score)
                    best[section.Id] = section;
            }
        }

        return KeepClosest(
            best.Values.Where(s => s.Score >= settings.MinScore).OrderByDescending(s => s.Score).Take(settings.TopK).ToList(),
            settings.MaxScoreGap);
    }

    // Drops the chunks that score well below the best one: fewer chunks make a shorter prompt, and a shorter prompt is a faster reply.
    private static List<KnowledgeSearchResult> KeepClosest(List<KnowledgeSearchResult> sections, double maxGap)
    {
        if (maxGap <= 0 || sections.Count == 0)
            return sections;

        var best = sections.Max(s => s.Score);
        return sections.Where(s => best - s.Score <= maxGap).ToList();
    }

    // A short message can be a follow-up ("and for sick leave?") that only makes sense with the earlier messages, or a new
    // question ("how to contact it support") that stands alone, and a word count cannot tell them apart. So for a short
    // message the chat model first writes it as one complete question from the last messages (a new question comes back
    // unchanged), and that question is searched, with the message itself as a second query. When the rewrite is off or
    // fails, the message is searched alone and together with the previous question instead. Either way a section found by
    // more than one query counts with its best score, so a new question is never pulled towards the previous topic.
    private async Task<List<string>> BuildSearchQueriesAsync(
        string message, IReadOnlyList<ChatMessage> history, AiSettings settings, CancellationToken cancellationToken)
    {
        var isShort = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= settings.FollowUpMaxWords;
        var previousQuestion = history.LastOrDefault(m => m.Role == "user")?.Content;
        if (!isShort || previousQuestion is null)
            return [message];

        if (settings.RewriteFollowUps > 0)
        {
            var rewritten = await RewriteAsync(message, history, settings, cancellationToken);
            if (rewritten is not null)
                return rewritten.Equals(message, StringComparison.OrdinalIgnoreCase) ? [message] : [rewritten, message];
        }

        return [message, $"{previousQuestion} {message}"];
    }

    private const int RewriteContextMessages = 4;
    private const int RewriteContextChars = 300;
    private static readonly TimeSpan RewriteTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Asks the chat model to write the message as one complete question, from the last messages. Null when that did not work (a
    /// failure, a timeout, or a reply that is not a question), so the caller can search another way.
    /// </summary>
    private async Task<string?> RewriteAsync(
        string message, IReadOnlyList<ChatMessage> history, AiSettings settings, CancellationToken cancellationToken)
    {
        // The last few messages, each cut short: an answer can be long, and only its topic matters here.
        var conversation = string.Join("\n", history.TakeLast(RewriteContextMessages).Select(m =>
        {
            var text = m.Content.Length > RewriteContextChars ? m.Content[..RewriteContextChars] + "..." : m.Content;
            return $"{(m.Role == "user" ? "Employee" : "Assistant")}: {text}";
        }));
        var prompt = new List<ChatMessage>
        {
            new("system", settings.RewritePrompt),
            new("user", $"Conversation:\n{conversation}\n\nLast message: {message}")
        };
        var options = new ChatModelOptions(
            settings.ChatModel, 0, settings.TopP, settings.NumCtx, settings.MaxTokens, settings.KeepAlive,
            settings.ChatBaseUrl, settings.ChatApiKeyName, settings.ChatReasoningEffort);

        var clock = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RewriteTimeout);
        try
        {
            var reply = await model.ChatAsync(prompt, options, timeout.Token);

            // Only the first line, without quotes: the instruction asks for the question alone.
            var question = reply.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim().Trim('"', '\'', '`') ?? "";
            if (question.Length == 0 || question.Length > 300)
                return null;

            logger.LogInformation("Search query rewritten in {Ms} ms: \"{Message}\" -> \"{Question}\"", clock.ElapsedMilliseconds, message, question);
            return question;
        }
        catch (Exception ex) when (ex is ChatModelException or EmbeddingException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning("The follow-up was not rewritten ({Error}); it is searched as it is", ex.Message);
            return null;
        }
    }
}
