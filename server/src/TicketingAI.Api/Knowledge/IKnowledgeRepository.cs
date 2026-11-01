using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeRepository
{
    Task<List<KnowledgeChunkDto>> GetAllAsync();

    /// <summary>Deletes every chunk and inserts the new ones in a single transaction.</summary>
    Task ReplaceAllAsync(IReadOnlyList<KnowledgeChunk> chunks);

    /// <summary>Deletes the chunks of one source file and inserts the new ones in a single transaction.</summary>
    Task ReplaceFileAsync(string sourceFile, IReadOnlyList<KnowledgeChunk> chunks);

    /// <summary>Deletes every chunk of one source file. Returns the number deleted.</summary>
    Task<int> DeleteFileAsync(string sourceFile);

    Task<bool> FileExistsAsync(string sourceFile);

    /// <summary>Deletes every chunk of a department. Returns the number deleted.</summary>
    Task<int> DeleteByDepartmentAsync(string department);

    /// <summary>The text of every stored section, to embed again with another model.</summary>
    Task<List<KnowledgeChunkText>> GetTextsAsync();

    /// <summary>Returns the sections whose embeddings are closest to the query vector, closest first.</summary>
    Task<List<KnowledgeSearchRow>> SearchAsync(float[] queryVector, int topK);
}
