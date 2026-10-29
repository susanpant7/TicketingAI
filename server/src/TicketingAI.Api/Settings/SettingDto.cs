namespace TicketingAI.Api.Settings;

public record SettingDto(string Key, string Label, string Description, string Value);

/// <summary>The result of saving a setting. KnowledgeDeleted is how many knowledge sections were deleted by the change.</summary>
public record SettingSaved(SettingDto Setting, int KnowledgeDeleted);
