namespace TicketingAI.Api.Chat;

/// <summary>
/// The key for an external chat service (Gemini, Cloudflare Workers AI, ...), used when the chat provider setting is "api".
/// It is read from the environment variable ChatApi__ApiKey and is never stored in the database.
/// </summary>
public class ChatApiOptions
{
    public const string SectionName = "ChatApi";

    public string ApiKey { get; init; } = "";
}
