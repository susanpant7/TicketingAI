namespace TicketingAI.Data.Entities;

/// <summary>One section of a knowledge document, with its embedding for similarity search.</summary>
public class KnowledgeChunk
{
    public int Id { get; set; }
    public required string DocumentTitle { get; set; }
    public required string Category { get; set; }
    public required string Heading { get; set; }
    public required string Content { get; set; }

    /// <summary>Path of the source file, relative to the documents folder.</summary>
    public required string SourceFile { get; set; }

    /// <summary>SHA-256 of the section text, used to skip sections that have not changed.</summary>
    public required string ContentHash { get; set; }

    /// <summary>The embedding as raw little-endian 32-bit floats (the format sqlite-vec expects).</summary>
    public required byte[] Embedding { get; set; }

    public DateTime IngestedAt { get; set; }
}
