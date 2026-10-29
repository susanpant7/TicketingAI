namespace TicketingAI.Api.Chat;

public interface IChatModelClient
{
    /// <summary>Sends the messages to the chat model and returns its complete reply.</summary>
    Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken);
}
