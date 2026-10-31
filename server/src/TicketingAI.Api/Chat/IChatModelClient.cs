namespace TicketingAI.Api.Chat;

public interface IChatModelClient
{
    /// <summary>
    /// Sends the messages to the chat model and returns its complete reply. The reply is streamed: onText, when given,
    /// is called with the reply so far each time more of it arrives.
    /// </summary>
    Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken,
        Func<string, Task>? onText = null);
}
