namespace TicketingAI.Api.Settings;

/// <summary>A known service that can run the chat model, shown on the Settings page with a Use button.</summary>
public record AiProvider(
    string Name, string BaseUrl, string ExampleModel, bool NeedsApiKey, string FreeTier, string SignUpUrl, string ApiKeyName);

/// <summary>A service that can turn text into vectors, with the text its model expects before a question and before a knowledge section.</summary>
public record EmbeddingProvider(
    string Name, string BaseUrl, string ExampleModel, string QueryPrefix, string DocumentPrefix, bool NeedsApiKey, string FreeTier,
    string SignUpUrl, string ApiKeyName);

/// <summary>A service for the Settings page. HasApiKey says whether its key variable is set; the key itself is never sent.</summary>
public record AiProviderDto(
    string Name, string BaseUrl, string ExampleModel, bool NeedsApiKey, string FreeTier, string SignUpUrl, string ApiKeyName, bool HasApiKey);

public record EmbeddingProviderDto(
    string Name, string BaseUrl, string ExampleModel, string QueryPrefix, string DocumentPrefix, bool NeedsApiKey, string FreeTier,
    string SignUpUrl, string ApiKeyName, bool HasApiKey);

/// <summary>What the Settings page shows about the known services.</summary>
/// <summary>
/// ChatKeyNameInUse and EmbeddingKeyNameInUse are the names of the variables the chat model and the embedding model really read right
/// now, and ChatKeyInUseSet and EmbeddingKeyInUseSet say whether they are set. The keys themselves are never sent.
/// </summary>
public record AiProvidersInfo(
    IReadOnlyList<AiProviderDto> Providers, IReadOnlyList<EmbeddingProviderDto> EmbeddingProviders, bool ChatKeyInUseSet,
    bool EmbeddingKeyInUseSet, string ChatKeyNameInUse, string EmbeddingKeyNameInUse);

public static class AiProviders
{
    // Checked against each service's own model list on 2026-11-01. Free tiers and model names change, so these are starting
    // points: check the service's page when a model is not found.
    public static IReadOnlyList<AiProvider> All { get; } =
    [
        new("Ollama (on our own machine)", "http://localhost:11434", "qwen2.5:7b", false,
            "Free, runs on our own machine. Pull the model first: ollama pull <model>. With Docker Compose the address is http://ollama:11434.",
            "https://ollama.com/download", ""),
        new("Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-3.8-flash", true,
            "Free tier with a daily limit. gemini-3.8-flash is the newest Flash model; gemini-3.5-flash-lite is a lighter, faster one. The older gemini-2.5-flash is no longer available to new accounts.",
            "https://aistudio.google.com/apikey", "GEMINI_API_KEY"),
        new("Groq", "https://api.groq.com/openai/v1", "openai/gpt-oss-20b", true,
            "Free tier with per-minute and daily limits. Very fast. Which models a key can use differs: use View models to list them, and Test one before you select it. Newer models here think before they answer, so Reasoning effort (low) helps.",
            "https://console.groq.com/keys", "GROQ_API_KEY"),
        new("OpenRouter", "https://openrouter.ai/api/v1", "recursive/flint-v1.0:free", true,
            "Many models with one key; models ending in :free cost nothing but have a small daily limit. The free models change often: pick one at openrouter.ai/models?max_price=0 and save it with Add chat model.",
            "https://openrouter.ai/keys", "OPENROUTER_API_KEY"),
        new("Mistral", "https://api.mistral.ai/v1", "mistral-small-latest", true,
            "Free \"Experiment\" plan with rate limits. The -latest name follows the current Mistral Small model.",
            "https://console.mistral.ai/api-keys", "MISTRAL_API_KEY"),
        new("Cerebras", "https://api.cerebras.ai/v1", "gpt-oss-120b", true,
            "Free tier with a daily token limit. Very fast. Also listed: qwen-3.8-27b.",
            "https://cloud.cerebras.ai", "CEREBRAS_API_KEY"),
        new("Cloudflare Workers AI", "https://api.cloudflare.com/client/v4/accounts/<ACCOUNT_ID>/ai/v1", "@cf/meta/llama-3.1-8b-instruct-fp8", true,
            "A free daily allowance. Replace <ACCOUNT_ID> in the address with the account id, and use an API token with Workers AI access. Also listed: @cf/meta/llama-3.3-70b-instruct-fp8-fast.",
            "https://dash.cloudflare.com/profile/api-tokens", "CLOUDFLARE_API_KEY")
    ];

    // The services with an embeddings endpoint. Most chat-only services (Groq, Cerebras) have none. The prefixes are what the model was
    // trained with: nomic-embed-text needs them, the others none.
    public static IReadOnlyList<EmbeddingProvider> Embedding { get; } =
    [
        new("Ollama (on our own machine)", "http://localhost:11434", "nomic-embed-text", "search_query:", "search_document:", false,
            "Free, runs on our own machine. Pull the model first: ollama pull nomic-embed-text. With Docker Compose the address is http://ollama:11434.",
            "https://ollama.com/download", ""),
        new("Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-embedding-001", "", "", true,
            "Free tier with rate limits. The same key as Gemini chat works.",
            "https://aistudio.google.com/apikey", "GEMINI_API_KEY"),
        new("Mistral", "https://api.mistral.ai/v1", "mistral-embed", "", "", true,
            "Free \"Experiment\" plan with rate limits. The same key as Mistral chat works.",
            "https://console.mistral.ai/api-keys", "MISTRAL_API_KEY"),
        new("OpenAI", "https://api.openai.com/v1", "text-embedding-3-small", "", "", true,
            "Paid, but very cheap: converting all the knowledge costs a fraction of a cent.",
            "https://platform.openai.com/api-keys", "OPENAI_API_KEY"),
        new("Cloudflare Workers AI", "https://api.cloudflare.com/client/v4/accounts/<ACCOUNT_ID>/ai/v1", "@cf/baai/bge-m3", "", "", true,
            "A free daily allowance. Replace <ACCOUNT_ID> in the address with the account id.",
            "https://dash.cloudflare.com/profile/api-tokens", "CLOUDFLARE_API_KEY")
    ];

    /// <summary>
    /// An address with no path (http://localhost:11434) is Ollama's own API, which can keep the model loaded and set the context
    /// window. An address with a path (.../v1, .../openai) is called in the OpenAI chat format.
    /// </summary>
    public static bool IsOllamaApi(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var url) && url.AbsolutePath.Trim('/').Length == 0;
}
