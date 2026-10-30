namespace TicketingAI.Api.Common;

/// <summary>The outcome of loading default data: how many were added, and how many already existed and were left as they are.</summary>
public record LoadDefaultsResult(int Added, int Skipped);
