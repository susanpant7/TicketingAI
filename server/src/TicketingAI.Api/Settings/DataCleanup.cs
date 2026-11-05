using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Settings;

/// <summary>
/// Deletes all data: tickets, users, knowledge, departments and settings. The admin signs in from configuration, so stays
/// signed in, and loads the data again on Settings → Manage App Data. Missing settings fall back to their defaults meanwhile.
/// </summary>
public class DataCleanup(AppDbContext db)
{
    public async Task CleanUpAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Rows that point at others go first, so no foreign key is left dangling.
        await db.ChatMessages.ExecuteDeleteAsync();
        await db.Tickets.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
        await db.KnowledgeChunks.ExecuteDeleteAsync();
        await db.Departments.ExecuteDeleteAsync();
        await db.Settings.ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }
}
