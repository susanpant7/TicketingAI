using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

public interface ISavedChatModelRepository
{
    Task<List<SavedChatModel>> GetAllAsync();
    Task<SavedChatModel?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task AddAsync(SavedChatModel model);
    Task UpdateAsync(SavedChatModel model);
    Task DeleteAsync(SavedChatModel model);
}
