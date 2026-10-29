namespace TicketingAI.Api.Chat;

/// <summary>One line of the chat stream: text as it is written, then the final answer with its sources, or an error.</summary>
public record ChatStreamEvent(string Type, string? Text = null, ChatResponse? Response = null, string? Message = null)
{
    public static ChatStreamEvent Token(string text) => new("token", Text: text);

    public static ChatStreamEvent Done(ChatResponse response) => new("done", Response: response);

    public static ChatStreamEvent Error(string message) => new("error", Message: message);
}
