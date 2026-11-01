namespace TicketingAI.Api.Settings;

/// <summary>The settings the embedding and chat calls use, read from the Settings table.</summary>
public record AiSettings(
    string OllamaBaseUrl,
    string EmbeddingModel,
    string EmbeddingQueryPrefix,
    string EmbeddingDocumentPrefix,
    int TopK,
    double MinScore,
    double MaxScoreGap,
    int FollowUpMaxWords,
    string ChatProvider,
    string ChatBaseUrl,
    string ChatModel,
    string ChatReasoningEffort,
    double Temperature,
    double TopP,
    int NumCtx,
    int MaxTokens,
    int HistoryMessages,
    string KeepAlive,
    string SystemPrompt,
    string FormatReminder);
