using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Knowledge;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required]
    public string BaseUrl { get; init; } = "http://localhost:11434";
}
