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

    /// <summary>
    /// Changes a setting. Changing the embedding model also deletes all knowledge sections, because vectors made with
    /// different models cannot be compared. The knowledge data has to be uploaded again afterwards.
    /// </summary>
    Task<SettingResult> UpdateAsync(string key, UpdateSettingRequest request);
}
