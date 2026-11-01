using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence;

public static class SettingsSeeder
{
    /// <summary>
    /// Adds every default setting that is not in the table yet. Existing rows are never changed. The overrides replace some
    /// default values, see <see cref="SettingDefaults.WithOverrides"/>.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, IReadOnlyDictionary<string, string>? overrides = null)
    {
        await RenameChatApiUrlAsync(db, overrides);

        var existingKeys = (await db.Settings.Select(s => s.Key).ToListAsync()).ToHashSet();

        var missing = SettingDefaults.WithOverrides(overrides)
            .Where(d => !existingKeys.Contains(d.Key))
            .Select(d => new Setting { Key = d.Key, Label = d.Label, Description = d.Description, Value = d.Value })
            .ToList();

        if (missing.Count == 0)
            return;

        db.Settings.AddRange(missing);
        await db.SaveChangesAsync();
    }

    // "chat.api_url" became "chat.base_url", which is now also used for Ollama. A database that has the old row gets the new
    // one with the same value (or the default when the old one was empty), and the old row is removed.
    private static async Task RenameChatApiUrlAsync(AppDbContext db, IReadOnlyDictionary<string, string>? overrides)
    {
        var old = await db.Settings.FirstOrDefaultAsync(s => s.Key == SettingKeys.LegacyChatApiUrl);
        if (old is null)
            return;

        if (!await db.Settings.AnyAsync(s => s.Key == SettingKeys.ChatBaseUrl))
        {
            var definition = SettingDefaults.WithOverrides(overrides).First(d => d.Key == SettingKeys.ChatBaseUrl);
            db.Settings.Add(new Setting
            {
                Key = definition.Key,
                Label = definition.Label,
                Description = definition.Description,
                Value = string.IsNullOrWhiteSpace(old.Value) ? definition.Value : old.Value
            });
        }

        db.Settings.Remove(old);
        await db.SaveChangesAsync();
    }
}
