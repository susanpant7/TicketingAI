namespace TicketingAI.Api.Settings;

public enum SettingError
{
    None,
    NotFound,
    KeyTaken,
    Invalid
}

/// <summary>Reembedded is how many stored knowledge sections were embedded again (0 unless the embedding model changed).</summary>
public record RestoreDefaultsResult(int Reembedded);

/// <summary>The time a model took to answer a test request, in milliseconds (the request is a tiny question, so this includes any thinking).</summary>
public record TestModelResult(long Milliseconds);

public record UseModelResult(int Reembedded, SettingError Error = SettingError.None, string? Message = null);

public record SettingResult(SettingSaved? Saved, SettingError Error = SettingError.None, string? Message = null);
