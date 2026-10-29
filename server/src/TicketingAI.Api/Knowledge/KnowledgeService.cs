using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public class KnowledgeService(
    IKnowledgeDocumentReader reader,
    IEmbeddingClient embeddingClient,
    IKnowledgeRepository repository) : IKnowledgeService
{
    // Only one reload may run at a time, across all requests.
    private static readonly SemaphoreSlim ReloadLock = new(1, 1);

    public Task<List<KnowledgeChunkDto>> GetChunksAsync() => repository.GetAllAsync();

    public async Task<ReloadResult> ReloadAsync(CancellationToken cancellationToken)
    {
        if (!await ReloadLock.WaitAsync(0, CancellationToken.None))
            throw new ReloadInProgressException();

        try
        {
            return await ReloadCoreAsync(cancellationToken);
        }
        finally
        {
            ReloadLock.Release();
        }
    }

    private async Task<ReloadResult> ReloadCoreAsync(CancellationToken cancellationToken)
    {
        var sections = reader.ReadSections();
        if (sections.Count == 0)
            throw new InvalidOperationException("No knowledge documents were found.");

        // Embed first: if Ollama fails, the existing chunks are left untouched.
        var vectors = await embeddingClient.EmbedAsync(sections.Select(ToEmbeddingText).ToList(), cancellationToken);

        var ingestedAt = DateTime.UtcNow;
        var chunks = sections.Select((section, i) => new KnowledgeChunk
        {
            DocumentTitle = section.DocumentTitle,
            Category = section.Category,
            Heading = section.Heading,
            Content = section.Content,
            SourceFile = section.SourceFile,
            ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(section.Content))),
            Embedding = MemoryMarshal.AsBytes(vectors[i].AsSpan()).ToArray(),
            IngestedAt = ingestedAt
        }).ToList();

        var deleted = await repository.ReplaceAllAsync(chunks);
        return new ReloadResult(deleted, chunks.Count, sections.Select(s => s.SourceFile).Distinct().Count());
    }

    // The title and heading give the section context, and the prefix is what nomic-embed-text expects for documents.
    private static string ToEmbeddingText(KnowledgeSection section) =>
        $"search_document: {section.DocumentTitle} - {section.Heading}\n{section.Content}";
}
