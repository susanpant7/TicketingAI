namespace TicketingAI.Api.Chat;

/// <summary>How the chat model is called. The values come from the Settings table.</summary>
public record ChatModelOptions(string Model, double Temperature, double TopP, int NumCtx, int MaxTokens);
