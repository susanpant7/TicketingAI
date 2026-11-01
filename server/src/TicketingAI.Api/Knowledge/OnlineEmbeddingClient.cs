using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Knowledge;

/// <summary>
/// Calls an online embedding service that speaks the OpenAI format: POST {address}/embeddings with "Authorization: Bearer &lt;key&gt;".
/// Gemini, Mistral, OpenAI and Cloudflare Workers AI all do. The key is found by the key name saved in the settings.
/// </summary>
public class OnlineEmbeddingClient(HttpClient http, ApiKeyResolver keys) : IEmbeddingClient
{
    private const int BatchSize = 16;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, EmbeddingModelOptions options, CancellationToken cancellationToken = default)
    {
        var key = keys.Get(options.ApiKeyName, options.BaseUrl)
            ?? throw new EmbeddingException(
                $"The embedding model is an online service, but no key is set: the environment variable " +
                $"{keys.EffectiveName(options.ApiKeyName, options.BaseUrl)} is missing in the .env file, or the " +
                "'Embedding API key name' is not valid. Set it and restart the server.");

        var vectors = new List<float[]>(texts.Count);
        foreach (var batch in texts.Chunk(BatchSize))
            vectors.AddRange(await EmbedBatchAsync(batch, options, key, cancellationToken));

        return vectors;
    }

    private async Task<float[][]> EmbedBatchAsync(string[] batch, EmbeddingModelOptions options, string key, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.BaseUrl.TrimEnd('/') + "/embeddings")
        {
            Content = JsonContent.Create(new { model = options.Model, input = batch })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingException($"Cannot reach the embedding service at {options.BaseUrl}.", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = body.Length > 300 ? body[..300] : body;
                throw new EmbeddingException(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        $"The embedding service rejected the key ({(int)response.StatusCode}). Check the key named {options.ApiKeyName}. {detail}",
                    HttpStatusCode.NotFound =>
                        $"The embedding service did not find '{options.Model}' or the address {options.BaseUrl} (404). The address and the model must belong together, and a service retires old model names from time to time. {detail}",
                    HttpStatusCode.TooManyRequests =>
                        $"The embedding service limit was reached (429). Wait a moment and try again. {detail}",
                    _ => $"The embedding service returned {(int)response.StatusCode} for model '{options.Model}': {detail}"
                });
            }

            // {"data":[{"index":0,"embedding":[...]}]}, in the order of the texts.
            var parsed = JsonSerializer.Deserialize<EmbeddingsResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var vectors = parsed?.Data?.OrderBy(d => d.Index).Select(d => d.Embedding).ToArray();
            if (vectors is null || vectors.Length != batch.Length || vectors.Any(v => v is null || v.Length == 0))
                throw new EmbeddingException("The embedding service returned an unexpected number of embeddings.");

            return vectors!;
        }
    }

    private record EmbeddingsResponse(List<EmbeddingItem>? Data);

    private record EmbeddingItem(int Index, float[] Embedding);
}
