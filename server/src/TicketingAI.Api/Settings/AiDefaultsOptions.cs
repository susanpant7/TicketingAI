using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

/// <summary>
/// The starting values for the Ollama address and the chat model settings, from configuration (environment variables
/// AiDefaults__OllamaUrl, AiDefaults__ChatProvider, AiDefaults__ChatBaseUrl and AiDefaults__ChatModel). They are used when the settings are first
/// created in a new database and by "Restore defaults". An empty value means "use the built-in default". Changing them later
/// does not change a database that already has the settings: use the Settings page for that.
/// </summary>
public class AiDefaultsOptions
{
    public const string SectionName = "AiDefaults";

    /// <summary>"ollama" (a model on our own machine) or "api" (an external service such as Gemini or Cloudflare).</summary>
    /// <summary>Where Ollama runs. In Docker Compose it is http://ollama:11434.</summary>
    public string OllamaUrl { get; init; } = "";

    public string ChatProvider { get; init; } = "";

    /// <summary>
    /// Where the chat model is called: where Ollama runs (http://ollama:11434 in Docker Compose), or for the provider "api" the
    /// service's address up to /chat/completions.
    /// </summary>
    public string ChatBaseUrl { get; init; } = "";

    public string ChatModel { get; init; } = "";

    public IReadOnlyDictionary<string, string> ToOverrides()
    {
        var overrides = new Dictionary<string, string>();
        void Add(string key, string value)
        {
            // A value that would not pass the settings rules is left out, so a typo here cannot break the chat.
            if (!string.IsNullOrWhiteSpace(value) && SettingRules.Validate(key, value.Trim()) is null)
                overrides[key] = value.Trim();
        }

        Add(SettingKeys.OllamaBaseUrl, OllamaUrl);
        Add(SettingKeys.ChatProvider, ChatProvider);
        Add(SettingKeys.ChatBaseUrl, ChatBaseUrl);
        Add(SettingKeys.ChatModel, ChatModel);
        return overrides;
    }
}
