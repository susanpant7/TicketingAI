using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TicketingAI.Api.Chat;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

/// <summary>The models the service at an address says it can run for our key.</summary>
public record AvailableAiModels(string BaseUrl, IReadOnlyList<string> Models);

/// <summary>
/// Asks a service which models it offers: Ollama's own /api/tags, or the /models list of an online service in the OpenAI format. It only
/// calls an address we already know (the saved ones, the known services, the saved models), and it sends a key, found by name, only to an
/// online service. This answers "does this model name exist for my key" without guessing.
/// </summary>
public class AiModelLister(
    HttpClient http, ISettingsService settingsService, ISavedAiModelRepository savedModels, ApiKeyResolver keys)
{
    public async Task<AvailableAiModels> ListAsync(string kind, string baseUrl, string? keyName, CancellationToken cancellationToken)
    {
        baseUrl = baseUrl.Trim().TrimEnd('/');
        await EnsureKnownAsync(baseUrl, cancellationToken);

        // <ACCOUNT_ID> in a known address is filled in from the environment, for the call only.
        var address = keys.ResolveAddress(baseUrl);
        if (ApiKeyResolver.IsIncomplete(address))
            throw new ChatModelException(ApiKeyResolver.IncompleteAddressMessage);

        var ollama = AiProviders.IsOllamaApi(address);
        var cloudflare = IsCloudflare(address);
        // Cloudflare's OpenAI-style address has no /models list. Its own model search lists the models of one kind of task.
        var url = cloudflare ? CloudflareSearchUrl(address, kind) : ollama ? $"{address}/api/tags" : $"{address}/models";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!ollama)
        {
            var key = keys.Get(keyName, baseUrl)
                ?? throw new ChatModelException(
                    $"No key is set: {keys.MissingKey(keyName, baseUrl)}. Set it and restart the server.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ChatModelException($"Cannot reach {address}.", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = body.Length > 300 ? body[..300] : body;
                throw new ChatModelException(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? $"The service rejected the key ({(int)response.StatusCode}). Check the key named {keyName}: it must be a key of this service. {detail}"
                    : $"The service answered {(int)response.StatusCode} for {baseUrl}. {detail}");
            }

            return new AvailableAiModels(baseUrl, cloudflare ? ReadCloudflareNames(body) : ReadNames(body, ollama, kind));
        }
    }

    /// <summary>Throws when the address is not one we already know (the saved ones, the known services, the saved models).</summary>
    public async Task EnsureKnownAsync(string baseUrl, CancellationToken cancellationToken)
    {
        if (!await IsKnownAsync(baseUrl.Trim().TrimEnd('/'), cancellationToken))
            throw new ChatModelException("That address is not one of the saved or known services.");
    }

    private async Task<bool> IsKnownAsync(string baseUrl, CancellationToken cancellationToken)
    {
        bool Same(string other) => string.Equals(other.TrimEnd('/'), baseUrl, StringComparison.OrdinalIgnoreCase);

        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);
        return Same(settings.ChatBaseUrl) || Same(settings.EmbeddingBaseUrl)
            || AiProviders.All.Any(p => Same(p.BaseUrl))
            || AiProviders.Embedding.Any(p => Same(p.BaseUrl))
            || (await savedModels.GetAllAsync(null)).Any(m => Same(m.BaseUrl));
    }

    private const string CloudflarePrefix = "https://api.cloudflare.com/client/v4/accounts/";

    private static bool IsCloudflare(string address) =>
        address.StartsWith(CloudflarePrefix, StringComparison.OrdinalIgnoreCase) && address.TrimEnd('/').EndsWith("/ai/v1", StringComparison.OrdinalIgnoreCase);

    // https://api.cloudflare.com/client/v4/accounts/{id}/ai/v1 -> .../accounts/{id}/ai/models/search?task=...
    private static string CloudflareSearchUrl(string address, string kind)
    {
        var account = address.TrimEnd('/')[..^"/ai/v1".Length];
        var task = kind == SavedAiModel.Embedding ? "Text Embeddings" : "Text Generation";
        return $"{account}/ai/models/search?task={Uri.EscapeDataString(task)}&per_page=100";
    }

    // {"result":[{"name":"@cf/baai/bge-base-en-v1.5", ...}], ...}
    private static List<string> ReadCloudflareNames(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("result", out var items) || items.ValueKind != JsonValueKind.Array)
            return [];

        return items.EnumerateArray()
            .Select(item => item.TryGetProperty("name", out var name) ? name.GetString() : null)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct()
            .Order()
            .ToList();
    }

    // Ollama: {"models":[{"name":"qwen2.5:7b"}]}. OpenAI format: {"data":[{"id":"gpt-oss-20b"}]}. Gemini writes "models/" before each name.
    private static List<string> ReadNames(string json, bool ollama, string kind)
    {
        using var document = JsonDocument.Parse(json);
        var list = ollama ? "models" : "data";
        var field = ollama ? "name" : "id";
        if (!document.RootElement.TryGetProperty(list, out var items) || items.ValueKind != JsonValueKind.Array)
            return [];

        var embedding = kind == SavedAiModel.Embedding;
        var names = items.EnumerateArray()
            .Where(item => embedding || IsChatModel(item))
            .Select(item => item.TryGetProperty(field, out var name) ? name.GetString() : null)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.StartsWith("models/") ? name["models/".Length..] : name)
            .Distinct()
            .Order()
            .ToList();

        // A service lists every model it has. For the chat, keep the ones that are not for chat out; for the embeddings, keep the ones
        // that look like embedding models, or everything when none look like one.
        if (!embedding)
            return names.Where(name => !NotForChat.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase))).ToList();

        var embeddingNames = names.Where(name => ForEmbedding.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase))).ToList();
        return embeddingNames.Count > 0 ? embeddingNames : names;
    }

    private static readonly string[] NotForChat = ["guard", "embed", "whisper"];
    private static readonly string[] ForEmbedding = ["embed", "bge", "minilm", "e5-", "gte-"];

    // When the service says what a model returns (output_modalities), only models that return text are chat models.
    private static bool IsChatModel(JsonElement item) =>
        !item.TryGetProperty("output_modalities", out var outputs)
        || outputs.ValueKind != JsonValueKind.Array
        || outputs.EnumerateArray().Any(o => o.GetString() == "text");
}
