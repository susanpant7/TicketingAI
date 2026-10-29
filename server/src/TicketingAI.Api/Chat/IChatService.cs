namespace TicketingAI.Api.Chat;

public interface IChatService
{
    /// <summary>Answers a message (employeeId is null for the admin). If the employee agreed to a ticket, it is created.
    /// Answers a message: finds the matching knowledge, asks the chat model, and returns the answer and its department.</summary>
    Task<ChatResponse> AskAsync(ChatRequest request, int? employeeId, CancellationToken cancellationToken);
}
