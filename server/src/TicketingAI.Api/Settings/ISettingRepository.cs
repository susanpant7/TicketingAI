using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

public interface ISettingRepository
{
    Task<List<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Setting?> GetAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task AddAsync(Setting setting);

    /// <summary>
    /// Saves the changes made to a setting loaded with <see cref="GetAsync"/>. When deleteKnowledge is true, all knowledge
    /// sections are deleted in the same transaction. Returns how many were deleted.
    /// </summary>
    Task<int> UpdateAsync(Setting setting, bool deleteKnowledge);
}
