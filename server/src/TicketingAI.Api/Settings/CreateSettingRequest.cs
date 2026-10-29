using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Settings;

public record CreateSettingRequest([Required] string Key, [Required] string Label, [Required] string Description, [Required] string Value);
