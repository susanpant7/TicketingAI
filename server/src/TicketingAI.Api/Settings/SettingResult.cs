namespace TicketingAI.Api.Settings;

public enum SettingError
{
    None,
    NotFound,
    KeyTaken,
    Invalid
}

public record SettingResult(SettingSaved? Saved, SettingError Error = SettingError.None, string? Message = null);
