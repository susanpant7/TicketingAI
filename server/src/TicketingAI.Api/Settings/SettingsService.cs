using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public class SettingsService(AppDbContext db, ILogger<SettingsService> logger) : ISettingsService
{
    public async Task<AiSettings> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        var stored = await db.Settings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return new AiSettings(
            EmbeddingModel: Text(stored, SettingKeys.EmbeddingModel),
            EmbeddingQueryPrefix: Text(stored, SettingKeys.EmbeddingQueryPrefix, allowEmpty: true),
            EmbeddingDocumentPrefix: Text(stored, SettingKeys.EmbeddingDocumentPrefix, allowEmpty: true),
            TopK: Int(stored, SettingKeys.RetrievalTopK, min: 1),
            MinScore: Number(stored, SettingKeys.RetrievalMinScore, min: 0, max: 1),
            FollowUpMaxWords: Int(stored, SettingKeys.RetrievalFollowUpMaxWords, min: 0),
            ChatModel: Text(stored, SettingKeys.ChatModel),
            Temperature: Number(stored, SettingKeys.ChatTemperature, min: 0, max: 2),
            TopP: Number(stored, SettingKeys.ChatTopP, min: 0, max: 1),
            NumCtx: Int(stored, SettingKeys.ChatNumCtx, min: 256),
            MaxTokens: Int(stored, SettingKeys.ChatMaxTokens, min: 1),
            HistoryMessages: Int(stored, SettingKeys.ChatHistoryMessages, min: 0),
            SystemPrompt: Text(stored, SettingKeys.ChatSystemPrompt),
            FormatReminder: Text(stored, SettingKeys.ChatFormatReminder));
    }

    // A missing or invalid value falls back to the default, so a typo in the table cannot break the chat.
    private static string DefaultOf(string key) => SettingDefaults.All.First(d => d.Key == key).Value;

    private static string Text(Dictionary<string, string> stored, string key, bool allowEmpty = false) =>
        stored.TryGetValue(key, out var value) && (allowEmpty || !string.IsNullOrWhiteSpace(value)) ? value : DefaultOf(key);

    private int Int(Dictionary<string, string> stored, string key, int min)
    {
        if (stored.TryGetValue(key, out var value))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= min)
                return number;

            logger.LogWarning("Setting {Key} has the invalid value '{Value}'. Using the default.", key, value);
        }

        return int.Parse(DefaultOf(key), CultureInfo.InvariantCulture);
    }

    private double Number(Dictionary<string, string> stored, string key, double min, double max)
    {
        if (stored.TryGetValue(key, out var value))
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number >= min && number <= max)
                return number;

            logger.LogWarning("Setting {Key} has the invalid value '{Value}'. Using the default.", key, value);
        }

        return double.Parse(DefaultOf(key), CultureInfo.InvariantCulture);
    }
}
