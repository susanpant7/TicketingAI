using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Settings;

/// <summary>A saved chat or embedding model. HasApiKey says whether the variable named ApiKeyName is set; the key itself is never sent.</summary>
public record SavedAiModelDto(
    int Id, string Kind, string Name, string BaseUrl, string Model, string ApiKeyName, bool HasApiKey,
    string Notes);

public record SaveAiModelRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(200)] string BaseUrl,
    [Required, StringLength(200)] string Model,
    [StringLength(100)] string? ApiKeyName,
    [StringLength(500)] string? Notes);

public enum SavedAiModelError
{
    None,
    NotFound,
    NameTaken,
    Invalid
}

public record SavedAiModelResult(SavedAiModelDto? Model, SavedAiModelError Error = SavedAiModelError.None, string? Message = null);
