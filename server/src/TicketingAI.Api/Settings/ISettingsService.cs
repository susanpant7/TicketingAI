using TicketingAI.Api.Chat;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Settings;

public interface ISettingsService
{
    /// <summary>
    /// Reads the current settings from the Settings table. It is called before every embedding and chat request,
    /// so a change made in the table is used by the next message.
    /// </summary>
    Task<AiSettings> GetAiSettingsAsync(CancellationToken cancellationToken = default);

    Task<List<SettingDto>> GetAllAsync();
    Task<SettingResult> CreateAsync(CreateSettingRequest request);

    /// <summary>Changes a setting. The embedding settings are refused: they change with <see cref="UseEmbeddingModelAsync"/>.</summary>
    Task<SettingResult> UpdateAsync(string key, UpdateSettingRequest request);

    /// <summary>
    /// Deletes every setting (including added ones) and inserts the defaults again. If that changes the embedding settings, the stored
    /// knowledge is embedded again with the default model first. Returns how many sections were embedded again.
    /// </summary>
    Task<int> RestoreDefaultsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets the Chat API base URL, the Chat model and the Chat API key name together, but only after the model answered a tiny test
    /// question. A failure throws a <see cref="ChatModelException"/> and changes nothing.
    /// </summary>
    Task<UseModelResult> UseChatModelAsync(UseChatModelRequest request, CancellationToken cancellationToken);

    /// <summary>Sends one tiny question to a chat model. Throws a <see cref="ChatModelException"/> when it does not answer. Saves nothing.</summary>
    Task TestChatModelAsync(UseChatModelRequest request, CancellationToken cancellationToken);

    /// <summary>Embeds one short text with an embedding model. Throws an <see cref="EmbeddingException"/> when it fails. Saves nothing.</summary>
    Task TestEmbeddingModelAsync(UseEmbeddingModelRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Converts the stored knowledge to a new embedding model and, only when that worked, saves the embedding settings. Returns how many
    /// sections were embedded again.
    /// </summary>
    Task<UseModelResult> UseEmbeddingModelAsync(UseEmbeddingModelRequest request, CancellationToken cancellationToken);
}
