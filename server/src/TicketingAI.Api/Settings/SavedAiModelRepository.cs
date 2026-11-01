using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public class SavedAiModelRepository(AppDbContext db) : ISavedAiModelRepository
{
    public Task<List<SavedAiModel>> GetAllAsync(string? kind) =>
        db.SavedAiModels.Where(m => kind == null || m.Kind == kind).OrderBy(m => m.Name).ToListAsync();

    public Task<SavedAiModel?> GetByIdAsync(string kind, int id) =>
        db.SavedAiModels.FirstOrDefaultAsync(m => m.Kind == kind && m.Id == id);

    public Task<bool> NameExistsAsync(string kind, string name, int? excludeId = null) =>
        db.SavedAiModels.AnyAsync(m => m.Kind == kind && m.Name.ToLower() == name.ToLower() && m.Id != excludeId);

    public async Task AddAsync(SavedAiModel model)
    {
        db.SavedAiModels.Add(model);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync(SavedAiModel model) => db.SaveChangesAsync();

    public async Task DeleteAsync(SavedAiModel model)
    {
        db.SavedAiModels.Remove(model);
        await db.SaveChangesAsync();
    }
}
