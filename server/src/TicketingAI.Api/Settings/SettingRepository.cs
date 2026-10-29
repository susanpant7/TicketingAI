using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

public class SettingRepository(AppDbContext db) : ISettingRepository
{
    public Task<List<Setting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.Settings.AsNoTracking().OrderBy(s => s.Key).ToListAsync(cancellationToken);

    public Task<Setting?> GetAsync(string key) => db.Settings.FirstOrDefaultAsync(s => s.Key == key);

    public Task<bool> ExistsAsync(string key) => db.Settings.AnyAsync(s => s.Key == key);

    public async Task AddAsync(Setting setting)
    {
        db.Settings.Add(setting);
        await db.SaveChangesAsync();
    }

    public async Task<int> UpdateAsync(Setting setting, bool deleteKnowledge)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.SaveChangesAsync();
        var deleted = deleteKnowledge ? await db.KnowledgeChunks.ExecuteDeleteAsync() : 0;

        await transaction.CommitAsync();
        return deleted;
    }
}
