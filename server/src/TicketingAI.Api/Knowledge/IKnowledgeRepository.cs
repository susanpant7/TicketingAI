using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeRepository
{
    Task<List<KnowledgeChunkDto>> GetAllAsync();

    /// <summary>Deletes every chunk and inserts the new ones in a single transaction.</summary>
    Task ReplaceAllAsync(IReadOnlyList<KnowledgeChunk> chunks);

    /// <summary>Deletes the chunks of one source file and inserts the new ones in a single transaction.</summary>
    Task ReplaceFileAsync(string sourceFile, IReadOnlyList<KnowledgeChunk> chunks);

    /// <summary>Deletes every chunk of a department. Returns the number deleted.</summary>
    Task<int> DeleteByDepartmentAsync(string department);

    /// <summary>Returns the sections whose embeddings are closest to the query vector, closest first.</summary>
    Task<List<KnowledgeSearchRow>> SearchAsync(float[] queryVector, int topK);
}
