namespace TicketingAI.Api.Chat;

/// <summary>Sends each chat request to the provider chosen in the settings: a local Ollama model, or an external API.</summary>
public class ChatModelClient(OllamaChatClient ollama, ApiChatClient api) : IChatModelClient
{
    public Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken,
        Func<string, Task>? onText = null)
    {
        IChatModelClient client = chat.Provider.Equals("api", StringComparison.OrdinalIgnoreCase) ? api : ollama;
        return client.ChatAsync(messages, chat, cancellationToken, onText);
    }
}
