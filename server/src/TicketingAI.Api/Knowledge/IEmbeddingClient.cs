namespace TicketingAI.Api.Knowledge;

public interface IEmbeddingClient
{
    /// <summary>Embeds each text and returns the vectors in the same order.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, string model, CancellationToken cancellationToken = default);
}
