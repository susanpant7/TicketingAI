using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TicketingAI.Api.Departments;
using TicketingAI.Api.Settings;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Knowledge;

public partial class KnowledgeService(
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

                var texts = sections
                    .Select(s => EmbeddingText.Document(settings.EmbeddingDocumentPrefix, s.DocumentTitle, s.Heading, s.Content))
                    .ToList();
                var vectors = await embeddingClient.EmbedAsync(texts, settings.Embedding, cancellationToken);

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

    public async Task<UploadFileResult> SaveDocumentAsync(SaveKnowledgeDocumentRequest request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(request.DepartmentId)
            ?? throw new UploadDepartmentNotFoundException();

        var title = request.Title.Trim();
        if (title.Length == 0 || title.Length > 200 || title.Contains('\n'))
            throw new ArgumentException("The title is required, on one line, and can be up to 200 characters.");
        if (request.Sections.Count == 0)
            throw new ArgumentException("Add at least one section.");
        foreach (var section in request.Sections)
        {
            if (section.Heading.Trim().Length == 0 || section.Heading.Contains('\n') || section.Content.Trim().Length == 0)
                throw new ArgumentException("Every section needs a name (on one line) and some details.");
            // A line starting with "## " would split the section in two when the file is read back.
            if (section.Content.Replace("\r\n", "\n").Split('\n').Any(line => line.TrimStart().StartsWith("## ")))
                throw new ArgumentException($"The details of '{section.Heading.Trim()}' have a line starting with '## '. Remove the '##'.");
        }

        var sourceFile = request.SourceFile ?? FileNameFor(title);
        if (request.SourceFile is null && await repository.FileExistsAsync(sourceFile))
            throw new KnowledgeConflictException($"There is already a document called '{title}'. Choose another title, or edit that one.");

        // Written in the upload format, so it goes through exactly the same checks as an uploaded file.
        var text = $"title: {title}\ndepartment: {department.Name}\n\n" +
                   string.Join("\n\n", request.Sections.Select(s => $"## {s.Heading.Trim()}\n{s.Content.Trim()}"));
        return (await UploadAsync(department.Id, false, [new UploadedFile(sourceFile, text)], cancellationToken))[0];
    }

    public Task<int> DeleteDocumentAsync(string sourceFile) => repository.DeleteFileAsync(sourceFile);

    public async Task<List<UploadFileResult>> LoadDefaultsAsync(CancellationToken cancellationToken)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Knowledge", "Documents");
        if (!Directory.Exists(folder))
            return [];

        var departments = await departmentRepository.GetAllAsync();
        var results = new List<UploadFileResult>();
        foreach (var departmentFolder in Directory.GetDirectories(folder).Order())
        {
            var files = Directory.GetFiles(departmentFolder, "*.md").Order()
                .Select(file => new UploadedFile(Path.GetFileName(file), File.ReadAllText(file)))
                .ToList();
            var name = Path.GetFileName(departmentFolder);
            var department = departments.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (department is null)
            {
                results.AddRange(files.Select(f => new UploadFileResult(
                    f.FileName, 0, $"There is no '{name}' department. Load the default departments first.")));
                continue;
            }
            results.AddRange(await UploadAsync(department.Id, false, files, cancellationToken));
        }
        return results;
    }

    // "Laptop and hardware" -> "laptop-and-hardware.md". A title without a-z or 0-9 gets a made-up name; the title is stored anyway.
    private static string FileNameFor(string title)
    {
        var slug = NonAlphanumeric().Replace(title.ToLowerInvariant(), "-").Trim('-');
        return slug.Length > 0 ? $"{slug}.md" : $"document-{Guid.NewGuid().ToString("N")[..8]}.md";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();

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
            [EmbeddingText.Query(settings.EmbeddingQueryPrefix, query)], settings.Embedding, cancellationToken);

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
}
