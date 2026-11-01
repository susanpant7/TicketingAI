namespace TicketingAI.Data.Entities;

/// <summary>
/// A chat model an admin saved to switch to later. "Use" on the Settings page copies BaseUrl and Model into the "Chat API base URL"
/// and "Chat model" settings. The key of an online service is never saved here: it is the environment variable ChatApi__ApiKey.
/// </summary>
public class SavedChatModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public required string Model { get; set; }
    public string Notes { get; set; } = "";
}
