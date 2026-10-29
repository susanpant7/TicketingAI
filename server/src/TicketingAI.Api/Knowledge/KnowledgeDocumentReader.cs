namespace TicketingAI.Api.Knowledge;

public class KnowledgeDocumentReader : IKnowledgeDocumentReader
{
    private static readonly string DocumentsPath = Path.Combine(AppContext.BaseDirectory, "Knowledge", "Documents");

    public List<KnowledgeSection> ReadSections()
    {
        if (!Directory.Exists(DocumentsPath))
            return [];

        return Directory.EnumerateFiles(DocumentsPath, "*.md", SearchOption.AllDirectories)
            .Order()
            .SelectMany(file => ReadFile(file, Path.GetRelativePath(DocumentsPath, file).Replace('\\', '/')))
            .ToList();
    }

    private static IEnumerable<KnowledgeSection> ReadFile(string path, string sourceFile)
    {
        var lines = File.ReadAllLines(path);
        var (metadata, bodyStart) = ReadFrontMatter(lines);

        metadata.TryGetValue("title", out var title);
        metadata.TryGetValue("category", out var category);
        title ??= Path.GetFileNameWithoutExtension(path);
        category ??= "General";

        string? heading = null;
        var content = new List<string>();

        foreach (var line in lines.Skip(bodyStart))
        {
            if (line.StartsWith("## "))
            {
                if (heading is not null)
                    yield return ToSection(title, category, heading, content, sourceFile);

                heading = line[3..].Trim();
                content.Clear();
            }
            else if (heading is not null)
            {
                content.Add(line);
            }
        }

        if (heading is not null)
            yield return ToSection(title, category, heading, content, sourceFile);
    }

    private static KnowledgeSection ToSection(
        string title, string category, string heading, List<string> content, string sourceFile) =>
        new(title, category, heading, string.Join('\n', content).Trim(), sourceFile);

    private static (Dictionary<string, string> Metadata, int BodyStart) ReadFrontMatter(string[] lines)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return (metadata, 0);

        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
                return (metadata, i + 1);

            var separator = lines[i].IndexOf(':');
            if (separator > 0)
                metadata[lines[i][..separator].Trim()] = lines[i][(separator + 1)..].Trim();
        }

        return (metadata, 0);
    }
}
