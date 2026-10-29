namespace TicketingAI.Api.Chat;

/// <summary>The final reply. Department is "HR", "IT" or "Finance" when the reply relates to one, otherwise null.</summary>
public record ChatResponse(string Answer, string? Department);
