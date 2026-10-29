namespace TicketingAI.Api.Knowledge;

/// <summary>The outcome for one uploaded file. Error is null when the file was added.</summary>
public record UploadFileResult(string FileName, int Sections, string? Error);

public record DeleteKnowledgeResult(int Deleted);

public record UploadedFile(string FileName, string Text);

public class UploadDepartmentNotFoundException() : Exception("The selected department does not exist.");
