using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Chat;

/// <summary>Sends each chat request to Ollama's own API or to an online service, depending on the Chat API base URL.</summary>
public class ChatModelClient(OllamaChatClient ollama, ApiChatClient api) : IChatModelClient
{
    public Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken,
        Func<string, Task>? onText = null)
    {
        IChatModelClient client = AiProviders.IsOllamaApi(chat.BaseUrl) ? ollama : api;
        return client.ChatAsync(messages, chat, cancellationToken, onText);
    }
}
