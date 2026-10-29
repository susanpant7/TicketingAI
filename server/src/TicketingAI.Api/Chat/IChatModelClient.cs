namespace TicketingAI.Api.Chat;

public interface IChatModelClient
{
    /// <summary>Streams the model's reply as it is written, in small pieces.</summary>
    IAsyncEnumerable<string> ChatStreamAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);
}
