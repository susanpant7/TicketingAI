namespace TicketingAI.Data.Entities;

/// <summary>One application setting. Settings are stored row by row, and the value is always text.</summary>
public class Setting
{
    /// <summary>Unique name, grouped by prefix, for example "retrieval.top_k".</summary>
    public required string Key { get; set; }

    /// <summary>Short name to show next to the value.</summary>
    public required string Label { get; set; }

    /// <summary>Plain-language help: what the setting does and what to watch for when changing it.</summary>
    public required string Description { get; set; }

    public required string Value { get; set; }
}
