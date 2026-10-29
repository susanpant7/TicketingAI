using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Chat;

/// <summary>The new message plus the recent conversation. The server keeps no conversation state yet.</summary>
public record ChatRequest(
    [Required, StringLength(2000)] string Message,
    [MaxLength(40)] List<ChatMessage>? History);
