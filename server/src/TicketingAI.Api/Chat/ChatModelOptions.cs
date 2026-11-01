namespace TicketingAI.Api.Chat;

/// <summary>
/// How the chat model is called. The values come from the Settings table. Provider is "ollama" (a local model) or "api"
/// (an external service with an OpenAI-style chat endpoint). BaseUrl is where the chat model is called, for both. KeepAlive and NumCtx only apply to Ollama.
/// </summary>
public record ChatModelOptions(string Model, double Temperature, double TopP, int NumCtx, int MaxTokens, string KeepAlive,
    string Provider, string BaseUrl, string ReasoningEffort);
