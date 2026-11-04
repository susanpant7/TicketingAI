using System.Text.RegularExpressions;

namespace TicketingAI.Api.Settings;

/// <summary>
/// Finds the key of an online service by the name of its environment variable (GEMINI_API_KEY, GROQ_API_KEY, ...), which is what a
/// setting or a saved model stores. The key itself is only ever read here, from the environment (the .env file), and never saved.
/// </summary>
public partial class ApiKeyResolver(IConfiguration configuration)
{
    /// <summary>The environment variable with the Cloudflare account id, and where it goes in the address of Cloudflare Workers AI.</summary>
    public const string AccountIdName = "CLOUDFLARE_ACCOUNT_ID";
    public const string AccountIdPlaceholder = "<ACCOUNT_ID>";

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

    /// <summary>
    /// The address to call: <ACCOUNT_ID> in it is replaced by the account id in CLOUDFLARE_ACCOUNT_ID, so the account id stays in the
    /// environment and not in a setting. An address without the placeholder is used as it is.
    /// </summary>
    public string ResolveAddress(string baseUrl)
    {
        var accountId = configuration[AccountIdName];
        return string.IsNullOrWhiteSpace(accountId) ? baseUrl : baseUrl.Replace(AccountIdPlaceholder, accountId.Trim());
    }

    /// <summary>True when the address still has a placeholder after <see cref="ResolveAddress"/>, so it cannot be called.</summary>
    public static bool IsIncomplete(string resolvedAddress) => resolvedAddress.Contains('<');

    public const string IncompleteAddressMessage =
        "The address still has <ACCOUNT_ID> in it. Set CLOUDFLARE_ACCOUNT_ID in the .env file (and restart the server), or put the account id in the address.";

    /// <summary>Says what is missing for a model whose key is not set, for an error message.</summary>
    public string MissingKey(string? keyName, string baseUrl)
    {
        var name = EffectiveName(keyName, baseUrl);
        return name.Length > 0
            ? $"the environment variable {name} is missing in the .env file, or its name is not valid"
            : "no key name is saved for it (choose the model again, or Edit it and set the key name)";
    }
}
