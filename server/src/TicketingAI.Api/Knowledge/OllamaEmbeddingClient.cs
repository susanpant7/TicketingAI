using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace TicketingAI.Api.Knowledge;

public class OllamaEmbeddingClient(HttpClient http, IOptions<OllamaOptions> options) : IEmbeddingClient
{
    private const int BatchSize = 16;

    private readonly OllamaOptions _options = options.Value;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var vectors = new List<float[]>(texts.Count);
        foreach (var batch in texts.Chunk(BatchSize))
            vectors.AddRange(await EmbedBatchAsync(batch, cancellationToken));

        return vectors;
    }

    private async Task<float[][]> EmbedBatchAsync(string[] batch, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(
                "/api/embed", new { model = _options.EmbeddingModel, input = batch }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingException(
                $"Cannot reach Ollama at {_options.BaseUrl}. Make sure Ollama is running.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new EmbeddingException(
                    $"Ollama returned {(int)response.StatusCode} for model '{_options.EmbeddingModel}': {detail}. " +
                    $"If the model is missing, run: ollama pull {_options.EmbeddingModel}");
            }

            var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken);
            if (body?.Embeddings is null || body.Embeddings.Length != batch.Length)
                throw new EmbeddingException("Ollama returned an unexpected number of embeddings.");

            return body.Embeddings;
        }
    }

    private record EmbedResponse(float[][] Embeddings);
}
