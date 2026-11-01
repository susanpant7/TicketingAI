using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

public interface ISavedAiModelRepository
{
    /// <summary>The saved models of one kind ("chat" or "embedding"), or of every kind when kind is null.</summary>
    Task<List<SavedAiModel>> GetAllAsync(string? kind);

    Task<SavedAiModel?> GetByIdAsync(string kind, int id);
    Task<bool> NameExistsAsync(string kind, string name, int? excludeId = null);
    Task AddAsync(SavedAiModel model);
    Task UpdateAsync(SavedAiModel model);
    Task DeleteAsync(SavedAiModel model);
}
