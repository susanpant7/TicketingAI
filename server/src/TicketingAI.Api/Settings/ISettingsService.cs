namespace TicketingAI.Api.Settings;

public interface ISettingsService
{
    /// <summary>
    /// Reads the current settings from the Settings table. It is called before every embedding and chat request,
    /// so a change made in the table is used by the next message.
    /// </summary>
    Task<AiSettings> GetAiSettingsAsync(CancellationToken cancellationToken = default);
}
