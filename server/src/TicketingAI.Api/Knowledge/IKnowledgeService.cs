namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeService
{
    Task<List<KnowledgeChunkDto>> GetChunksAsync();

    /// <summary>Validates each file, then embeds and stores the valid ones. With flush, all existing knowledge is deleted first (only if at least one file is valid); otherwise a file replaces its earlier sections. A file whose department differs from the selected one is discarded.</summary>
    Task<List<UploadFileResult>> UploadAsync(int departmentId, bool flush, IReadOnlyList<UploadedFile> files, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a document written as a title and sections, replacing its earlier sections like an upload. Throws
    /// UploadDepartmentNotFoundException, ArgumentException for missing or invalid text, and KnowledgeConflictException when a
    /// new document's title is taken. Problems found by the upload checks (or Ollama) come back in the result's Error.
    /// </summary>
    Task<UploadFileResult> SaveDocumentAsync(SaveKnowledgeDocumentRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes every section of one document. Returns the number deleted.</summary>
    Task<int> DeleteDocumentAsync(string sourceFile);

    /// <summary>Uploads the default knowledge files (Knowledge/Documents/&lt;department&gt;/*.md) into their departments.</summary>
    Task<List<UploadFileResult>> LoadDefaultsAsync(CancellationToken cancellationToken);

    /// <summary>Deletes all the knowledge of a department. Returns the number of sections deleted.</summary>
    Task<int> DeleteDepartmentKnowledgeAsync(int departmentId);

    /// <summary>Finds the sections most relevant to a question.</summary>
    Task<List<KnowledgeSearchResult>> SearchAsync(string query, int topK, CancellationToken cancellationToken);
}
