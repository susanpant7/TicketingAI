using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Chat;

public class ChatOptions
{
    public const string SectionName = "Chat";

    /// <summary>How many knowledge sections to give the model.</summary>
    [Range(1, 10)]
    public int TopK { get; init; } = 5;

    /// <summary>Sections scoring below this are ignored. Loose on purpose: it only filters out unrelated questions.</summary>
    [Range(0, 1)]
    public double MinScore { get; init; } = 0.6;

    /// <summary>How many recent messages are sent to the model for conversation memory.</summary>
    [Range(0, 20)]
    public int HistoryMessages { get; init; } = 6;
}
