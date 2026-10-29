namespace TicketingAI.Api.Settings;

/// <summary>The settings the embedding and chat calls use, read from the Settings table.</summary>
public record AiSettings(
    string EmbeddingModel,
    string EmbeddingQueryPrefix,
    string EmbeddingDocumentPrefix,
    int TopK,
    double MinScore,
    int FollowUpMaxWords,
    string ChatModel,
    double Temperature,
    double TopP,
    int NumCtx,
    int MaxTokens,
    int HistoryMessages,
    string SystemPrompt,
    string FormatReminder);
