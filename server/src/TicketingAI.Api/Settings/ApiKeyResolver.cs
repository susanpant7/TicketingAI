using System.Text.RegularExpressions;

namespace TicketingAI.Api.Settings;

/// <summary>
/// Finds the key of an online service by the name of its environment variable (GEMINI_API_KEY, GROQ_API_KEY, ...), which is what a
/// setting or a saved model stores. The key itself is only ever read here, from the environment (the .env file), and never saved.
/// </summary>
public partial class ApiKeyResolver(IConfiguration configuration)
{
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex NamePattern();

    /// <summary>
    /// A key name is a variable name ending in API_KEY or ApiKey. Anything else (the admin password, the token key) is refused, so a
    /// saved model cannot make the server send another secret to a service.
    /// </summary>
    public static bool IsValidName(string name) =>
        name.Length <= 100 && NamePattern().IsMatch(name)
        && (name.EndsWith("API_KEY", StringComparison.OrdinalIgnoreCase) || name.EndsWith("ApiKey", StringComparison.OrdinalIgnoreCase));

    /// <summary>The key, or null when the name is empty or not valid, or the variable is not set.</summary>
    public string? Get(string? keyName)
    {
        var name = keyName?.Trim() ?? "";
        if (!IsValidName(name))
            return null;

        // A name with "__" is a nested configuration key ("A__B" is "A:B"); "GROQ_API_KEY" has no "__" and stays as it is.
        var value = configuration[name.Replace("__", ":")];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool IsSet(string? keyName) => Get(keyName) is not null;

    /// <summary>
    /// The name of the variable a model really reads. A saved name is used as it is. With no name saved, the key variable of the known
    /// service at the same address is used (a model for Google Gemini reads GEMINI_API_KEY). Empty when there is none.
    /// </summary>
    public string EffectiveName(string? keyName, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(keyName))
            return keyName.Trim();

        var service = AiProviders.All.Select(p => (p.BaseUrl, p.ApiKeyName)).Concat(AiProviders.Embedding.Select(p => (p.BaseUrl, p.ApiKeyName)))
            .FirstOrDefault(p => string.Equals(p.BaseUrl.TrimEnd('/'), baseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                                 && !string.IsNullOrEmpty(p.ApiKeyName));
        return service.ApiKeyName ?? "";
    }

    /// <summary>The key a model really reads (see <see cref="EffectiveName"/>), or null when it is not set.</summary>
    public string? Get(string? keyName, string baseUrl) => Get(EffectiveName(keyName, baseUrl));

    /// <summary>Says what is missing for a model whose key is not set, for an error message.</summary>
    public string MissingKey(string? keyName, string baseUrl)
    {
        var name = EffectiveName(keyName, baseUrl);
        return name.Length > 0
            ? $"the environment variable {name} is missing in the .env file, or its name is not valid"
            : "no key name is saved for it (choose the model again, or Edit it and set the key name)";
    }
}
