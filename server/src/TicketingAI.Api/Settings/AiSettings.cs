using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Settings;

/// <summary>The settings the embedding and chat calls use, read from the Settings table.</summary>
public record AiSettings(
    string EmbeddingBaseUrl,
    string EmbeddingApiKeyName,
    string EmbeddingModel,
    int TopK,
    double MinScore,
    double MaxScoreGap,
    int FollowUpMaxWords,
    string ChatBaseUrl,
    string ChatApiKeyName,
    string ChatModel,
    string ChatReasoningEffort,
    double Temperature,
    double TopP,
    int NumCtx,
    int MaxTokens,
    int HistoryMessages,
    string KeepAlive,
    string SystemPrompt,
    string FormatReminder)
{
    /// <summary>How the embedding model is called, from the settings above.</summary>
    public EmbeddingModelOptions Embedding => new(EmbeddingModel, EmbeddingBaseUrl, EmbeddingApiKeyName, KeepAlive);
}
