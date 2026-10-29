using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeRepository
{
    Task<List<KnowledgeChunkDto>> GetAllAsync();

    /// <summary>Deletes every chunk and inserts the new ones in a single transaction. Returns the number deleted.</summary>
    Task<int> ReplaceAllAsync(IReadOnlyList<KnowledgeChunk> chunks);

    /// <summary>Returns the sections whose embeddings are closest to the query vector, closest first.</summary>
    Task<List<KnowledgeSearchRow>> SearchAsync(float[] queryVector, int topK);
}
