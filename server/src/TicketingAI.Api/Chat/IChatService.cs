namespace TicketingAI.Api.Chat;

public interface IChatService
{
    Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken);
}
