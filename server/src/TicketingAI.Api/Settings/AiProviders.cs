namespace TicketingAI.Api.Settings;

/// <summary>A known service that can run the chat model, shown on the Settings page with a Use button.</summary>
public record AiProvider(string Name, string BaseUrl, string ExampleModel, bool NeedsApiKey, string FreeTier, string SignUpUrl);

/// <summary>
/// What the Settings page shows about the services: the name of the key variable, whether it is set, and the known services.
/// The key itself is never sent.
/// </summary>
public record AiProvidersInfo(string ApiKeyName, bool HasApiKey, IReadOnlyList<AiProvider> Providers);

public static class AiProviders
{
    /// <summary>The environment variable (the .env file) with the key of an online chat service. Never stored in the database.</summary>
    public const string ApiKeyName = "ChatApi__ApiKey";

    // Free tiers and model names change, so these are starting points: check the service's own page when a model is not found.
    public static IReadOnlyList<AiProvider> All { get; } =
    [
        new("Ollama (on our own machine)", "http://localhost:11434", "qwen2.5:7b", false,
            "Free, runs on our own machine. Pull the model first: ollama pull <model>. With Docker Compose the address is http://ollama:11434.",
            "https://ollama.com/download"),
        new("Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-flash-latest", true,
            "Free tier for the Flash and Flash-Lite models, with a daily limit. The -latest name follows the current Flash model, so it is not retired.",
            "https://aistudio.google.com/apikey"),
        new("Groq", "https://api.groq.com/openai/v1", "llama-3.3-70b-versatile", true,
            "Free tier with per-minute and daily limits. Very fast.",
            "https://console.groq.com/keys"),
        new("OpenRouter", "https://openrouter.ai/api/v1", "meta-llama/llama-3.3-70b-instruct:free", true,
            "Many models with one key; models ending in :free cost nothing but have a small daily limit.",
            "https://openrouter.ai/keys"),
        new("Mistral", "https://api.mistral.ai/v1", "mistral-small-latest", true,
            "Free \"Experiment\" plan with rate limits.",
            "https://console.mistral.ai/api-keys"),
        new("Cerebras", "https://api.cerebras.ai/v1", "gpt-oss-120b", true,
            "Free tier with a daily token limit. Very fast.",
            "https://cloud.cerebras.ai"),
        new("Cloudflare Workers AI", "https://api.cloudflare.com/client/v4/accounts/<ACCOUNT_ID>/ai/v1", "@cf/meta/llama-3.1-8b-instruct", true,
            "A free daily allowance. Replace <ACCOUNT_ID> in the address with the account id, and use an API token with Workers AI access.",
            "https://dash.cloudflare.com/profile/api-tokens")
    ];

    /// <summary>
    /// An address with no path (http://localhost:11434) is Ollama's own API, which can keep the model loaded and set the context
    /// window. An address with a path (.../v1, .../openai) is called in the OpenAI chat format.
    /// </summary>
    public static bool IsOllamaApi(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var url) && url.AbsolutePath.Trim('/').Length == 0;
}
