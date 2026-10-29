namespace TicketingAI.Api.Knowledge;

/// <summary>Shape of the raw SQL search query: a section plus its cosine distance to the query vector.</summary>
public class KnowledgeSearchRow
{
    public int Id { get; set; }
    public string DocumentTitle { get; set; } = "";
    public string Category { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Content { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public double Distance { get; set; }
}
