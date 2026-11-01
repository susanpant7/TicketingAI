namespace TicketingAI.Api.Knowledge;

/// <summary>The text that is embedded: the title and heading give a section its context, and some models need a prefix.</summary>
public static class EmbeddingText
{
    public static string Document(string prefix, string title, string heading, string content) =>
        WithPrefix(prefix, $"{title} - {heading}\n{content}");

    public static string Query(string prefix, string query) => WithPrefix(prefix, query);

    // Some embedding models need a prefix on queries and on documents. An empty prefix means none.
    private static string WithPrefix(string prefix, string text) => prefix.Length == 0 ? text : $"{prefix} {text}";
}
