using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public class SavedChatModelRepository(AppDbContext db) : ISavedChatModelRepository
{
    public Task<List<SavedChatModel>> GetAllAsync() => db.SavedChatModels.OrderBy(m => m.Name).ToListAsync();

    public Task<SavedChatModel?> GetByIdAsync(int id) => db.SavedChatModels.FirstOrDefaultAsync(m => m.Id == id);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null) =>
        db.SavedChatModels.AnyAsync(m => m.Name.ToLower() == name.ToLower() && m.Id != excludeId);

    public async Task AddAsync(SavedChatModel model)
    {
        db.SavedChatModels.Add(model);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync(SavedChatModel model) => db.SaveChangesAsync();

    public async Task DeleteAsync(SavedChatModel model)
    {
        db.SavedChatModels.Remove(model);
        await db.SaveChangesAsync();
    }
}
