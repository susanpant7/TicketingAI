namespace TicketingAI.Api.Chat;

/// <summary>
/// How the chat model is called. The values come from the Settings table. BaseUrl is where the chat model is called: an
/// address with no path is Ollama's own API, and one with a path is an online service in the OpenAI chat format (see
/// <see cref="AiProviders.IsOllamaApi"/>). KeepAlive and NumCtx only apply to Ollama.
/// </summary>
public record ChatModelOptions(string Model, double Temperature, double TopP, int NumCtx, int MaxTokens, string KeepAlive,
    string BaseUrl, string ReasoningEffort);
