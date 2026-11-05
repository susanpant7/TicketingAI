namespace TicketingAI.Data.Entities;

/// <summary>One message of a chat, kept so the conversation is still there after a reload. The admin's chat has no user.</summary>
public class StoredChatMessage
{
    public int Id { get; set; }

    /// <summary>The employee the chat belongs to. Null is the admin, who is not an employee.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>"user" for what the person wrote, "assistant" for the reply.</summary>
    public required string Role { get; set; }
    public required string Content { get; set; }

    /// <summary>The department an assistant reply is about, when there is one.</summary>
    public string? Department { get; set; }

    public DateTime CreatedAt { get; set; }
}
