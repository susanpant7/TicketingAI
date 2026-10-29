namespace TicketingAI.Api.Chat;

public interface IChatService
{
    /// <summary>Answers a message: finds the matching knowledge, asks the chat model, and returns the answer and its department.</summary>
    Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken);
}
