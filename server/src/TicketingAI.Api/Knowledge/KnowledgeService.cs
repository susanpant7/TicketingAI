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
    public Task<List<KnowledgeChunkDto>> GetChunksAsync() => repository.GetAllAsync();

    public async Task<IngestResult> IngestAsync(CancellationToken cancellationToken)
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
        return new IngestResult(deleted, chunks.Count, sections.Select(s => s.SourceFile).Distinct().Count());
    }

    public Task<int> DeleteAllAsync() => repository.DeleteAllAsync();

    // The title and heading give the section context, and the prefix is what nomic-embed-text expects for documents.
    private static string ToEmbeddingText(KnowledgeSection section) =>
        $"search_document: {section.DocumentTitle} - {section.Heading}\n{section.Content}";
}
