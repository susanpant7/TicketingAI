namespace TicketingAI.Api.Knowledge;

public interface IKnowledgeDocumentReader
{
    /// <summary>Reads every Markdown document and splits it into one section per "##" heading.</summary>
    List<KnowledgeSection> ReadSections();
}
