using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeRepository
{
    Task<List<KnowledgeChunkDto>> GetAllAsync();

    /// <summary>Deletes every chunk and inserts the new ones in a single transaction. Returns the number deleted.</summary>
    Task<int> ReplaceAllAsync(IReadOnlyList<KnowledgeChunk> chunks);

    Task<int> DeleteAllAsync();
}
