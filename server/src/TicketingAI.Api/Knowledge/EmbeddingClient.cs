using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Knowledge;

/// <summary>Sends each embedding request to Ollama's own API or to an online service, depending on the embedding base URL.</summary>
public class EmbeddingClient(OllamaEmbeddingClient ollama, OnlineEmbeddingClient online) : IEmbeddingClient
{
    public Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, EmbeddingModelOptions options, CancellationToken cancellationToken = default) =>
        (AiProviders.IsOllamaApi(options.BaseUrl) ? (IEmbeddingClient)ollama : online).EmbedAsync(texts, options, cancellationToken);
}
