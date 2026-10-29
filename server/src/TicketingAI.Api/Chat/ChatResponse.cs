namespace TicketingAI.Api.Chat;

/// <summary>The final reply. Department is the name of a department from the Departments table when the reply relates to one, otherwise null.</summary>
public record ChatResponse(string Answer, string? Department);
