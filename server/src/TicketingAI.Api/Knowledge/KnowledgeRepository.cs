using System.Runtime.InteropServices;
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

    // Raw SQL because EF Core's LINQ does not know sqlite-vec's functions. The extension is loaded by the connection interceptor.
    public Task<List<KnowledgeSearchRow>> SearchAsync(float[] queryVector, int topK) =>
        db.Database.SqlQuery<KnowledgeSearchRow>($"""
                SELECT Id, DocumentTitle, Category, Heading, Content, SourceFile,
                       vec_distance_cosine(Embedding, {MemoryMarshal.AsBytes(queryVector.AsSpan()).ToArray()}) AS Distance
                FROM KnowledgeChunks
                ORDER BY Distance
                LIMIT {topK}
                """)
            .ToListAsync();
}
