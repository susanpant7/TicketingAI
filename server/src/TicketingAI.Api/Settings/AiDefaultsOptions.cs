using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

/// <summary>
/// The starting values for the chat and embedding models, from configuration (environment variables AiDefaults__ChatBaseUrl,
/// AiDefaults__ChatModel, AiDefaults__EmbeddingBaseUrl and AiDefaults__EmbeddingModel). They are used when the settings are first
/// created in a new database and by "Restore defaults". An empty value means "use the built-in default". Changing them later does
/// not change a database that already has the settings: use the Settings page for that.
/// When an address is one of the known services (see <see cref="AiProviders"/>), the rest of that service is filled in as well: its
/// key name, its example model (unless a model is given) and, for an embedding model, its prefixes.
/// When no address is given, no chat or embedding model is selected: an admin chooses them on the Settings page, or they are given here.
/// </summary>
public class AiDefaultsOptions
{
    public const string SectionName = "AiDefaults";

    /// <summary>Where the embedding model runs (http://host.docker.internal:11434 for an Ollama on the computer that runs Docker).</summary>
    public string EmbeddingBaseUrl { get; init; } = "";

    /// <summary>The embedding model. Empty means the example model of the known service at the address, or the built-in default.</summary>
    public string EmbeddingModel { get; init; } = "";

    /// <summary>
    /// Where the chat model is called: where Ollama runs (http://host.docker.internal:11434 from a Docker container), or an online service's address
    /// up to /chat/completions.
    /// </summary>
    public string ChatBaseUrl { get; init; } = "";

    public string ChatModel { get; init; } = "";

    public IReadOnlyDictionary<string, string> ToOverrides()
    {
        var chatUrl = ChatBaseUrl;
        var embeddingUrl = EmbeddingBaseUrl;

        var overrides = new Dictionary<string, string>();
        void Add(string key, string value)
        {
            // A value that would not pass the settings rules is left out, so a typo here cannot break the chat.
            if (!string.IsNullOrWhiteSpace(value) && SettingRules.Validate(key, value.Trim()) is null)
                overrides[key] = value.Trim();
        }

        Add(SettingKeys.EmbeddingBaseUrl, embeddingUrl);
        Add(SettingKeys.EmbeddingModel, EmbeddingModel);
        Add(SettingKeys.ChatBaseUrl, chatUrl);
        Add(SettingKeys.ChatModel, ChatModel);

        // A known service at the address brings its own key name (and model, and prefixes) with it.
        var chat = AiProviders.All.FirstOrDefault(p => Same(p.BaseUrl, chatUrl));
        if (chat is not null)
        {
            overrides.TryAdd(SettingKeys.ChatModel, chat.ExampleModel);
            if (chat.ApiKeyName.Length > 0)
                overrides[SettingKeys.ChatApiKeyName] = chat.ApiKeyName;
        }

        var embedding = AiProviders.Embedding.FirstOrDefault(p => Same(p.BaseUrl, embeddingUrl));
        if (embedding is not null)
        {
            overrides.TryAdd(SettingKeys.EmbeddingModel, embedding.ExampleModel);
            if (embedding.ApiKeyName.Length > 0)
                overrides[SettingKeys.EmbeddingApiKeyName] = embedding.ApiKeyName;

            // The prefixes belong to the model: nomic-embed-text needs them, the online models need none, so an empty prefix is a value.
            overrides[SettingKeys.EmbeddingQueryPrefix] = embedding.QueryPrefix;
            overrides[SettingKeys.EmbeddingDocumentPrefix] = embedding.DocumentPrefix;
        }

        return overrides;
    }

    private static bool Same(string known, string given) =>
        given.Length > 0 && string.Equals(known.TrimEnd('/'), given.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
