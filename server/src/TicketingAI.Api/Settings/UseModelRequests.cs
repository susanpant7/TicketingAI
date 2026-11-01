using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Settings;

/// <summary>Sets the Chat API base URL, the Chat model and the Chat API key name together.</summary>
public record UseChatModelRequest([Required] string BaseUrl, [Required] string Model, string? ApiKeyName);

/// <summary>
/// Sets the embedding address, model, key name and prefixes together, and converts the stored knowledge to the new model first. The
/// settings are only saved when that worked.
/// </summary>
public record UseEmbeddingModelRequest(
    [Required] string BaseUrl, [Required] string Model, string? ApiKeyName, string? QueryPrefix, string? DocumentPrefix);
