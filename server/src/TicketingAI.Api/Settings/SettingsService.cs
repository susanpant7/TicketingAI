using System.Globalization;
using System.Text.RegularExpressions;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public partial class SettingsService(ISettingRepository settings, ILogger<SettingsService> logger) : ISettingsService
{
    private const int MaxKeyLength = 100;
    private const int MaxLabelLength = 100;
    private const int MaxDescriptionLength = 1000;

    // Lowercase words joined by dots, with at least one dot, for example "chat.new_option".
    [GeneratedRegex(@"^[a-z0-9_]+(\.[a-z0-9_]+)+$")]
    private static partial Regex KeyPattern();

    public async Task<AiSettings> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        var stored = (await settings.GetAllAsync(cancellationToken)).ToDictionary(s => s.Key, s => s.Value);

        return new AiSettings(
            EmbeddingModel: Text(stored, SettingKeys.EmbeddingModel),
            EmbeddingQueryPrefix: Text(stored, SettingKeys.EmbeddingQueryPrefix),
            EmbeddingDocumentPrefix: Text(stored, SettingKeys.EmbeddingDocumentPrefix),
            TopK: Int(stored, SettingKeys.RetrievalTopK),
            MinScore: Number(stored, SettingKeys.RetrievalMinScore),
            FollowUpMaxWords: Int(stored, SettingKeys.RetrievalFollowUpMaxWords),
            ChatModel: Text(stored, SettingKeys.ChatModel),
            Temperature: Number(stored, SettingKeys.ChatTemperature),
            TopP: Number(stored, SettingKeys.ChatTopP),
            NumCtx: Int(stored, SettingKeys.ChatNumCtx),
            MaxTokens: Int(stored, SettingKeys.ChatMaxTokens),
            HistoryMessages: Int(stored, SettingKeys.ChatHistoryMessages),
            SystemPrompt: Text(stored, SettingKeys.ChatSystemPrompt),
            FormatReminder: Text(stored, SettingKeys.ChatFormatReminder));
    }

    public async Task<List<SettingDto>> GetAllAsync() =>
        (await settings.GetAllAsync()).Select(ToDto).ToList();

    public async Task<SettingResult> CreateAsync(CreateSettingRequest request)
    {
        var key = request.Key.Trim();
        if (key.Length > MaxKeyLength || !KeyPattern().IsMatch(key))
            return Invalid("The key must be lowercase words joined by dots, for example chat.new_option.");
        if (await settings.ExistsAsync(key))
            return new SettingResult(null, SettingError.KeyTaken, "A setting with this key already exists.");

        var problem = Check(key, request.Label, request.Description, request.Value);
        if (problem is not null)
            return Invalid(problem);

        var setting = new Setting
        {
            Key = key,
            Label = request.Label.Trim(),
            Description = request.Description.Trim(),
            Value = request.Value.Trim()
        };
        await settings.AddAsync(setting);

        return new SettingResult(new SettingSaved(ToDto(setting), 0));
    }

    public async Task<SettingResult> UpdateAsync(string key, UpdateSettingRequest request)
    {
        var setting = await settings.GetAsync(key);
        if (setting is null)
            return new SettingResult(null, SettingError.NotFound);

        var problem = Check(key, request.Label, request.Description, request.Value);
        if (problem is not null)
            return Invalid(problem);

        var newValue = request.Value.Trim();
        var embeddingModelChanged = key == SettingKeys.EmbeddingModel && newValue != setting.Value;

        setting.Label = request.Label.Trim();
        setting.Description = request.Description.Trim();
        setting.Value = newValue;

        var knowledgeDeleted = await settings.UpdateAsync(setting, deleteKnowledge: embeddingModelChanged);
        return new SettingResult(new SettingSaved(ToDto(setting), knowledgeDeleted));
    }

    private static string? Check(string key, string label, string description, string value)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Trim().Length > MaxLabelLength)
            return $"The label is required and can be up to {MaxLabelLength} characters.";
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > MaxDescriptionLength)
            return $"The description is required and can be up to {MaxDescriptionLength} characters.";

        return SettingRules.Validate(key, value.Trim());
    }

    private static SettingResult Invalid(string message) => new(null, SettingError.Invalid, message);

    private static SettingDto ToDto(Setting s) => new(s.Key, s.Label, s.Description, s.Value);

    // A missing or invalid value falls back to the default, so a bad value in the table cannot break the chat.
    private static string DefaultOf(string key) => SettingDefaults.All.First(d => d.Key == key).Value;

    private static string Text(Dictionary<string, string> stored, string key) =>
        stored.TryGetValue(key, out var value) && SettingRules.Validate(key, value) is null ? value : DefaultOf(key);

    private int Int(Dictionary<string, string> stored, string key)
    {
        if (stored.TryGetValue(key, out var value))
        {
            if (SettingRules.Validate(key, value) is null)
                return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

            logger.LogWarning("Setting {Key} has the invalid value '{Value}'. Using the default.", key, value);
        }

        return int.Parse(DefaultOf(key), CultureInfo.InvariantCulture);
    }

    private double Number(Dictionary<string, string> stored, string key)
    {
        if (stored.TryGetValue(key, out var value))
        {
            if (SettingRules.Validate(key, value) is null)
                return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

            logger.LogWarning("Setting {Key} has the invalid value '{Value}'. Using the default.", key, value);
        }

        return double.Parse(DefaultOf(key), CultureInfo.InvariantCulture);
    }
}
