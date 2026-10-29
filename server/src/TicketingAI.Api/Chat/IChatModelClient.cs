namespace TicketingAI.Api.Chat;

public interface IChatModelClient
{
    Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);
}
