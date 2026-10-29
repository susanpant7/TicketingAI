using System.Globalization;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public enum SettingKind
{
    /// <summary>Text that must not be empty.</summary>
    Text,

    /// <summary>Text that may be empty.</summary>
    OptionalText,

    Int,
    Decimal
}

public record SettingRule(SettingKind Kind, double Min = 0, double Max = double.MaxValue);

/// <summary>What a value may be, for the settings the app uses. Any other key is plain, non-empty text.</summary>
public static class SettingRules
{
    private static readonly Dictionary<string, SettingRule> Known = new()
    {
        [SettingKeys.EmbeddingModel] = new(SettingKind.Text),
        [SettingKeys.EmbeddingQueryPrefix] = new(SettingKind.OptionalText),
        [SettingKeys.EmbeddingDocumentPrefix] = new(SettingKind.OptionalText),
        [SettingKeys.RetrievalTopK] = new(SettingKind.Int, Min: 1),
        [SettingKeys.RetrievalMinScore] = new(SettingKind.Decimal, Min: 0, Max: 1),
        [SettingKeys.RetrievalFollowUpMaxWords] = new(SettingKind.Int, Min: 0),
        [SettingKeys.ChatModel] = new(SettingKind.Text),
        [SettingKeys.ChatTemperature] = new(SettingKind.Decimal, Min: 0, Max: 2),
        [SettingKeys.ChatTopP] = new(SettingKind.Decimal, Min: 0, Max: 1),
        [SettingKeys.ChatNumCtx] = new(SettingKind.Int, Min: 256),
        [SettingKeys.ChatMaxTokens] = new(SettingKind.Int, Min: 1),
        [SettingKeys.ChatHistoryMessages] = new(SettingKind.Int, Min: 0),
        [SettingKeys.ChatSystemPrompt] = new(SettingKind.Text),
        [SettingKeys.ChatFormatReminder] = new(SettingKind.Text)
    };

    public static SettingRule For(string key) => Known.GetValueOrDefault(key) ?? new SettingRule(SettingKind.Text);

    /// <summary>Returns a message that says what is wrong with the value, or null when it is fine.</summary>
    public static string? Validate(string key, string value)
    {
        var rule = For(key);
        switch (rule.Kind)
        {
            case SettingKind.Text when string.IsNullOrWhiteSpace(value):
                return "The value cannot be empty.";

            case SettingKind.Int:
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) && whole >= rule.Min
                    ? null
                    : $"The value must be a whole number of at least {rule.Min}.";

            case SettingKind.Decimal:
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    && number >= rule.Min && number <= rule.Max
                    ? null
                    : $"The value must be a number from {rule.Min} to {rule.Max}.";

            default:
                return null;
        }
    }
}
