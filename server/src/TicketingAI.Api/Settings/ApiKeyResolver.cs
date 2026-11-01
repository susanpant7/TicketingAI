using System.Text.RegularExpressions;

namespace TicketingAI.Api.Settings;

/// <summary>
/// Finds the key of an online service by the name of its environment variable (GEMINI_API_KEY, GROQ_API_KEY, ...), which is what a
/// setting or a saved model stores. The key itself is only ever read here, from the environment (the .env file), and never saved.
/// </summary>
public partial class ApiKeyResolver(IConfiguration configuration)
{
    /// <summary>The variable of the first version, used when no name is given.</summary>
    public const string LegacyKeyName = "ChatApi__ApiKey";

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex NamePattern();

    /// <summary>
    /// A key name is a variable name ending in API_KEY or ApiKey. Anything else (the admin password, the token key) is refused, so a
    /// saved model cannot make the server send another secret to a service.
    /// </summary>
    public static bool IsValidName(string name) =>
        name.Length <= 100 && NamePattern().IsMatch(name)
        && (name.EndsWith("API_KEY", StringComparison.OrdinalIgnoreCase) || name.EndsWith("ApiKey", StringComparison.OrdinalIgnoreCase));

    /// <summary>The key, or null when the name is not valid or the variable is not set. An empty name means the legacy variable.</summary>
    public string? Get(string? keyName)
    {
        var name = string.IsNullOrWhiteSpace(keyName) ? LegacyKeyName : keyName.Trim();
        if (!IsValidName(name))
            return null;

        // "ChatApi__ApiKey" is the configuration key "ChatApi:ApiKey"; "GROQ_API_KEY" has no "__" and stays as it is.
        var value = configuration[name.Replace("__", ":")];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool IsSet(string? keyName) => Get(keyName) is not null;

    /// <summary>
    /// The name of the variable a model really reads. A saved name is used as it is. With no name saved, the key variable of the known
    /// service at the same address is used when it is set (a model for Google Gemini reads GEMINI_API_KEY), and otherwise the legacy
    /// variable of the first version.
    /// </summary>
    public string EffectiveName(string? keyName, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(keyName))
            return keyName.Trim();

        var service = AiProviders.All.Select(p => (p.BaseUrl, p.ApiKeyName)).Concat(AiProviders.Embedding.Select(p => (p.BaseUrl, p.ApiKeyName)))
            .FirstOrDefault(p => string.Equals(p.BaseUrl.TrimEnd('/'), baseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                                 && !string.IsNullOrEmpty(p.ApiKeyName) && IsSet(p.ApiKeyName));
        return service.ApiKeyName ?? LegacyKeyName;
    }

    /// <summary>The key a model really reads (see <see cref="EffectiveName"/>), or null when it is not set.</summary>
    public string? Get(string? keyName, string baseUrl) => Get(EffectiveName(keyName, baseUrl));
}
