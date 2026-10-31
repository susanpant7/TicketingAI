namespace TicketingAI.Api.Knowledge;

/// <summary>The outcome for one uploaded file. Error is null when the file was added.</summary>
public record UploadFileResult(string FileName, int Sections, string? Error);

public record DeleteKnowledgeResult(int Deleted);

public record UploadedFile(string FileName, string Text);

/// <summary>
/// A knowledge document written on the Knowledge page: a title, its department and its sections, without any file format.
/// SourceFile is set when editing an existing document, and left out for a new one (it is made from the title).
/// </summary>
public record SaveKnowledgeDocumentRequest(string? SourceFile, string Title, int DepartmentId, List<KnowledgeSectionInput> Sections);

public record KnowledgeSectionInput(string Heading, string Content);

/// <summary>The request is fine but clashes with existing knowledge, e.g. a new document with a title that is taken.</summary>
public class KnowledgeConflictException(string message) : Exception(message);

public class UploadDepartmentNotFoundException() : Exception("The selected department does not exist.");
