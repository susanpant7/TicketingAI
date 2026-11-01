namespace TicketingAI.Api.Settings;

public record SettingDto(string Key, string Label, string Description, string Value);

/// <summary>The result of saving a setting.</summary>
public record SettingSaved(SettingDto Setting);
