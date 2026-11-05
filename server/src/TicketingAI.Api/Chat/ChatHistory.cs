using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Chat;

/// <summary>A message of a saved chat, as the page shows it.</summary>
public record ChatHistoryItem(int Id, string Role, string Content, string? Department, DateTime CreatedAt);

public interface IChatHistoryRepository
{
    /// <summary>Saves what an employee wrote and the reply to it. userId is null for the admin.</summary>
    Task AddAsync(int? userId, string message, ChatResponse reply);

    /// <summary>The last messages of one person's chat, oldest first.</summary>
    Task<List<ChatHistoryItem>> GetRecentAsync(int? userId, int count);

    /// <summary>Deletes one person's chat. Returns how many messages were deleted.</summary>
    Task<int> DeleteForUserAsync(int? userId);

    /// <summary>Deletes every chat. Returns how many messages were deleted.</summary>
    Task<int> DeleteAllAsync();
}

public class ChatHistoryRepository(AppDbContext db) : IChatHistoryRepository
{
    public async Task AddAsync(int? userId, string message, ChatResponse reply)
    {
        var now = DateTime.UtcNow;
        db.ChatMessages.Add(new StoredChatMessage { UserId = userId, Role = "user", Content = message, CreatedAt = now });
        db.ChatMessages.Add(new StoredChatMessage
        {
            UserId = userId, Role = "assistant", Content = reply.Answer, Department = reply.Department, CreatedAt = now
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ChatHistoryItem>> GetRecentAsync(int? userId, int count)
    {
        var recent = await db.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Id)
            .Take(count)
            .Select(m => new ChatHistoryItem(m.Id, m.Role, m.Content, m.Department, m.CreatedAt))
            .ToListAsync();
        recent.Reverse();
        return recent;
    }

    public Task<int> DeleteForUserAsync(int? userId) => db.ChatMessages.Where(m => m.UserId == userId).ExecuteDeleteAsync();

    public Task<int> DeleteAllAsync() => db.ChatMessages.ExecuteDeleteAsync();
}
