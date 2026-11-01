using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Knowledge;

/// <summary>Sends each embedding request to Ollama's own API or to an online service, depending on the embedding base URL.</summary>
public class EmbeddingClient(OllamaEmbeddingClient ollama, OnlineEmbeddingClient online) : IEmbeddingClient
{
    public Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, EmbeddingModelOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.Model))
            throw new EmbeddingException("No embedding model is selected yet. An admin chooses one on the Settings page (AI assistant tab), and then loads the knowledge files.");

        return (AiProviders.IsOllamaApi(options.BaseUrl) ? (IEmbeddingClient)ollama : online).EmbedAsync(texts, options, cancellationToken);
    }
}
