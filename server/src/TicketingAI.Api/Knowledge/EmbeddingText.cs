namespace TicketingAI.Api.Knowledge;

/// <summary>
/// The text that is embedded: the title and heading give a section its context. The same two prefixes are used for every embedding
/// model, so a question and a section are always compared the same way: a model that does not need them is not hurt by them.
/// </summary>
public static class EmbeddingText
{
    private const string QueryPrefix = "search_query:";
    private const string DocumentPrefix = "search_document:";

    public static string Document(string title, string heading, string content) =>
        $"{DocumentPrefix} {title} - {heading}\n{content}";

    public static string Query(string query) => $"{QueryPrefix} {query}";
}
