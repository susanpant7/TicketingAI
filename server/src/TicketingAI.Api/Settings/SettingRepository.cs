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

    public Task UpdateAsync(Setting setting) => db.SaveChangesAsync();

    public async Task ReplaceAllAsync(
        IReadOnlyList<Setting> settings, IReadOnlyDictionary<int, byte[]>? vectors, bool deleteKnowledge = false)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Settings.ExecuteDeleteAsync();
        db.ChangeTracker.Clear();
        db.Settings.AddRange(settings);
        await db.SaveChangesAsync();
        await ReplaceVectorsAsync(vectors);
        if (deleteKnowledge)
            await db.KnowledgeChunks.ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }

    public async Task ChangeValuesAsync(IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<int, byte[]>? vectors)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        foreach (var (key, value) in values)
        {
            var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting is null)
            {
                var definition = SettingDefaults.All.First(d => d.Key == key);
                db.Settings.Add(new Setting { Key = key, Label = definition.Label, Description = definition.Description, Value = value });
            }
            else
            {
                setting.Value = value;
            }
        }
        await db.SaveChangesAsync();
        await ReplaceVectorsAsync(vectors);

        await transaction.CommitAsync();
    }

    private async Task ReplaceVectorsAsync(IReadOnlyDictionary<int, byte[]>? vectors)
    {
        if (vectors is null)
            return;

        foreach (var (id, bytes) in vectors)
            await db.KnowledgeChunks.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.Embedding, bytes));
    }
}
