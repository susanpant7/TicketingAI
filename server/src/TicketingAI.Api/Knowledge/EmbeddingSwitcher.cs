using System.Runtime.InteropServices;

namespace TicketingAI.Api.Knowledge;

/// <summary>
/// Converts the stored knowledge to another embedding model. Vectors from different models cannot be compared, so when the embedding
/// model changes every stored section is embedded again from its text. Nothing is saved here: the caller saves the new vectors together
/// with the new settings in one transaction, so a failure (a wrong key, a model that is not there) changes nothing.
/// </summary>
public class EmbeddingSwitcher(IKnowledgeRepository knowledge, IEmbeddingClient embeddingClient)
{
    private const int BatchSize = 16;

    /// <summary>
    /// Embeds the text of every stored section with the given model. Returns the new vector of each section, by its id. onProgress is told
    /// how many sections are done of how many, before the first batch and after each one.
    /// </summary>
    public async Task<Dictionary<int, byte[]>> EmbedAllAsync(
        EmbeddingModelOptions options, CancellationToken cancellationToken, Func<int, int, Task>? onProgress = null)
    {
        var chunks = await knowledge.GetTextsAsync();
        if (chunks.Count == 0)
            return [];

        var texts = chunks.Select(c => EmbeddingText.Document(c.DocumentTitle, c.Heading, c.Content)).ToList();
        var vectors = new List<float[]>(texts.Count);
        if (onProgress is not null) await onProgress(0, texts.Count);
        foreach (var batch in texts.Chunk(BatchSize))
        {
            vectors.AddRange(await embeddingClient.EmbedAsync(batch, options, cancellationToken));
            if (onProgress is not null) await onProgress(vectors.Count, texts.Count);
        }

        return chunks
            .Select((chunk, i) => (chunk.Id, Bytes: MemoryMarshal.AsBytes(vectors[i].AsSpan()).ToArray()))
            .ToDictionary(x => x.Id, x => x.Bytes);
    }
}
