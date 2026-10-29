using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TicketingAI.Api.Departments;
using TicketingAI.Api.Settings;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public class KnowledgeService(
    IEmbeddingClient embeddingClient,
    IKnowledgeRepository repository,
    ISettingsService settingsService,
    IDepartmentRepository departmentRepository) : IKnowledgeService
{
    public Task<List<KnowledgeChunkDto>> GetChunksAsync() => repository.GetAllAsync();

    public async Task<List<UploadFileResult>> UploadAsync(
        int departmentId, bool flush, IReadOnlyList<UploadedFile> files, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(departmentId)
            ?? throw new UploadDepartmentNotFoundException();

        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);
        var results = new List<UploadFileResult>();
        var accepted = new List<(string FileName, List<KnowledgeChunk> Chunks)>();

        // Validate and embed every file first, so nothing is deleted or changed if a file fails.
        foreach (var file in files)
        {
            var (parsed, error) = KnowledgeUploadParser.Parse(file.Text);
            if (parsed is not null && !parsed.Department.Equals(department.Name, StringComparison.OrdinalIgnoreCase))
                error = $"The file is for department '{parsed.Department}' but '{department.Name}' was selected. The file was discarded.";
            if (accepted.Any(a => a.FileName == file.FileName))
                error = "Another file with the same name is in this upload.";

            if (parsed is null || error is not null)
            {
                results.Add(new UploadFileResult(file.FileName, 0, error));
                continue;
            }

            try
            {
                var sections = parsed.Sections
                    .Select(s => new KnowledgeSection(parsed.Title, department.Name, s.Heading, s.Content, file.FileName))
                    .ToList();

                var texts = sections.Select(s => WithPrefix(settings.EmbeddingDocumentPrefix, ToEmbeddingText(s))).ToList();
                var vectors = await embeddingClient.EmbedAsync(texts, settings.EmbeddingModel, cancellationToken);

                accepted.Add((file.FileName, ToChunks(sections, vectors)));
                results.Add(new UploadFileResult(file.FileName, sections.Count, null));
            }
            catch (EmbeddingException ex)
            {
                results.Add(new UploadFileResult(file.FileName, 0, ex.Message));
            }
        }

        if (flush && accepted.Count > 0)
        {
            await repository.ReplaceAllAsync(accepted.SelectMany(a => a.Chunks).ToList());
        }
        else if (!flush)
        {
            foreach (var (fileName, chunks) in accepted)
                await repository.ReplaceFileAsync(fileName, chunks);
        }

        return results;
    }

    public async Task<int> DeleteDepartmentKnowledgeAsync(int departmentId)
    {
        var department = await departmentRepository.GetByIdAsync(departmentId)
            ?? throw new UploadDepartmentNotFoundException();
        return await repository.DeleteByDepartmentAsync(department.Name);
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

    private static List<KnowledgeChunk> ToChunks(IReadOnlyList<KnowledgeSection> sections, IReadOnlyList<float[]> vectors)
    {
        var ingestedAt = DateTime.UtcNow;
        return sections.Select((section, i) => new KnowledgeChunk
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
    }

    // The title and heading give the section context.
    private static string ToEmbeddingText(KnowledgeSection section) =>
        $"{section.DocumentTitle} - {section.Heading}\n{section.Content}";

    // Some embedding models need a prefix on queries and on documents. An empty prefix means none.
    private static string WithPrefix(string prefix, string text) => prefix.Length == 0 ? text : $"{prefix} {text}";
}
