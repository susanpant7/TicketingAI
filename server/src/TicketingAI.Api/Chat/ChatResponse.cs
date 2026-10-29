namespace TicketingAI.Api.Chat;

public record ChatSource(string DocumentTitle, string Heading, string Category, double Score);

public record ChatResponse(string Answer, List<ChatSource> Sources);
