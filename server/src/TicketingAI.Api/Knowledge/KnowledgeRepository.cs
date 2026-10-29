using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Knowledge;

public class KnowledgeRepository(AppDbContext db) : IKnowledgeRepository
{
    public Task<List<KnowledgeChunkDto>> GetAllAsync() =>
        db.KnowledgeChunks
            .OrderBy(c => c.Category).ThenBy(c => c.SourceFile).ThenBy(c => c.Id)
            .Select(c => new KnowledgeChunkDto(
                c.Id, c.DocumentTitle, c.Category, c.Heading, c.Content, c.SourceFile, c.IngestedAt))
            .ToListAsync();

    public async Task<int> ReplaceAllAsync(IReadOnlyList<KnowledgeChunk> chunks)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var deleted = await db.KnowledgeChunks.ExecuteDeleteAsync();
        db.KnowledgeChunks.AddRange(chunks);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
        return deleted;
    }
}
