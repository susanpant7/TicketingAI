namespace TicketingAI.Api.Chat;

public interface IChatService
{
    /// <summary>Answers a message, streaming the reply as it is written and ending with the final answer and sources.</summary>
    IAsyncEnumerable<ChatStreamEvent> AskAsync(ChatRequest request, CancellationToken cancellationToken);
}
