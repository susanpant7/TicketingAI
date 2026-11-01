namespace TicketingAI.Api.Knowledge;

/// <summary>
/// How the embedding model is called. BaseUrl with no path (http://localhost:11434) is Ollama's own API, and one with a path is an
/// online service in the OpenAI format (see <see cref="Settings.AiProviders.IsOllamaApi"/>). ApiKeyName is the environment variable
/// with the key of an online service. KeepAlive only applies to Ollama.
/// </summary>
public record EmbeddingModelOptions(string Model, string BaseUrl, string ApiKeyName, string KeepAlive);
