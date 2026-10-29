namespace TicketingAI.Api.Knowledge;

public record ParsedKnowledgeFile(string Title, string Department, List<(string Heading, string Content)> Sections);

/// <summary>
/// Validates an uploaded knowledge file. Expected format:
/// <code>
/// title: Leave Policy
/// department: HR
///
/// ## Annual leave
/// Section content...
/// </code>
/// </summary>
public static class KnowledgeUploadParser
{
    /// <summary>Returns the parsed file, or an error message that tells the admin what to fix.</summary>
    public static (ParsedKnowledgeFile? File, string? Error) Parse(string text)
    {
        string? title = null, department = null;
        var sections = new List<(string Heading, string Content)>();
        string? heading = null;
        var content = new List<string>();

        string? CloseSection()
        {
            if (heading is null) return null;
            var body = string.Join('\n', content).Trim();
            if (heading.Length == 0) return "A section has an empty title. Write the title after '## '.";
            if (body.Length == 0) return $"Section '{heading}' has no content.";
            if (sections.Any(s => s.Heading.Equals(heading, StringComparison.OrdinalIgnoreCase)))
                return $"Section '{heading}' appears more than once.";
            sections.Add((heading, body));
            return null;
        }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();

            if (line.StartsWith("## "))
            {
                if (CloseSection() is { } error) return (null, error);
                heading = line[3..].Trim();
                content.Clear();
            }
            else if (heading is not null)
            {
                content.Add(raw);
            }
            else if (line.Length > 0 && line != "---" && !line.StartsWith("# "))
            {
                var colon = line.IndexOf(':');
                var key = colon > 0 ? line[..colon].Trim().ToLowerInvariant() : "";
                var value = colon > 0 ? line[(colon + 1)..].Trim() : "";
                if (key == "title") title = value;
                else if (key == "department") department = value;
                else return (null, $"Unexpected text before the first section: '{line}'. Only 'title:' and 'department:' are allowed at the top.");
            }
        }

        if (CloseSection() is { } lastError) return (null, lastError);

        if (string.IsNullOrWhiteSpace(title)) return (null, "The file must start with a 'title:' line.");
        if (string.IsNullOrWhiteSpace(department)) return (null, "The file must have a 'department:' line at the top.");
        if (sections.Count == 0) return (null, "The file has no sections. Add at least one '## Section title' followed by its content.");

        return (new ParsedKnowledgeFile(title, department, sections), null);
    }
}
