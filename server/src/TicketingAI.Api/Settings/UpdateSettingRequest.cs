using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Settings;

public record UpdateSettingRequest([Required] string Label, [Required] string Description, [Required] string Value);
