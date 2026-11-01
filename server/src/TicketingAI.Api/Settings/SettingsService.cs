using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TicketingAI.Api.Chat;
using TicketingAI.Api.Knowledge;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public partial class SettingsService(
    ISettingRepository settings, ILogger<SettingsService> logger, IOptions<AiDefaultsOptions> aiDefaults, EmbeddingSwitcher embeddingSwitcher,
    IChatModelClient chatModel, IEmbeddingClient embeddingClient, ApiKeyResolver apiKeys) : ISettingsService
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
            EmbeddingBaseUrl: Text(stored, SettingKeys.EmbeddingBaseUrl),
            EmbeddingApiKeyName: Text(stored, SettingKeys.EmbeddingApiKeyName),
            EmbeddingModel: Text(stored, SettingKeys.EmbeddingModel),
            EmbeddingQueryPrefix: Text(stored, SettingKeys.EmbeddingQueryPrefix),
            EmbeddingDocumentPrefix: Text(stored, SettingKeys.EmbeddingDocumentPrefix),
            TopK: Int(stored, SettingKeys.RetrievalTopK),
            MinScore: Number(stored, SettingKeys.RetrievalMinScore),
            MaxScoreGap: Number(stored, SettingKeys.RetrievalMaxScoreGap),
            FollowUpMaxWords: Int(stored, SettingKeys.RetrievalFollowUpMaxWords),
            ChatBaseUrl: Text(stored, SettingKeys.ChatBaseUrl),
            ChatApiKeyName: Text(stored, SettingKeys.ChatApiKeyName),
            ChatModel: Text(stored, SettingKeys.ChatModel),
            ChatReasoningEffort: Text(stored, SettingKeys.ChatReasoningEffort),
            Temperature: Number(stored, SettingKeys.ChatTemperature),
            TopP: Number(stored, SettingKeys.ChatTopP),
            NumCtx: Int(stored, SettingKeys.ChatNumCtx),
            MaxTokens: Int(stored, SettingKeys.ChatMaxTokens),
            HistoryMessages: Int(stored, SettingKeys.ChatHistoryMessages),
            KeepAlive: Text(stored, SettingKeys.ChatKeepAlive),
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

        return new SettingResult(new SettingSaved(ToDto(setting)));
    }

    public async Task<SettingResult> UpdateAsync(string key, UpdateSettingRequest request)
    {
        var setting = await settings.GetAsync(key);
        if (setting is null)
            return new SettingResult(null, SettingError.NotFound);

        var problem = Check(key, request.Label, request.Description, request.Value);
        if (problem is not null)
            return Invalid(problem);

        // The embedding settings are changed only with "Use", which also converts the stored knowledge to the new model.
        if (EmbeddingKeys.Contains(key))
            return Invalid("Change the embedding model with Use in the list above the settings: it also converts the stored knowledge to the new model, and only saves the change when that worked.");

        setting.Label = request.Label.Trim();
        setting.Description = request.Description.Trim();
        setting.Value = request.Value.Trim();

        await settings.UpdateAsync(setting);
        return new SettingResult(new SettingSaved(ToDto(setting)));
    }

    public async Task<int> RestoreDefaultsAsync(CancellationToken cancellationToken)
    {
        var defaults = Defaults
            .Select(d => new Setting { Key = d.Key, Label = d.Label, Description = d.Description, Value = d.Value })
            .ToList();

        // If the embedding settings change, the stored knowledge is embedded again with the default ones first. When that fails
        // (the model or the key is not there), nothing is changed. The default has no embedding model, so then the knowledge is deleted.
        var stored = (await settings.GetAllAsync(cancellationToken)).ToDictionary(s => s.Key, s => s.Value);
        var changed = EmbeddingKeys.Any(key => stored.TryGetValue(key, out var current) && current != DefaultOf(key));

        Dictionary<int, byte[]>? vectors = null;
        var noEmbeddingModel = DefaultOf(SettingKeys.EmbeddingModel).Length == 0;
        if (changed && !noEmbeddingModel)
        {
            var options = new EmbeddingModelOptions(
                DefaultOf(SettingKeys.EmbeddingModel), DefaultOf(SettingKeys.EmbeddingBaseUrl),
                DefaultOf(SettingKeys.EmbeddingApiKeyName), DefaultOf(SettingKeys.ChatKeepAlive));
            vectors = await embeddingSwitcher.EmbedAllAsync(options, DefaultOf(SettingKeys.EmbeddingDocumentPrefix), cancellationToken);
        }

        await settings.ReplaceAllAsync(defaults, vectors, deleteKnowledge: changed && noEmbeddingModel);
        return vectors?.Count ?? 0;
    }

    public async Task<UseModelResult> UseChatModelAsync(UseChatModelRequest request, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>
        {
            [SettingKeys.ChatBaseUrl] = request.BaseUrl.Trim().TrimEnd('/'),
            [SettingKeys.ChatModel] = request.Model.Trim(),
            [SettingKeys.ChatApiKeyName] = request.ApiKeyName?.Trim() ?? ""
        };
        var problem = Problem(values);
        if (problem is not null)
            return new UseModelResult(0, SettingError.Invalid, problem);

        // A model list can show a model that this key cannot use (Gemini lists models that new accounts no longer get), so the model
        // answers one tiny question first. When it does not (a retired name, a wrong key), this throws before anything is saved.
        await TestChatModelAsync(request, cancellationToken);

        await settings.ChangeValuesAsync(values, null);
        return new UseModelResult(0);
    }

    public async Task<UseModelResult> UseEmbeddingModelAsync(UseEmbeddingModelRequest request, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>
        {
            [SettingKeys.EmbeddingBaseUrl] = request.BaseUrl.Trim().TrimEnd('/'),
            [SettingKeys.EmbeddingModel] = request.Model.Trim(),
            [SettingKeys.EmbeddingApiKeyName] = request.ApiKeyName?.Trim() ?? "",
            [SettingKeys.EmbeddingQueryPrefix] = request.QueryPrefix?.Trim() ?? "",
            [SettingKeys.EmbeddingDocumentPrefix] = request.DocumentPrefix?.Trim() ?? ""
        };
        var problem = Problem(values);
        if (problem is not null)
            return new UseModelResult(0, SettingError.Invalid, problem);

        // Embed every stored section again with the new model first. A failure here (a wrong key, a model that is not there) throws
        // before anything is saved, so the old settings and the old vectors stay as they were.
        var keepAlive = (await settings.GetAsync(SettingKeys.ChatKeepAlive))?.Value ?? DefaultOf(SettingKeys.ChatKeepAlive);
        var options = new EmbeddingModelOptions(
            values[SettingKeys.EmbeddingModel], values[SettingKeys.EmbeddingBaseUrl], values[SettingKeys.EmbeddingApiKeyName], keepAlive);
        var vectors = await embeddingSwitcher.EmbedAllAsync(options, values[SettingKeys.EmbeddingDocumentPrefix], cancellationToken);

        await settings.ChangeValuesAsync(values, vectors);
        return new UseModelResult(vectors.Count);
    }

    public async Task TestChatModelAsync(UseChatModelRequest request, CancellationToken cancellationToken)
    {
        var current = await GetAiSettingsAsync(cancellationToken);
        var options = new ChatModelOptions(
            request.Model.Trim(), 0, current.TopP, current.NumCtx, 1024, current.KeepAlive,
            request.BaseUrl.Trim().TrimEnd('/'), request.ApiKeyName?.Trim() ?? "", current.ChatReasoningEffort);
        await chatModel.ChatAsync([new ChatMessage("user", "Reply with one word: ok")], options, cancellationToken);
    }

    public async Task TestEmbeddingModelAsync(UseEmbeddingModelRequest request, CancellationToken cancellationToken)
    {
        var current = await GetAiSettingsAsync(cancellationToken);
        var options = new EmbeddingModelOptions(
            request.Model.Trim(), request.BaseUrl.Trim().TrimEnd('/'), request.ApiKeyName?.Trim() ?? "", current.KeepAlive);
        await embeddingClient.EmbedAsync(
            [EmbeddingText.Document(request.DocumentPrefix?.Trim() ?? "", "Test", "Test", "This is a test.")], options, cancellationToken);
    }

    private static readonly string[] EmbeddingKeys =
    [
        SettingKeys.EmbeddingBaseUrl, SettingKeys.EmbeddingApiKeyName, SettingKeys.EmbeddingModel,
        SettingKeys.EmbeddingQueryPrefix, SettingKeys.EmbeddingDocumentPrefix
    ];

    private static string? Problem(Dictionary<string, string> values) =>
        values.Select(v => SettingRules.Validate(v.Key, v.Value)).FirstOrDefault(message => message is not null);

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
    private IReadOnlyList<SettingDefinition> Defaults => SettingDefaults.WithOverrides(aiDefaults.Value.ToOverrides());

    private string DefaultOf(string key) => Defaults.First(d => d.Key == key).Value;

    private string Text(Dictionary<string, string> stored, string key) =>
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
