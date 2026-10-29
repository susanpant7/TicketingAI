namespace TicketingAI.Api.Knowledge;

/// <summary>A section that matched a search. Score is cosine similarity: 1 is identical, lower is less related.</summary>
public record KnowledgeSearchResult(
    int Id, string DocumentTitle, string Category, string Heading, string Content, string SourceFile, double Score);
