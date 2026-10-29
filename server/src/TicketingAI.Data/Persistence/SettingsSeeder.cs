using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence;

public static class SettingsSeeder
{
    /// <summary>Adds every default setting that is not in the table yet. Existing rows are never changed.</summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        var existingKeys = (await db.Settings.Select(s => s.Key).ToListAsync()).ToHashSet();

        var missing = SettingDefaults.All
            .Where(d => !existingKeys.Contains(d.Key))
            .Select(d => new Setting { Key = d.Key, Label = d.Label, Description = d.Description, Value = d.Value })
            .ToList();

        if (missing.Count == 0)
            return;

        db.Settings.AddRange(missing);
        await db.SaveChangesAsync();
    }
}
