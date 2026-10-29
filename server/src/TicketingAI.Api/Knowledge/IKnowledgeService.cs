namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeService
{
    Task<List<KnowledgeChunkDto>> GetChunksAsync();

    /// <summary>Deletes all chunks, then reads, embeds and inserts every document section.</summary>
    Task<IngestResult> IngestAsync(CancellationToken cancellationToken);

    Task<int> DeleteAllAsync();
}
