using System.Runtime.InteropServices;

namespace TicketingAI.Api.Knowledge;

/// <summary>
/// Converts the stored knowledge to another embedding model. Vectors from different models cannot be compared, so when the embedding
/// model changes every stored section is embedded again from its text. Nothing is saved here: the caller saves the new vectors together
/// with the new settings in one transaction, so a failure (a wrong key, a model that is not there) changes nothing.
/// </summary>
public class EmbeddingSwitcher(IKnowledgeRepository knowledge, IEmbeddingClient embeddingClient)
{
    /// <summary>Embeds the text of every stored section with the given model. Returns the new vector of each section, by its id.</summary>
    public async Task<Dictionary<int, byte[]>> EmbedAllAsync(
        EmbeddingModelOptions options, string documentPrefix, CancellationToken cancellationToken)
    {
        var chunks = await knowledge.GetTextsAsync();
        if (chunks.Count == 0)
            return [];

        var texts = chunks.Select(c => EmbeddingText.Document(documentPrefix, c.DocumentTitle, c.Heading, c.Content)).ToList();
        var vectors = await embeddingClient.EmbedAsync(texts, options, cancellationToken);

        return chunks
            .Select((chunk, i) => (chunk.Id, Bytes: MemoryMarshal.AsBytes(vectors[i].AsSpan()).ToArray()))
            .ToDictionary(x => x.Id, x => x.Bytes);
    }
}
