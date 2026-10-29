using System.Text;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

public static class ChatPromptBuilder
{
    /// <summary>
    /// The system prompt and the format reminder come from the Settings table. The system prompt is the first message
    /// and stays the same between requests, so Ollama can reuse its cached copy of the start of the prompt.
    /// </summary>
    public static List<ChatMessage> Build(
        IReadOnlyList<ChatMessage> history, IReadOnlyList<KnowledgeSearchResult> sections, string question,
        string systemPrompt, string formatReminder)
    {
        var messages = new List<ChatMessage> { new("system", systemPrompt) };
        messages.AddRange(history);
        messages.Add(new ChatMessage("user", BuildQuestionMessage(sections, question, formatReminder)));
        return messages;
    }

    private static string BuildQuestionMessage(
        IReadOnlyList<KnowledgeSearchResult> sections, string question, string formatReminder)
    {
        if (sections.Count == 0)
            return $"No company documents matched this message.\n\nMessage: {question}\n\n{formatReminder}";

        var text = new StringBuilder("Sections:\n\n");
        foreach (var section in sections)
            text.Append($"[{section.DocumentTitle} - {section.Heading} | {section.Category}]\n{section.Content}\n\n");

        text.Append($"Message: {question}\n\n{formatReminder}");
        return text.ToString();
    }
}
