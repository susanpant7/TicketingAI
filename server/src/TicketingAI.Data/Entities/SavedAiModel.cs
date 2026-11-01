namespace TicketingAI.Data.Entities;

/// <summary>
/// A chat or embedding model an admin saved to switch to later. "Use" on the Settings page copies the address, the model and the key
/// name into the settings. The key itself is never saved: ApiKeyName is only the name of the environment variable that holds it.
/// </summary>
public class SavedAiModel
{
    public const string Chat = "chat";
    public const string Embedding = "embedding";

    public int Id { get; set; }

    /// <summary>"chat" or "embedding".</summary>
    public required string Kind { get; set; }

    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public required string Model { get; set; }

    /// <summary>The environment variable with the key of an online service, for example GROQ_API_KEY. Empty for Ollama.</summary>
    public string ApiKeyName { get; set; } = "";

    /// <summary>Only for an embedding model: the text put before a question and before a knowledge section (empty means none).</summary>
    public string QueryPrefix { get; set; } = "";

    public string DocumentPrefix { get; set; } = "";
    public string Notes { get; set; } = "";
}
