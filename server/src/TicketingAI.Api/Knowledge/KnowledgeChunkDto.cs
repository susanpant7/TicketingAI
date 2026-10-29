namespace TicketingAI.Api.Knowledge;

public record KnowledgeChunkDto(
    int Id, string DocumentTitle, string Category, string Heading, string Content, string SourceFile, DateTime IngestedAt);
