using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

public interface ISettingRepository
{
    Task<List<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Setting?> GetAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task AddAsync(Setting setting);

    /// <summary>Saves the changes made to a setting loaded with <see cref="GetAsync"/>.</summary>
    Task UpdateAsync(Setting setting);

    /// <summary>
    /// Deletes every setting and inserts the given ones in a single transaction. When vectors are given (the knowledge embedded again for
    /// new embedding settings), the stored vectors are replaced in the same transaction. With deleteKnowledge, the stored
    /// knowledge is deleted in the same transaction (when there is no embedding model to embed it with).
    /// </summary>
    Task ReplaceAllAsync(IReadOnlyList<Setting> settings, IReadOnlyDictionary<int, byte[]>? vectors, bool deleteKnowledge = false);

    /// <summary>
    /// Sets the value of several settings in a single transaction (a missing one is added from its default). When vectors are given, the
    /// stored vectors are replaced in the same transaction.
    /// </summary>
    Task ChangeValuesAsync(IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<int, byte[]>? vectors);
}
