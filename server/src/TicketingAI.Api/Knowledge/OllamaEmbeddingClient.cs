using System.Net.Http.Json;

namespace TicketingAI.Api.Knowledge;

public class OllamaEmbeddingClient(HttpClient http) : IEmbeddingClient
{
    private const int BatchSize = 16;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, EmbeddingModelOptions options, CancellationToken cancellationToken = default)
    {
        var vectors = new List<float[]>(texts.Count);
        foreach (var batch in texts.Chunk(BatchSize))
            vectors.AddRange(await EmbedBatchAsync(batch, options.Model, options.BaseUrl, options.KeepAlive, cancellationToken));

        return vectors;
    }

    private async Task<float[][]> EmbedBatchAsync(string[] batch, string model, string ollamaUrl, string keepAlive, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(
                ollamaUrl.TrimEnd('/') + "/api/embed", new { model, input = batch, keep_alive = keepAlive }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingException(
                $"Cannot reach Ollama at {ollamaUrl}. Make sure Ollama is running.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new EmbeddingException(
                    $"Ollama returned {(int)response.StatusCode} for model '{model}': {detail}. " +
                    $"If the model is missing, run: ollama pull {model}");
            }

            var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken);
            if (body?.Embeddings is null || body.Embeddings.Length != batch.Length)
                throw new EmbeddingException("Ollama returned an unexpected number of embeddings.");

            return body.Embeddings;
        }
    }

    private record EmbedResponse(float[][] Embeddings);
}
