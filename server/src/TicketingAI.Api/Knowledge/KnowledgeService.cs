using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TicketingAI.Api.Settings;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public class KnowledgeService(
    IKnowledgeDocumentReader reader,
    IEmbeddingClient embeddingClient,
    IKnowledgeRepository repository,
    ISettingsService settingsService) : IKnowledgeService
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

        // Read the settings first, so a change in the Settings table applies to this reload.
        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);

        // Embed first: if Ollama fails, the existing chunks are left untouched.
        var texts = sections.Select(s => WithPrefix(settings.EmbeddingDocumentPrefix, ToEmbeddingText(s))).ToList();
        var vectors = await embeddingClient.EmbedAsync(texts, settings.EmbeddingModel, cancellationToken);

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

    public async Task<List<KnowledgeSearchResult>> SearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        // Read the settings first. The query prefix must match the model and the prefix used when the sections were stored.
        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);
        var vectors = await embeddingClient.EmbedAsync(
            [WithPrefix(settings.EmbeddingQueryPrefix, query)], settings.EmbeddingModel, cancellationToken);

        var rows = await repository.SearchAsync(vectors[0], topK);
        return rows
            .Select(r => new KnowledgeSearchResult(
                r.Id, r.DocumentTitle, r.Category, r.Heading, r.Content, r.SourceFile, 1 - r.Distance))
            .ToList();
    }

    // The title and heading give the section context.
    private static string ToEmbeddingText(KnowledgeSection section) =>
        $"{section.DocumentTitle} - {section.Heading}\n{section.Content}";

    // Some embedding models need a prefix on queries and on documents. An empty prefix means none.
    private static string WithPrefix(string prefix, string text) => prefix.Length == 0 ? text : $"{prefix} {text}";
}
