using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Settings;

public record SavedChatModelDto(int Id, string Name, string BaseUrl, string Model, string Notes);

public record SaveChatModelRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(200)] string BaseUrl,
    [Required, StringLength(200)] string Model,
    [StringLength(500)] string? Notes);

public enum SavedChatModelError
{
    None,
    NotFound,
    NameTaken,
    Invalid
}

public record SavedChatModelResult(SavedChatModelDto? Model, SavedChatModelError Error = SavedChatModelError.None, string? Message = null);
