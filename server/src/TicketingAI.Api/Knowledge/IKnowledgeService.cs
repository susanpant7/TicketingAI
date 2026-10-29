namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeService
{
    Task<List<KnowledgeChunkDto>> GetChunksAsync();

    /// <summary>Reads every document section, embeds it, then deletes all chunks and inserts the new ones.</summary>
    Task<ReloadResult> ReloadAsync(CancellationToken cancellationToken);

    /// <summary>Finds the sections most relevant to a question.</summary>
    Task<List<KnowledgeSearchResult>> SearchAsync(string query, int topK, CancellationToken cancellationToken);
}
