namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeService
{
    Task<List<KnowledgeChunkDto>> GetChunksAsync();

    /// <summary>Reads every document section, embeds it, then deletes all chunks and inserts the new ones.</summary>
    Task<ReloadResult> ReloadAsync(CancellationToken cancellationToken);
}
